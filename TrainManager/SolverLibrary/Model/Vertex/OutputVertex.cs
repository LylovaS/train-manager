

namespace SolverLibrary.Model
{
    public class OutputVertex : Vertex
    {
        public OutputVertex(int id) : base(vertexType.OUTPUT, id) { }
        public void SetEdge(Edge edge)
        {
            base.edgeConnections.Clear();
            base.edgeConnections.Add(new Tuple<Edge, Edge>(null, edge));
        }
    }
}
