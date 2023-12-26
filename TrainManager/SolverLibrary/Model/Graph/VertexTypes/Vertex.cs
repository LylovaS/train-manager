using SolverLibrary.Model.Graph;

namespace SolverLibrary.Model.Graph.VertexTypes
{
    public enum VertexType { TRAFFIC, SWITCH, CONNECTION, INPUT, OUTPUT, DEADEND };
    public class Vertex
    {
        private readonly VertexType type;
        private readonly int id;
        private bool blocked;
        protected List<Tuple<Edge?, Edge?>> edgeConnections = new();

        protected Vertex(VertexType type, int id)
        {
            this.type = type;
            this.id = id;
        }
        public VertexType GetVertexType()
        {
            return type;
        }
        public List<Tuple<Edge?, Edge?>> GetEdgeConnections() { return edgeConnections; }
        public bool IsBlocked() { return blocked; }
        public void Block() { blocked = true; }
        public void Unblock() { blocked = false; }
        public int getId() { return id; }
    }
}
