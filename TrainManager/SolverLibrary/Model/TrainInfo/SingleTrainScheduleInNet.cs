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
        public Train train;
        public InputVertex start;
        public int startTime;

        public SingleTrainScheduleInNet(Train train, InputVertex start, int startTime)
        {
            this.train = train;
            this.start = start;
            this.startTime = startTime;
        }
    }
}
