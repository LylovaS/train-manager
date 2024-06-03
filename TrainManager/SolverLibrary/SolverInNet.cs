using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary
{
    public class SolverInNet
    {
        private StationNet stationNet;
        private int timeInaccuracy;

        public SolverInNet(StationNet stationNet, int timeInaccuracy)
        {
            this.timeInaccuracy = timeInaccuracy;
            this.stationNet = stationNet;
        }
    }
}
