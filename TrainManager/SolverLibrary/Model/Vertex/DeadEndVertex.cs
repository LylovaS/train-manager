using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class DeadEndVertex : Vertex
    {
        public DeadEndVertex(int id) : base(vertexType.DEADEND, id) { }
        public void SetEdge(Edge edge)
        {
            base.edgeConnections.Clear();
            base.edgeConnections.Add(new Tuple<Edge, Edge>(null, edge));
        }
    }
}
