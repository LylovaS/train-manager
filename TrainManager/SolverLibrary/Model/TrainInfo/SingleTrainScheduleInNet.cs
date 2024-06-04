using SolverLibrary.Model.Graph.VertexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model.TrainInfo
{
    public class SingleTrainScheduleInNet
    {
        private Train train;
        private InputVertex start;
        private int startTime;
        private List<StopPointOfPath> movementPath = new();

        public Train Train { get => train; set => train = value; }
        public InputVertex Start { get => start; set => start = value; }
        public int StartTime { get => startTime; set => startTime = value; }
        public List<StopPointOfPath> MovementPath { get => movementPath; set => movementPath = value; }

        public SingleTrainScheduleInNet(Train train, InputVertex start, int startTime)
        {
            this.train = train;
            this.start = start;
            this.startTime = startTime;
        }

        public void AddPointInMovementPath(StopPointOfPath stopPoint) {
            movementPath.Add(stopPoint);    
        }
    }
}
