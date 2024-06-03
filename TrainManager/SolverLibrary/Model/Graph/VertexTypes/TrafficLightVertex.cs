using SolverLibrary.Model.Graph;

namespace SolverLibrary.Model.Graph.VertexTypes
{
    public enum TrafficLightStatus { STOP, PASSING }

    public class TrafficLightVertex : Vertex
    {
        private TrafficLightStatus status;

        public TrafficLightVertex(int id) : base(VertexType.TRAFFIC, id)
        {
            status = TrafficLightStatus.STOP;
        }

        public void SetEdges(Edge edge1, Edge edge2)
        {
            edgeConnections.Clear();
            edgeConnections.Add(new Tuple<Edge?, Edge?>(edge1, edge2));
        }

        public TrafficLightStatus GetStatus()
        {
            return status;
        }

        public void ChangeStatus()
        {
            if (status == TrafficLightStatus.STOP)
            {
                status = TrafficLightStatus.PASSING;
            }
            else
            {
                status = TrafficLightStatus.STOP;
            }
        }
    }
}
