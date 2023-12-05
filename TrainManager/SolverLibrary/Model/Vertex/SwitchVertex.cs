namespace SolverLibrary.Model
{
    public enum SwitchStatus { PASSINGCON1, PASSINGCON2 }
    public enum SwitchWorkCondition { WORKING, FREEZED }
    public class SwitchVertex : Vertex
    {
        private SwitchStatus status = SwitchStatus.PASSINGCON1;
        private SwitchWorkCondition workCondition = SwitchWorkCondition.WORKING;
        public SwitchVertex(int id) : base(vertexType.SWITCH, id) { }
        public void SetEdges(Edge inputEdge, Edge switchEdge1, Edge switchEdge2)
        {
            base.edgeConnections.Clear();
            base.edgeConnections.Add(new Tuple<Edge, Edge>(inputEdge, switchEdge1));
            base.edgeConnections.Add(new Tuple<Edge, Edge>(inputEdge, switchEdge2));
        }
        public SwitchStatus GetStatus() { return status; }
        public void ChangeStatus()
        {
            if (status == SwitchStatus.PASSINGCON1)
            {
                status = SwitchStatus.PASSINGCON2;
            }
            else
            {
                status = SwitchStatus.PASSINGCON1;
            }
        }
        public SwitchWorkCondition GetWorkCondition() { return workCondition; }
        public void ChangeWorkCondition()
        {
            if (workCondition == SwitchWorkCondition.WORKING)
            {
                workCondition = SwitchWorkCondition.FREEZED;
            }
            else
            {
                workCondition = SwitchWorkCondition.WORKING;
            }
        }

    }
}
