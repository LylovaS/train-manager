using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class InputVertex : Vertex
    {
        public InputVertex(int id) : base(vertexType.INPUT, id) { }
        public void SetEdge(Edge edge)
        {
            base.edgeConnections.Clear();
            base.edgeConnections.Add(new Tuple<Edge, Edge>(null, edge));
        }
    }
}
