﻿using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.TrainInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace SolverLibrary.Algorithms
{
    internal class PathCalculator
    {
        private StationGraph station;
        internal readonly Dictionary<InputVertex, List<GraphPath>> pathsStartFromInputVertex = new();
        internal readonly HashSet<Tuple<Vertex, Vertex>> platformsWithDirection = new();
        internal readonly Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatform = new();
        internal readonly HashSet<OutputVertex> outputVertexes = new();

        public PathCalculator(StationGraph station) 
        { 
            this.station = station;
            calculatePathsFromIn(
                station.GetInputVertices(),
                this.platformsWithDirection,
                this.pathsStartFromInputVertex
                );
            calculatePathsFromPlatfroms(
                this.platformsWithDirection,
                this.outputVertexes,
                this.pathsStartFromPlatform
                );
        }

        internal bool hasPathFromInputVertexToPlatform(InputVertex inputVertex, Tuple<Vertex, Vertex> platform)
        {
            foreach(var path in pathsStartFromInputVertex[inputVertex])
            {
                var vertices = path.GetVertices();
                if (vertices.Count >= 2 && vertices[vertices.Count - 1] == platform.Item2 && vertices[vertices.Count - 2] == platform.Item1)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool hasPathFromPlatformToOutputVertex(Tuple<Vertex, Vertex> platform, OutputVertex outputVertex)
        {
            foreach (var path in pathsStartFromPlatform[platform])
            {
                var vertices = path.GetVertices();
                if (vertices.Count >= 1 && vertices[vertices.Count - 1] == outputVertex)
                {
                    return true;
                }
            }
            return false;
        }

        internal int maxLengthOfPath(
            List<InputVertex> possibleInputs, List<OutputVertex> possibleOutputs,
            int trainLength, TrainType platformType) 
        {
            int result = 0;
            foreach (InputVertex inputVertex in possibleInputs)
            {
                foreach (GraphPath pathFromInput in pathsStartFromInputVertex[inputVertex])
                {
                    var vertices = pathFromInput.GetVertices();
                    Tuple<Vertex, Vertex> platform = new(vertices[vertices.Count - 2], vertices[vertices.Count - 1]);
                    Edge platformEdge = HelpFunctions.findEdge(platform.Item1, platform.Item2);
                    if (!HelpFunctions.checkPlatfrom(platformEdge, platformType, trainLength)) { continue; }
                    foreach( var pathFromPlatfrom in pathsStartFromPlatform[platform])
                    {
                        vertices = pathFromPlatfrom.GetVertices();
                        if (!possibleOutputs.Contains(vertices[vertices.Count - 1])) { continue; }
                        result = Math.Max(result, pathFromInput.length + pathFromPlatfrom.length - platformEdge.GetLength());
                    }
                }
            }
            return result;
        }

        internal static void calculatePathsFromIn(
            HashSet<InputVertex> inputVertices,
            HashSet<Tuple<Vertex, Vertex>> platformsWithDirection,
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
                            if (connection.Item1 != null && HelpFunctions.hasEdgeThatEndings(connection.Item1, bestPos))
                            {
                                nextEdge = connection.Item2;
                            }
                            if (connection.Item2 != null && HelpFunctions.hasEdgeThatEndings(connection.Item2, bestPos))
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

        internal static void calculatePathsFromPlatfroms(
            HashSet<Tuple<Vertex, Vertex>> platformsWithDirection,
            HashSet<OutputVertex> outputVertexes,
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
                            if (connection.Item1 != null && HelpFunctions.hasEdgeThatEndings(connection.Item1, bestPos))
                            {
                                nextEdge = connection.Item2;
                            }
                            else
                            if (connection.Item2 != null && HelpFunctions.hasEdgeThatEndings(connection.Item2, bestPos))
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
                                outputVertexes.Add((OutputVertex) v);
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
    }
}
