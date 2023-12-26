using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.Sat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SolverLibrary
{
    public class Solver
    {
        private StationGraph station;
        private int timeInaccuracy;
        public Solver(StationGraph station, int timeInaccuracy) 
        {
            station.CheckStationGraph();
            this.timeInaccuracy = timeInaccuracy;
            this.station = station;
        }

        public StationWorkPlan CalculateWorkPlan(TrainSchedule schedule)
        {
            // Check the graph for stupid errors
            if (!this.station.CheckStationGraph())
            {
                throw new Exception("Something wrong with graph");
            }
            Dictionary<Train, SingleTrainSchedule> dictSchedule = schedule.GetSchedule();
            HashSet<InputVertex> inputVertices = station.GetInputVertices();
            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection = new();
            Dictionary<InputVertex, List<GraphPath>> pathsStartFromVertex = new();

            // Calculate pathes that start from InputVertex and end on some platform
            calculatePathsFromIn(inputVertices, platformsWithDirection, pathsStartFromVertex);

            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatfrom = new();
            HashSet<Vertex> outputVertexes = new();
            // Calculate pathes that start from platform and end in OutputVertex
            calculatePathsFromPlatfroms(platformsWithDirection, outputVertexes, pathsStartFromPlatfrom);

            CpModel model = new CpModel();

            // Enumerate trains and platforms
            Dictionary<Train, int> trainId = new();
            int trainsCnt = 0;
            foreach (var trainSchedule in dictSchedule)
            {
                if (trainId.ContainsKey(trainSchedule.Key))
                {
                    throw new Exception("Duplicate train in schedule");
                }
                trainId[trainSchedule.Key] = trainsCnt;
                trainsCnt++;
            }
            Dictionary<Tuple<Vertex, Vertex>, int> platformId = new();
            int platformsCnt = 0;
            foreach (var platform in platformsWithDirection)
            {
                platformId[platform] = platformsCnt;
                platformsCnt++;
            }
            
            // Create BoolVar for each train condition
            BoolVar[,] trainGoesThroughPlatf = new BoolVar[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainGoesThroughPlatf[i, j] = model.NewBoolVar($"x[{i}, {j}]");
                }
            }

            //Find paths for each train condition
            Tuple<GraphPath?, GraphPath?>[,] trainConditionPaths = new Tuple<GraphPath?, GraphPath?>[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainConditionPaths[i, j] = new(null, null);
                }
            }
            foreach (var train in dictSchedule.Keys)
            {
                var trainSchedule = dictSchedule[train];
                InputVertex input = trainSchedule.GetVertexIn();
                Vertex output = trainSchedule.GetVertexOut();
                foreach (var pathFromIn in pathsStartFromVertex[input])
                {
                    var vertices = pathFromIn.GetVertices();
                    Tuple<Vertex, Vertex> platform = new(vertices[vertices.Count - 2], vertices[vertices.Count - 1]);
                    foreach (var pathFromPlat in pathsStartFromPlatfrom[platform])
                    {
                        if (pathFromPlat.GetVertices().Last() == output)
                        {
                            Edge edgePlat = findEdge(platform.Item1, platform.Item2);
                            if (edgePlat == null)
                            {
                                continue;
                            }

                            int travelTime = (pathFromIn.length + pathFromPlat.length + train.GetSpeed() - 1) / train.GetSpeed(); 
                            if ( edgePlat.GetLength() >= train.GetLength() &&
                                (train.GetTrainType() == TrainType.NONE || edgePlat.GetEdgeType() == train.GetTrainType()) &&
                                 trainSchedule.GetTimeArrival() + trainSchedule.GetTimeStop() + travelTime <= trainSchedule.GetTimeDeparture()
                                )
                            {
                                trainConditionPaths[trainId[train], platformId[platform]] = new(pathFromIn, pathFromPlat);
                            }
                        }
                    }
                }
            }

            // Check that for each train we have at least one suitable and reachable platform 
            foreach (var train in dictSchedule.Keys)
            {
                bool hasSuitablePlat = false;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        hasSuitablePlat = true;
                    }
                }
                if (!hasSuitablePlat)
                {
                    throw new Exception("One of train hasn't suitable and reachable platform");
                }
            }

            // Add constraint for each train with his possible paths
            foreach (var train in dictSchedule.Keys)
            {
                List<ILiteral> goodConditons = new();
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        goodConditons.Add(trainGoesThroughPlatf[trainId[train], j]);
                    } else
                    {
                        model.AddAssumption(trainGoesThroughPlatf[trainId[train], j].Not());
                    }
                }
                model.AddExactlyOne(goodConditons);
            }

            foreach (var train1 in dictSchedule.Keys)
            {
                for (int plat1 = 0; plat1 < platformsCnt; ++plat1)
                {
                    if (trainConditionPaths[trainId[train1], plat1].Item1 == null ||
                        trainConditionPaths[trainId[train1], plat1].Item2 == null)
                    {
                        continue;
                    }
                    var edgesTimeBlocks1 = calculateEdgesTimeBlocking(
                                train1, dictSchedule[train1], 
                                trainConditionPaths[trainId[train1], plat1].Item1, 
                                trainConditionPaths[trainId[train1], plat1].Item2);
                    foreach (var train2 in dictSchedule.Keys)
                    {
                        if (train2 == train1)
                        {
                            continue;
                        }
                        for (int plat2 = 0; plat2 < platformsCnt; ++plat2)
                        {
                            if (trainConditionPaths[trainId[train2], plat2].Item1 == null ||
                                trainConditionPaths[trainId[train2], plat2].Item2 == null)
                            {
                                continue;
                            }

                            bool flag = true;
                            var edgesTimeBlocks2 = calculateEdgesTimeBlocking(
                                train2, dictSchedule[train2],
                                trainConditionPaths[trainId[train2], plat2].Item1,
                                trainConditionPaths[trainId[train2], plat2].Item2);

                            foreach (var edge in edgesTimeBlocks1.Keys)
                            {
                                if (edgesTimeBlocks2.ContainsKey(edge)) {
                                    if (hasListsOfIntervalsIntrsection(edgesTimeBlocks1[edge], edgesTimeBlocks2[edge]))
                                    {
                                        flag = false;
                                    }
                                }
                            }
                            if (!flag)
                            {
                                ILiteral[] boolVars = { trainGoesThroughPlatf[trainId[train1], plat1].Not(), trainGoesThroughPlatf[trainId[train2], plat2].Not() };
                                model.AddBoolOr(boolVars);
                            }
                        }
                    }
                }
            }


            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
            {
                throw new Exception("No solution ((((");
            }

            StationWorkPlan plan = new();
            foreach (Train train in dictSchedule.Keys)
            {
                for (int platform = 0; platform < platformsCnt; ++platform)
                {
                    if (solver.Value(trainGoesThroughPlatf[trainId[train], platform]) == 1)
                    {
                        var tmp = trainConditionPaths[trainId[train], platform].Item1.GetVertices();
                        plan.AddTrainWithPlatform(train, findEdge(tmp[tmp.Count - 1], tmp[tmp.Count - 2]));
                    }
                }
            }

            return plan;
        }

        private static Edge findEdge(Vertex start, Vertex end)
        {
            foreach (var i in start.GetEdgeConnections())
            {
                if (i.Item1 != null && hasEdgeThatEndings(i.Item1, new(start, end)))
                {
                    return i.Item1;
                }
                if (i.Item2 != null && hasEdgeThatEndings(i.Item2, new(start, end)))
                {
                    return i.Item2;
                }
            }
            return null;
        }

        private static bool hasIntervalsIntersection(Tuple<int, int> t1,  Tuple<int, int> t2)
        {
            return Math.Min(t1.Item2, t2.Item2) >= Math.Max(t1.Item1, t2.Item1);
        }

        private static bool hasListsOfIntervalsIntrsection(List<Tuple<int, int>> l1, List<Tuple<int, int>> l2)
        {
            for (int i = 0; i < l1.Count; ++i)
            {
                for (int j = 0 ; j < l2.Count; ++j)
                {
                    if (hasIntervalsIntersection(l1[i], l2[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Dictionary<Edge, List<Tuple<int, int>>> calculateEdgesTimeBlocking(Train train,
            SingleTrainSchedule trainSchedule, GraphPath pathFromIn, GraphPath pathFromPlat)
        {
            Dictionary<Edge, List<Tuple<int, int>>> res = calculateEdgesTimeBlocking(train, trainSchedule.GetTimeArrival() ,pathFromIn);
            var platRes = calculateEdgesTimeBlocking(train, trainSchedule.GetTimeDeparture() - (pathFromPlat.length + train.GetSpeed() - 1) / train.GetSpeed(), pathFromPlat);
            foreach (var edge in platRes.Keys)
            {
                if (res.ContainsKey(edge))
                {
                    res[edge].AddRange(platRes[edge]);
                } else
                {
                    res[edge] = platRes[edge];
                }
            }

            var vertices = pathFromIn.GetVertices();
            Edge platform = findEdge(vertices[vertices.Count - 2], vertices[vertices.Count - 1]);
            int timeStopBegin = trainSchedule.GetTimeArrival() + (pathFromIn.length + train.GetSpeed() - 1) / train.GetSpeed();
            int timeStopEnd = trainSchedule.GetTimeDeparture() - (pathFromPlat.length + train.GetSpeed() - 1) / train.GetSpeed();
            if (!res.ContainsKey(platform))
            {
                res[platform] = new();
            }
            res[platform].Add(new(timeStopBegin - timeInaccuracy, timeStopEnd + timeInaccuracy));
            return res;
        }

        private Dictionary<Edge, List<Tuple<int, int>>> calculateEdgesTimeBlocking(Train train, int beginTime, GraphPath path)
        {
            Dictionary<Edge, List<Tuple<int, int>>> res = new();
            
            var vertices = path.GetVertices();
            for (int i = 0; i < vertices.Count - 1; ++i)
            {
                List<Tuple<int, int>> tmp = new();
                tmp.Add(new(beginTime - timeInaccuracy, beginTime + (path.length + train.GetSpeed() - 1) / train.GetSpeed() + timeInaccuracy));
                Edge e = findEdge(vertices[i], vertices[i + 1]);
                res[e] = tmp;
            }
            return res;
        }

        private static void calculatePathsFromIn(
            HashSet<InputVertex> inputVertices,
            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection,
            Dictionary<InputVertex, List<GraphPath>> pathsStartFromVertex)
        {
            // Calculate pathes that start from InputVertex and end on some platform
            foreach (InputVertex start in inputVertices)
            {
                Dictionary<Tuple<Vertex?, Vertex?>, int> dist = new();
                Dictionary<Tuple<Vertex?, Vertex?>, Tuple<Vertex?, Vertex?>> parent = new();
                HashSet<Tuple<Vertex?, Vertex?>> usedPositions = new();
                Tuple<Vertex?, Vertex?> startPosition = new(null, start);
                List<GraphPath> paths = new();
                dist[startPosition] = 0;
                parent[startPosition] = startPosition;
                while (dist.Count > usedPositions.Count)
                {
                    Tuple<Vertex?, Vertex?>? bestPos = null;
                    foreach (var pairPosDist in dist)
                    {
                        if (!usedPositions.Contains(pairPosDist.Key) && (bestPos == null || dist[bestPos] > pairPosDist.Value))
                        {
                            bestPos = pairPosDist.Key;
                        }
                    }
                    if (bestPos == null)
                    {
                        break;
                    }
                    usedPositions.Add(bestPos);
                    if (platformsWithDirection.Contains(bestPos))
                    {
                        // In this moment we calculated shortest path from start to some platform
                        GraphPath path = new GraphPath(start);
                        List<Vertex> invPath = new();
                        while (parent[bestPos] != bestPos)
                        {
                            invPath.Add(bestPos.Item2);
                            bestPos = parent[bestPos];
                        }
                        invPath.Reverse();
                        foreach (var pathV in invPath)
                        {
                            if (!path.TryAddVertexToEnd(pathV))
                            {
                                throw new Exception();
                            }
                        }
                        paths.Add(path);
                        continue;
                    }
                    Vertex v = bestPos.Item2;
                    foreach (var connection in v.GetEdgeConnections())
                    {
                        Edge? nextEdge = null;
                        if (bestPos.Item1 == null)
                        {
                            // Position in which train just enter station
                            nextEdge = connection.Item1 != null ? connection.Item1 : connection.Item2;
                        }
                        else
                        {
                            Tuple<Vertex?, Vertex?> swapedPos = new(bestPos.Item2, bestPos.Item1);
                            if (connection.Item1 != null && hasEdgeThatEndings(connection.Item1, bestPos))
                            {
                                nextEdge = connection.Item2;
                            }
                            if (connection.Item2 != null && hasEdgeThatEndings(connection.Item2, bestPos))
                            {
                                nextEdge = connection.Item1;
                            }
                        }
                        if (nextEdge == null)
                        {
                            continue;
                        }
                        Tuple<Vertex?, Vertex?> nextPos = nextEdge.GetStart() == v ? new(v, nextEdge.GetEnd()) : new(v, nextEdge.GetStart());
                        if (!dist.ContainsKey(nextPos) && nextEdge.GetEdgeType() != TrainType.NONE)
                        {
                            platformsWithDirection.Add(nextPos);
                        }
                        if (!dist.ContainsKey(nextPos) || dist[nextPos] > dist[bestPos] + nextEdge.GetLength())
                        {
                            dist[nextPos] = dist[bestPos] + nextEdge.GetLength();
                            parent[nextPos] = bestPos;
                        }
                    }
                }
                pathsStartFromVertex[start] = paths;
            }
        }

        private static void calculatePathsFromPlatfroms(
            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection,
            HashSet<Vertex> outputVertexes,
            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatfrom
            )
        {
            foreach (Tuple<Vertex, Vertex> startPos in platformsWithDirection)
            {
                Dictionary<Tuple<Vertex?, Vertex?>, int> dist = new();
                Dictionary<Tuple<Vertex?, Vertex?>, Tuple<Vertex?, Vertex?>> parent = new();
                HashSet<Tuple<Vertex?, Vertex?>> usedPositions = new();
                List<GraphPath> paths = new();
                dist[startPos] = 0;
                parent[startPos] = startPos;
                while (dist.Count > usedPositions.Count)
                {
                    Tuple<Vertex?, Vertex?>? bestPos = null;
                    foreach (var pairPosDist in dist)
                    {
                        if (!usedPositions.Contains(pairPosDist.Key) && (bestPos == null || dist[bestPos] > pairPosDist.Value))
                        {
                            bestPos = pairPosDist.Key;
                        }
                    }
                    if (bestPos == null)
                    {
                        break;
                    }
                    usedPositions.Add(bestPos);
                    if (bestPos.Item2 == null)
                    {
                        if (!outputVertexes.Contains(bestPos.Item1))
                        {
                            continue;
                        }
                        // In this moment we calculated shortest path from platform to exit from station
                        GraphPath path = new GraphPath(startPos.Item1, startPos.Item2);
                        List<Vertex> invPath = new();
                        bestPos = parent[bestPos];
                        while (parent[bestPos] != bestPos)
                        {
                            invPath.Add(bestPos.Item2);
                            bestPos = parent[bestPos];
                        }
                        invPath.Reverse();
                        foreach (var pathV in invPath)
                        {
                            if (!path.TryAddVertexToEnd(pathV))
                            {
                                throw new Exception();
                            }
                        }
                        paths.Add(path);
                        continue;
                    }
                    Vertex v = bestPos.Item2;
                    foreach (var connection in v.GetEdgeConnections())
                    {
                        Edge? nextEdge = null;
                        if (bestPos.Item2 == null)
                        {
                            // Position in which train exit station
                            nextEdge = null;
                        }
                        else
                        {
                            Tuple<Vertex?, Vertex?> swapedPos = new(bestPos.Item2, bestPos.Item1);
                            if (connection.Item1 != null && hasEdgeThatEndings(connection.Item1, bestPos))
                            {
                                nextEdge = connection.Item2;
                            }
                            else
                            if (connection.Item2 != null && hasEdgeThatEndings(connection.Item2, bestPos))
                            {
                                nextEdge = connection.Item1;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        Tuple<Vertex?, Vertex?> nextPos;
                        if (nextEdge == null)
                        {
                            if (v.GetVertexType() == VertexType.OUTPUT)
                            {
                                outputVertexes.Add(v);
                            }
                            nextPos = new(v, null);
                        }
                        else
                        {
                            nextPos = nextEdge.GetStart() == v ? new(v, nextEdge.GetEnd()) : new(v, nextEdge.GetStart());
                        }
                        if (!dist.ContainsKey(nextPos) || dist[nextPos] > dist[bestPos] + nextEdge.GetLength())
                        {
                            dist[nextPos] = dist[bestPos] + (nextEdge == null ? 0 : nextEdge.GetLength());
                            parent[nextPos] = bestPos;
                        }
                    }
                }
                pathsStartFromPlatfrom[startPos] = paths;
            }
        }
        private static bool hasEdgeThatEndings(Edge edge, Tuple<Vertex, Vertex> endings)
        {
            return (edge.GetEnd() == endings.Item1 && edge.GetStart() == endings.Item2) ||
                (edge.GetEnd() == endings.Item2 && edge.GetStart() == endings.Item1);
        }

        private class GraphPath
        {
            private List<Vertex> vertices = new();
            public int length = 0;
            public int id = -1;
            public GraphPath(InputVertex start) {
                vertices.Add(start);
            }

            public GraphPath(Vertex platformStart, Vertex platformEnd)
            {
                vertices.Add(platformStart);
                this.TryAddVertexToEnd(platformEnd);
            }

            public List<Vertex> GetVertices()
            {
                return vertices;
            }

            public bool TryAddVertexToEnd(Vertex vertex)
            {
                if (CheckVertexAsNext(vertex))
                {
                    Vertex p = vertices[vertices.Count - 1];
                    vertices.Add(vertex);
                    Tuple<Vertex, Vertex> t = new(p, vertex);
                    foreach (var connection in p.GetEdgeConnections())
                    {
                        if (connection.Item1 != null && hasEdgeThatEndings(connection.Item1, t)) 
                        {
                            length += connection.Item1.GetLength();
                            break;
                        }
                    }
                    return true;
                }
                return false;
            }

            public bool CheckVertexAsNext(Vertex vertex)
            {
                // Check that p1 -> p2 -> vertex is normal path.
                Vertex? p1 = null;
                Vertex p2 = vertices.Last();
                if (vertices.Count > 1)
                {
                    p1 = vertices[vertices.Count - 2];
                }
                foreach (var connection in p2.GetEdgeConnections())
                {
                    Edge e1 = connection.Item1;
                    Edge e2 = connection.Item2;

                    if (vertices.Count == 1)
                    {
                        if (e1 == null)
                        {
                            return hasEdgeThatEndings(e2, new(p2, vertex));
                        }
                        if (e2 == null)
                        {
                            return hasEdgeThatEndings(e1, new(p2, vertex));
                        }
                        return hasEdgeThatEndings(e1, new(p2, vertex)) || hasEdgeThatEndings(e2, new(p2, vertex));
                    }

                    if ((e2.GetStart() == p1 && e2.GetEnd() == p2) || (e2.GetStart() == p2 && e2.GetEnd() == p1))
                    {
                        (e1, e2) = (e2, e1);
                    }
                    if ((e1.GetStart() == p1 && e1.GetEnd() == p2) || (e1.GetStart() == p2 && e1.GetEnd() == p1))
                    {
                        if ((e2.GetStart() == p2 && e2.GetEnd() == vertex) || (e2.GetStart() == vertex && e2.GetEnd() == p2))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
