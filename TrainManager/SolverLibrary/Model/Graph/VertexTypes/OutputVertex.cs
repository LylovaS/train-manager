using SolverLibrary.Model.Graph;

namespace SolverLibrary.Model.Graph.VertexTypes
{
    public class OutputVertex : Vertex
    {
        public OutputVertex(int id) : base(VertexType.OUTPUT, id) { }
        public void SetEdge(Edge edge)
        {
            edgeConnections.Clear();
            edgeConnections.Add(new Tuple<Edge, Edge>(null, edge));
        }
    }
}
