using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Algorithms
{
    internal class GraphPath
    {
        private List<Vertex> vertices = new();
        public int length = 0;
        public int id = -1;
        public GraphPath(Vertex start)
        {
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
                    if (connection.Item1 != null && HelpFunctions.hasEdgeThatEndings(connection.Item1, t))
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
                        return HelpFunctions.hasEdgeThatEndings(e2, new(p2, vertex));
                    }
                    if (e2 == null)
                    {
                        return HelpFunctions.hasEdgeThatEndings(e1, new(p2, vertex));
                    }
                    return HelpFunctions.hasEdgeThatEndings(e1, new(p2, vertex)) || HelpFunctions.hasEdgeThatEndings(e2, new(p2, vertex));
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

