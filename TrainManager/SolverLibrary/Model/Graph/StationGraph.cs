using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;

namespace SolverLibrary.Model.Graph
{
    public class StationGraph
    {
        [JsonProperty(Order = 1)]
        private HashSet<Vertex> vertices;
        [JsonProperty(Order = 2)]
        private HashSet<Edge> edges;
        private HashSet<InputVertex> inputVertices;
        private HashSet<OutputVertex> outputVertices;

        public StationGraph()
        {
            vertices = new HashSet<Vertex>();
            edges = new HashSet<Edge>();
            inputVertices = new HashSet<InputVertex>();
            outputVertices = new HashSet<OutputVertex>();
        }

        public bool TryAddVerticeWithEdges(Vertex vertex)
        {
            if (vertices.Contains(vertex))
            {
                return false;
            }
            vertices.Add(vertex);
            foreach (Tuple<Edge, Edge> t in vertex.GetEdgeConnections())
            {
                if (t.Item1 != null) { edges.Add(t.Item1); }
                if (t.Item2 != null) { edges.Add(t.Item2); }
            }
            if (vertex.GetVertexType().Equals(VertexType.INPUT)) { inputVertices.Add((InputVertex)vertex); }
            if (vertex.GetVertexType().Equals(VertexType.OUTPUT)) { outputVertices.Add((OutputVertex)vertex); }
            return true;
        }

        public bool TryAddVerticeWithEdges(InputVertex vertex)
        {
            if (TryAddVerticeWithEdges(vertex as Vertex))
            {
                inputVertices.Add(vertex);
                return true;
            }
            return false;
        }

        public bool TryAddVerticeWithEdges(OutputVertex vertex)
        {
            if (TryAddVerticeWithEdges(vertex as Vertex))
            {
                outputVertices.Add(vertex);
                return true;
            }
            return false;
        }

        public bool CheckStationGraph()
        {
            foreach (Edge e in edges)
            {
                if (e.GetStart == e.GetEnd)
                {
                    return false;
                }
            }

            HashSet<Vertex> verticesSet = new HashSet<Vertex>();
            HashSet<Edge> edgesSet = new HashSet<Edge>();
            verticesSet.Clear();
            Vertex start = vertices.First();
            edgesSet.Clear();
            Edge edge = start.GetEdgeConnections().First().Item1;
            if (edge == null)
            {
                edge = start.GetEdgeConnections().First().Item2;
            }
            FindAllVertices(start, null, verticesSet, edgesSet);
            return verticesSet.Count == vertices.Count;
        }

        public void FindAllVertices(Vertex vertex, Edge? edge, HashSet<Vertex> acc, HashSet<Edge> path)
        {
            if (acc.Contains(vertex))
            {
                return;
            }
            acc.Add(vertex);
            Tuple<Edge, Edge>[] tuples = vertex.GetEdgeConnections().ToArray();
            foreach (Tuple<Edge, Edge> t in tuples)
            {
                Vertex? next_vertex = null;
                if (t.Item1 != null) {
                    next_vertex = t.Item1.GetStart();
                    FindAllVertices(next_vertex, edge, acc, path);
                    next_vertex = t.Item1.GetEnd();
                    FindAllVertices(next_vertex, edge, acc, path);
                }
                if (t.Item2 != null)
                {
                    next_vertex = t.Item2.GetStart();
                    FindAllVertices(next_vertex, edge, acc, path);
                    next_vertex = t.Item2.GetEnd();
                    FindAllVertices(next_vertex, edge, acc, path);
                }
            }
        }

        public List<Edge> FindEdgesWithType(TrainType type)
        {
            List<Edge> result = new List<Edge>();
            foreach (Edge edge in edges)
            {
                if (edge.GetEdgeType() == type)
                {
                    result.Add(edge);
                }
            }
            return result;
        }

        public HashSet<TrainType> GetEdgeTypes()
        {
            HashSet<TrainType> result = new HashSet<TrainType>();
            foreach (Edge edge in edges)
            {
                result.Add(edge.GetEdgeType());
            }
            return result;
        }

        public HashSet<Vertex> GetVertices() { return vertices; }
        public HashSet<Edge> GetEdges() { return edges; }
        public HashSet<InputVertex> GetInputVertices() { return inputVertices; }
        public HashSet<OutputVertex> GetOutputVertices() { return outputVertices; }
    }
}
