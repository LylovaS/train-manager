using SolverLibrary.Model.Graph.VertexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model.PlanUnit
{
    public class TrafficLightPlanUnit : IComparable<TrafficLightPlanUnit>
    {
        private int BeginTime, EndTime;
        private TrafficLightStatus StatusOfSwitch;
        private TrafficLightVertex Vertex;

        public TrafficLightPlanUnit(TrafficLightVertex vertex, int beginTime, int endTime, TrafficLightStatus status)
        {
            this.Vertex = vertex;
            this.BeginTime = beginTime;
            this.EndTime = endTime;
            this.StatusOfSwitch = status;
        }

        public TrafficLightVertex GetVertex() { return Vertex; }
        public TrafficLightStatus GetStatus() { return StatusOfSwitch; }
        public int GetBeginTime() { return BeginTime; }
        public int GetEndTime() { return EndTime; }

        public int CompareTo(TrafficLightPlanUnit? otherUnit)
        {
            if (otherUnit == null)
            {
                return 1;
            }
            return this.BeginTime.CompareTo(otherUnit.BeginTime);
        }
    }
}
