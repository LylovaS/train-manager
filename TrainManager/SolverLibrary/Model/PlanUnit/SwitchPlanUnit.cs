using SolverLibrary.Model.Graph.VertexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model.PlanUnit
{
    public class SwitchPlanUnit : IComparable<SwitchPlanUnit>
    {
        private int BeginTime, EndTime;
        private SwitchStatus StatusOfSwitch;
        private SwitchVertex Vertex;

        public SwitchPlanUnit(SwitchVertex vertex, int beginTime, int endTime, SwitchStatus status) {
            this.Vertex = vertex;
            this.BeginTime = beginTime;
            this.EndTime = endTime;
            this.StatusOfSwitch = status;
        }

        public SwitchVertex GetVertex() { return Vertex; }
        public SwitchStatus GetStatus() {  return StatusOfSwitch; }
        public int GetBeginTime() {  return BeginTime; }
        public int GetEndTime() { return EndTime;}

        public int CompareTo(SwitchPlanUnit? otherUnit)
        {
            if (otherUnit == null)
            {
                return 1;
            }
            return this.BeginTime.CompareTo(otherUnit.BeginTime);
        }
    }
}
