namespace SolverLibrary.Model
{
    public enum TrafficLightStatus { STOP, PASSING }

    public class TrafficLightVertex : Vertex
    {
        private TrafficLightStatus status;

        public TrafficLightVertex(int id) : base(vertexType.TRAFFIC, id)
        {
            status = TrafficLightStatus.STOP;
        }

        public void SetEdges(Edge edge1, Edge edge2)
        {
            base.edgeConnections.Clear();
            base.edgeConnections.Add(new Tuple<Edge, Edge>(edge1, edge2));
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
