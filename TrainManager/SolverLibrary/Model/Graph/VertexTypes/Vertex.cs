using Newtonsoft.Json;
using SolverLibrary.Model.Graph;

namespace SolverLibrary.Model.Graph.VertexTypes
{
    public enum VertexType { TRAFFIC, SWITCH, CONNECTION, INPUT, OUTPUT, DEADEND };
    public class Vertex
    {
        [JsonProperty(PropertyName = "vertexType",  Order = 2)]
        private readonly VertexType type;
        [JsonProperty(Order = 1)]
        private readonly int id;
        private bool blocked;
        [JsonProperty(Order = 3)]
        protected List<Tuple<Edge?, Edge?>> edgeConnections = new();
        private bool hidden = false;

        [JsonConstructor]
        protected Vertex(VertexType type, int id)
        {
            this.type = type;
            this.id = id;
        }
        public VertexType GetVertexType()
        {
            return type;
        }
        public List<Tuple<Edge?, Edge?>> GetEdgeConnections() { 
            if (hidden) { return new(); }
            return edgeConnections; 
        }
        public void HideConnections() { hidden = true; }
        public void ShowConnections() { hidden = false; }
        public bool IsBlocked() { return blocked; }
        public void Block() { blocked = true; }
        public void Unblock() { blocked = false; }
        public int getId() { return id; }
    }
}
