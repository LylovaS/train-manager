using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolverLibrary.Model.Graph;

namespace SolverLibrary.Model.Graph.VertexTypes
{
    public class InputVertex : Vertex
    {
        public InputVertex(int id) : base(VertexType.INPUT, id) { }
        public void SetEdge(Edge edge)
        {
            edgeConnections.Clear();
            edgeConnections.Add(new Tuple<Edge, Edge>(null, edge));
        }
    }
}
