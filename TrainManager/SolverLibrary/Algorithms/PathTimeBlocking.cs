using SolverLibrary.Model.Graph;
using SolverLibrary.Model.TrainInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Algorithms
{
    internal class PathTimeBlocking
    {
        private int timeInaccuracy;

        public PathTimeBlocking(int timeInaccuracy)
        {
            this.timeInaccuracy = timeInaccuracy;
        }

        internal Dictionary<Edge, List<Tuple<int, int>>> calculateEdgesTimeBlocking(Train train,
            SingleTrainSchedule trainSchedule, GraphPath pathFromIn, GraphPath pathFromPlat)
        {
            Dictionary<Edge, List<Tuple<int, int>>> res = calculateEdgesTimeBlocking(train, trainSchedule.GetTimeArrival(), pathFromIn);
            var platRes = calculateEdgesTimeBlocking(train, trainSchedule.GetTimeDeparture() - (pathFromPlat.length + train.GetSpeed() - 1) / train.GetSpeed(), pathFromPlat);
            foreach (var edge in platRes.Keys)
            {
                if (res.ContainsKey(edge))
                {
                    res[edge].AddRange(platRes[edge]);
                }
                else
                {
                    res[edge] = platRes[edge];
                }
            }

            var vertices = pathFromIn.GetVertices();
            Edge platform = HelpFunctions.findEdge(vertices[vertices.Count - 2], vertices[vertices.Count - 1]);
            int timeStopBegin = trainSchedule.GetTimeArrival() + (pathFromIn.length + train.GetSpeed() - 1) / train.GetSpeed();
            int timeStopEnd = trainSchedule.GetTimeDeparture() - (pathFromPlat.length + train.GetSpeed() - 1) / train.GetSpeed();
            if (!res.ContainsKey(platform))
            {
                res[platform] = new();
            }
            res[platform].Add(new(timeStopBegin - timeInaccuracy, timeStopEnd + timeInaccuracy));
            return res;
        }

        internal Dictionary<Edge, List<Tuple<int, int>>> calculateEdgesTimeBlocking(Train train, int beginTime, GraphPath path)
        {
            Dictionary<Edge, List<Tuple<int, int>>> res = new();

            var vertices = path.GetVertices();
            for (int i = 0; i < vertices.Count - 1; ++i)
            {
                List<Tuple<int, int>> tmp = new();
                tmp.Add(new(beginTime - timeInaccuracy, beginTime + (path.length + train.GetSpeed() - 1) / train.GetSpeed() + timeInaccuracy));
                Edge e = HelpFunctions.findEdge(vertices[i], vertices[i + 1]);
                res[e] = tmp;
            }
            return res;
        }
    }
}
