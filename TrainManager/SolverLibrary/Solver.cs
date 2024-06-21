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
using SolverLibrary.Model.PlanUnit;
using SolverLibrary.Algorithms;

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
            PathCalculator.calculatePathsFromIn(inputVertices, platformsWithDirection, pathsStartFromVertex);

            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatfrom = new();
            HashSet<OutputVertex> outputVertexes = new();
            // Calculate pathes that start from platform and end in OutputVertex
            PathCalculator.calculatePathsFromPlatfroms(platformsWithDirection, outputVertexes, pathsStartFromPlatfrom);

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
                            Edge edgePlat = HelpFunctions.findEdge(platform.Item1, platform.Item2);
                            if (edgePlat == null)
                            {
                                continue;
                            }

                            int travelTime = (pathFromIn.length + pathFromPlat.length + train.GetSpeed() - 1) / train.GetSpeed(); 
                            if ( HelpFunctions.checkPlatfrom(edgePlat, train.GetTrainType(), train.GetLength()) &&
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

            PathTimeBlocker timeBlocker = new(timeInaccuracy);

            foreach (var train1 in dictSchedule.Keys)
            {
                for (int plat1 = 0; plat1 < platformsCnt; ++plat1)
                {
                    if (trainConditionPaths[trainId[train1], plat1].Item1 == null ||
                        trainConditionPaths[trainId[train1], plat1].Item2 == null)
                    {
                        continue;
                    }
                    var edgesTimeBlocks1 = timeBlocker.calculateEdgesTimeBlocking(
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
                            var edgesTimeBlocks2 = timeBlocker.calculateEdgesTimeBlocking(
                                train2, dictSchedule[train2],
                                trainConditionPaths[trainId[train2], plat2].Item1,
                                trainConditionPaths[trainId[train2], plat2].Item2);

                            foreach (var edge in edgesTimeBlocks1.Keys)
                            {
                                if (edgesTimeBlocks2.ContainsKey(edge)) {
                                    if (HelpFunctions.hasListsOfIntervalsIntrsection(edgesTimeBlocks1[edge], edgesTimeBlocks2[edge]))
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
                        plan.AddTrainWithPlatform(train, dictSchedule[train], HelpFunctions.findEdge(tmp[tmp.Count - 1], tmp[tmp.Count - 2]));
                    }
                }
            }
            
            foreach (Train train in dictSchedule.Keys)
            {
                var trainSchedule = dictSchedule[train];
                int platform = -1;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (solver.Value(trainGoesThroughPlatf[trainId[train], j]) == 1)
                    {
                        platform = j; break;  
                    }
                }
                var vertices = trainConditionPaths[trainId[train], platform].Item1.GetVertices();
                int t = (trainConditionPaths[trainId[train], platform].Item1.length + train.GetSpeed() - 1) / train.GetSpeed();
                for (int i = 1; i + 1 < vertices.Count; ++i)
                {
                    if (vertices[i].GetVertexType() == VertexType.TRAFFIC)
                    {
                        TrafficLightPlanUnit planUnit = new((TrafficLightVertex)vertices[i],
                            trainSchedule.GetTimeArrival() - timeInaccuracy,
                            trainSchedule.GetTimeArrival() + t + timeInaccuracy,
                            TrafficLightStatus.PASSING);
                        plan.AddTrafficLightPlanUnit(planUnit);
                    }
                    if (vertices[i].GetVertexType() == VertexType.SWITCH)
                    {
                        SwitchStatus switchStatus = SwitchStatus.PASSINGCON2;
                        SwitchVertex v = (SwitchVertex)vertices[i];
                        if (HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item1, new(vertices[i - 1], vertices[i])) &&
                            HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item2, new(vertices[i + 1], vertices[i])))
                        {
                            switchStatus = SwitchStatus.PASSINGCON1;
                        }
                        if (HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item1, new(vertices[i + 1], vertices[i])) &&
                            HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item2, new(vertices[i - 1], vertices[i])))
                        {
                            switchStatus = SwitchStatus.PASSINGCON1;
                        }
                        SwitchPlanUnit planUnit = new(v,
                            trainSchedule.GetTimeArrival() - timeInaccuracy,
                            trainSchedule.GetTimeArrival() + t + timeInaccuracy,
                            switchStatus);
                        plan.AddSwitchPlanUnit(planUnit);
                    }
                }
            }
            Dictionary<TrafficLightVertex, int> pastTime = new();
            List<TrafficLightPlanUnit> planUnits = new();
            foreach (var unit in plan.GetTrafficLightPlanUnits())
            {
                TrafficLightVertex v = unit.GetVertex();
                if (pastTime.ContainsKey(v))
                {
                    if (unit.GetBeginTime() - 1 >= pastTime[v] + 1)
                    {
                        planUnits.Add(new(v, pastTime[v] + 1, unit.GetBeginTime() - 1, TrafficLightStatus.STOP));
                    }
                } else
                {
                    planUnits.Add(new(v, int.MinValue, unit.GetBeginTime() - 1, TrafficLightStatus.STOP));
                }
                pastTime[v] = unit.GetEndTime();
            }
            foreach (var trLight in pastTime.Keys)
            {
                planUnits.Add(new(trLight, pastTime[trLight] + 1, int.MaxValue, TrafficLightStatus.STOP));
            }
            foreach (var unit in planUnits)
            {
                plan.AddTrafficLightPlanUnit(unit);
            }

            return plan;
        }
    }
}
