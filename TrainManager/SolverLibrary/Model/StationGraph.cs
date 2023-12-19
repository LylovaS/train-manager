using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class StationGraph
    {
        private HashSet<Vertex> vertices;
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
                edges.Add(t.Item1);
                edges.Add(t.Item2);
            }
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
            Vertex start = verticesSet.First();
            verticesSet.Add(start);
            edgesSet.Clear();
            Edge edge = start.GetEdgeConnections().First().Item1;
            edgesSet.Add(start.GetEdgeConnections().First().Item1);
            return FindAllVertices(start, edge, verticesSet, edgesSet);
        }

        public bool FindAllVertices(Vertex vertex, Edge edge, HashSet<Vertex> acc, HashSet<Edge> path)
        {
            if (acc.Count == vertices.Count)
            {
                return true;
            }
            Tuple<Edge, Edge>[] tuples = vertex.GetEdgeConnections().ToArray();
            foreach (Tuple<Edge, Edge> t in tuples)
            {
                Edge? nextEdge = null;
                if (t.Item1 != edge && t.Item2 == edge)
                {
                    nextEdge = t.Item1;
                }
                else if (t.Item1 == edge && t.Item2 != edge)
                {
                    nextEdge = t.Item2;
                }
                if (nextEdge != null && !path.Contains(nextEdge))
                {
                    Vertex? nextVertex = nextEdge.GetVertexOut(vertex);
                    if (nextVertex != null && !acc.Contains(nextVertex))
                    {
                        acc.Add(nextVertex);
                        path.Add(nextEdge);
                        FindAllVertices(nextVertex, nextEdge, acc, path);
                    }
                }
            }
            return false;
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

        public HashSet<InputVertex> GetInputVertices()
        {
            return inputVertices;
        }

        public HashSet<OutputVertex> GetOutputVertices()
        {
            return outputVertices;
        }
    }
}
