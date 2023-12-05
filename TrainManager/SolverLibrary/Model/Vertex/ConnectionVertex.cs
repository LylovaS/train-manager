using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class ConnectionVertex : Vertex
    {
        public ConnectionVertex(int id) : base(vertexType.CONNECTION, id) { }
        public void SetEdges(Edge edge1, Edge edge2)
        {
            base.edgeConnections.Clear();
            base.edgeConnections.Add(new Tuple<Edge, Edge>(edge1, edge2));
        }
    }
}
