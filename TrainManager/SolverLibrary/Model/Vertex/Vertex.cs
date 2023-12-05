namespace SolverLibrary.Model { 
    public enum vertexType { TRAFFIC, SWITCH, CONNECTION, INPUT, OUTPUT, DEADEND }
    public class Vertex
    {
        private readonly vertexType type;
        private readonly int id;
        private bool blocked;
        protected List<Tuple<Edge, Edge>> edgeConnections = new List<Tuple<Edge, Edge>>();

        protected Vertex(vertexType type, int id)
        {
            this.type = type;
            this.id = id;
        }
        public vertexType GetVertexType()
        {
            return type;
        }
        public List<Tuple<Edge, Edge>> GetEdgeConnections() { return edgeConnections; }
        public bool IsBlocked() { return blocked; }
        public void Block() { blocked = true; }
        public void Unblock() { blocked = false; }
    }
}
