using SolverLibrary.JsonDoc;
using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using SolverLibrary;
using System;
using System.Security.Cryptography;
using SolverLibrary.Model.TrainInfo;
using SolverLibrary.Model.Graph.VertexTypes;

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
            var trainPlatforms = workPlan.trainPlatforms;
            foreach (Tuple<Train, SingleTrainSchedule> i in  trainPlatforms.Keys) 
            {
                Train train = i.Item1;
                SingleTrainSchedule trainSchedule = i.Item2;
                Console.WriteLine($"train(length={train.GetLength()}, Input={trainSchedule.GetVertexIn().getId()}," +
                    $" Output={trainSchedule.GetVertexOut().getId()}, type={train.GetTrainType()}, " +
                    $"timeArrival={trainSchedule.GetTimeArrival()}, timeDeparture={trainSchedule.GetTimeDeparture()})" +
                    $" stops on platfrom Edge(start={trainPlatforms[i].GetStart().getId()}, end={trainPlatforms[i].GetEnd().getId()})");
            }
            JsonParser.SaveJsonStationWorkPlan("./SAVED_station_work_plan.json", workPlan);
            foreach (var unit in workPlan.GetSwitchPlanUnits())
            {
                Console.WriteLine($"SWITCH-VERTEX: {unit.GetVertex().getId()}, " +
                    $"STATUS: {unit.GetStatus()}, " +
                    $"START TIME: {unit.GetBeginTime()}, END TIME: {unit.GetEndTime()}");
            }
            foreach (var unit in workPlan.GetTrafficLightPlanUnits())
            {
                Console.WriteLine($"TRAFFIC_LIGHT-VERTEX: {unit.GetVertex().getId()}, " +
                    $"STATUS: {unit.GetStatus()}, " +
                    $"START TIME: {unit.GetBeginTime()}, END TIME: {unit.GetEndTime()}");
            }



            //Edge? edge1 = graph.GetEdges().Where(e => e.getId() == 10).FirstOrDefault();
            //edge1.GetEnd().Block();

            /*Edge? edge3 = graph.GetEdges().Where(e => e.getId() == 24).FirstOrDefault();
            Train arrivedTrain = schedule.GetSchedule().Keys.Where(t => t.GetTrainType() == TrainType.PASSENGER).FirstOrDefault();
            Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>> arrivedTrainPos = new Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>>();
            arrivedTrainPos.Add(
                arrivedTrain, 
                new(new(edge3.GetEnd(), edge3.GetStart()), 151));*/
            Edge? edge3 = graph.GetEdges().Where(e => e.getId() == 12).FirstOrDefault();
            Train arrivedTrain = schedule.GetSchedule().Keys.Where(t => t.GetTrainType() == TrainType.PASSENGER).FirstOrDefault();
            Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>> arrivedTrainPos = new Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>>();
            arrivedTrainPos.Add(
                arrivedTrain,
                new(new(edge3.GetEnd(), edge3.GetStart()), 2310));
            Dictionary<Train, bool> passedStopPlatform = new Dictionary<Train, bool>();
            passedStopPlatform.Add(arrivedTrain, true);
            SwitchVertex? vertex = (SwitchVertex)graph.GetVertices().Where(v => v.getId() == 6).FirstOrDefault();
            //vertex.Block();
            //if (vertex.GetStatus() == SwitchStatus.PASSINGCON1) { vertex.ChangeStatus(); }
            //vertex.ChangeWorkCondition();

            //workPlan.trainPlatforms[workPlan.trainPlatforms.ElementAt(2).Key] = edge1;
            Edge? edge2 = graph.GetEdges().Where(e => e.getId() == 19).FirstOrDefault();
            workPlan.trainPlatforms[workPlan.trainPlatforms.ElementAt(1).Key] = edge2;
            //StationWorkPlan workPlan2 = solver.matchWorkplanToStation(workPlan, schedule);
            //if (solver.matchWorkplanToStation(workPlan, schedule)) { Console.WriteLine("Matching is alright!"); }
            //else {  Console.WriteLine("Matching is WRONG!"); }

            StationWorkPlan workPlan3 = solver.ReconfigureStationWorkPlan(workPlan, schedule, arrivedTrainPos, passedStopPlatform);
            //StationWorkPlan workPlan3 = solver.ReconfigureStationWorkPlan(workPlan, schedule);
            trainPlatforms = workPlan3.trainPlatforms;
            foreach (Tuple<Train, SingleTrainSchedule> i in trainPlatforms.Keys)
            {
                Train train = i.Item1;
                SingleTrainSchedule trainSchedule = i.Item2;
                Console.WriteLine($"train(length={train.GetLength()}, Input={trainSchedule.GetVertexIn().getId()}," +
                    $" Output={trainSchedule.GetVertexOut().getId()}, type={train.GetTrainType()}, " +
                    $"timeArrival={trainSchedule.GetTimeArrival()}, timeDeparture={trainSchedule.GetTimeDeparture()})" +
                    $" stops on platfrom Edge(start={trainPlatforms[i].GetStart().getId()}, end={trainPlatforms[i].GetEnd().getId()})");
            }
            foreach (var unit in workPlan3.GetSwitchPlanUnits())
            {
                Console.WriteLine($"SWITCH-VERTEX: {unit.GetVertex().getId()}, " +
                    $"STATUS: {unit.GetStatus()}, " +
                    $"START TIME: {unit.GetBeginTime()}, END TIME: {unit.GetEndTime()}");
            }
            foreach (var unit in workPlan3.GetTrafficLightPlanUnits())
            {
                Console.WriteLine($"TRAFFIC_LIGHT-VERTEX: {unit.GetVertex().getId()}, " +
                    $"STATUS: {unit.GetStatus()}, " +
                    $"START TIME: {unit.GetBeginTime()}, END TIME: {unit.GetEndTime()}");
            }
        }
    }
}