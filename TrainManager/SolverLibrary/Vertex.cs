using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary
{
    public enum vertexType { TRAFFIC, SWITCH, CONNECTION, INPUT, OUTPUT, DEADEND }
    public class Vertex
    {
        private vertexType type;
        private bool blocked;

        protected Vertex(vertexType type)
        {
            this.type = type;
        }

        public vertexType getVertexType()
        {
            return type;
        }

        public bool isBlocked() { return blocked; }

        public void block() { blocked = true; }

        public void unblock() { blocked = false; }
    }

    public enum TrafficLightStatus { STOP, PASSING }

    public class TrafficLightVertex : Vertex
    {
        private TrafficLightStatus status;
        private Edge[] edges = new Edge[2];

        public TrafficLightVertex() : base(vertexType.TRAFFIC)
        {
            this.status = TrafficLightStatus.STOP;
        }

        public void setEdges(Edge edge1, Edge edge2)
        {
            edges[0] = edge1;
            edges[1] = edge2;
        }

        public Edge[] getEdges()
        {
            return edges;
        }
    }

}
