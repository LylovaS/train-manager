
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.Graph;
using System.Data;
using SolverLibrary.Model.TrainInfo;

namespace SolverLibrary.Algorithms
{
    public static class HelpFunctions
    {
        internal static Edge? findEdge(Vertex start, Vertex end)
        {
            foreach (var i in start.GetEdgeConnections())
            {
                if (i.Item1 != null && hasEdgeThatEndings(i.Item1, new(start, end)))
                {
                    return i.Item1;
                }
                if (i.Item2 != null && hasEdgeThatEndings(i.Item2, new(start, end)))
                {
                    return i.Item2;
                }
            }
            return null;
        }

        internal static bool hasEdgeThatEndings(Edge edge, Tuple<Vertex, Vertex> endings)
        {
            return (edge.GetEnd() == endings.Item1 && edge.GetStart() == endings.Item2) ||
                (edge.GetEnd() == endings.Item2 && edge.GetStart() == endings.Item1);
        }

        internal static bool hasIntervalsIntersection(Tuple<int, int> t1, Tuple<int, int> t2)
        {
            return Math.Min(t1.Item2, t2.Item2) >= Math.Max(t1.Item1, t2.Item1);
        }

        internal static bool hasListsOfIntervalsIntrsection(List<Tuple<int, int>> l1, List<Tuple<int, int>> l2)
        {
            for (int i = 0; i < l1.Count; ++i)
            {
                for (int j = 0; j < l2.Count; ++j)
                {
                    if (hasIntervalsIntersection(l1[i], l2[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool checkPlatfrom(Edge edge, TrainType trainType, int trainLen)
        {
            return (trainType == TrainType.NONE || edge.GetEdgeType() == trainType) &&
                edge.GetLength() >= trainLen;
        }
    }
}