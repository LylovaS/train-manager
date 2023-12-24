using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class SingleTrainSchedule
    {
        private int timeArrival, timeDeparture, timeStop;
        private InputVertex vertexIn;
        private OutputVertex vertexOut;

        public SingleTrainSchedule(int timeArrival, int timeDeparture, int timeStop, InputVertex vertexIn, OutputVertex vertexOut)
        {
            SetTimeArrival(timeArrival);
            SetTimeDeparture(timeDeparture);
            SetTimeStop(timeStop);
            this.vertexIn = vertexIn;
            this.vertexOut = vertexOut;
        }
        public int GetTimeArrival() { return timeArrival; }
        public void SetTimeArrival(int timeArrival)
        {
            if (timeArrival < 0)
            {
                throw new ArgumentException("All time stamps must be non-negative.");
            }
            CheckTimeStamps();
            this.timeArrival = timeArrival;
        }
        public int GetTimeDeparture() { return timeDeparture; }
        public void SetTimeDeparture(int timeDeparture)
        {
            if (timeDeparture < 0)
            {
                throw new ArgumentException("All time stamps must be non-negative.");
            }
            CheckTimeStamps();
            this.timeDeparture = timeDeparture;
        }
        public int GetTimeStop() { return timeStop; }
        public void SetTimeStop(int timeStop)
        {
            if (timeStop < 0)
            {
                throw new ArgumentException("All time stamps must be non-negative.");
            }
            CheckTimeStamps();
            this.timeStop = timeStop;
        }
        public InputVertex GetVertexIn() { return vertexIn; }
        public void SetVertexIn(InputVertex vertexIn) { this.vertexIn = vertexIn; }
        public OutputVertex GetVertexOut() { return vertexOut; }
        public void SetVertexOut(OutputVertex vertexOut) { this.vertexOut = vertexOut; }

        private void CheckTimeStamps() 
        {
            if (timeDeparture < timeArrival + timeStop)
            {
                throw new ArgumentException("Impossible to depart from the station in time.");
            }
        }
    }
}
