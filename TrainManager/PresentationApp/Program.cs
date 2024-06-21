using SolverLibrary.JsonDoc;
using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using SolverLibrary;
using System;
using System.Security.Cryptography;
using SolverLibrary.Model.TrainInfo;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            StationGraph graph = JsonParser.LoadJsonStationGraph("./station_topology.json");
            JsonParser.SaveJsonStationGraph("./SAVED_station_topology.json", graph);

            TrainSchedule schedule = JsonParser.LoadJsonTrainSchedule("./train_schedule.json", graph);
            JsonParser.SaveJsonTrainSchedule("./SAVED_train_schedule.json", schedule);

            Solver solver = new(graph, 5);
            var workPlan = solver.CalculateWorkPlan(schedule);
            var trainPlatforms = workPlan.TrainPlatforms;
            foreach (Tuple<Train, SingleTrainSchedule> i in  trainPlatforms.Keys) 
            {
                Train train = i.Item1;
                SingleTrainSchedule trainSchedule = i.Item2;
                Console.WriteLine($"train(length={train.GetLength()}, Input={trainSchedule.GetVertexIn().getId()}," +
                    $" Output={trainSchedule.GetVertexOut().getId()}, type={train.GetTrainType()}, " +
                    $"timeArrival={trainSchedule.GetTimeArrival()}, timeDeparture={trainSchedule.GetTimeDeparture()})" +
                    $" stops on platfrom Edge(start={trainPlatforms[i].GetStart().getId()}, start={trainPlatforms[i].GetEnd().getId()})");
            }
            JsonParser.SaveJsonStationWorkPlan("./SAVED_station_work_plan.json", workPlan);
        }
    }
}