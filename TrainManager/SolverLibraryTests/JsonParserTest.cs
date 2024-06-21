using SolverLibrary.JsonDoc;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.TrainInfo;
using SolverLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolverLibrary;

namespace SolverLibraryTests
{
    [TestClass]
    public class JsonParserTest
    {
        [TestMethod]
        public void SimpleTrainsJsonTest()
        {
            List<Train>? trains = null;
            trains = JsonParser.LoadJsonTrains("./test_files/trains.json");
            Assert.IsNotNull(trains);
            Assert.AreEqual(1, trains.Count());
            Assert.AreEqual(10, trains[0].GetLength());
            Assert.AreEqual(1, trains[0].GetSpeed());
            Assert.AreEqual(TrainType.NONE, trains[0].GetTrainType());
        }
        [TestMethod]
        public void SimpleStationGraphJsonTest()
        {
            StationGraph graph = null;
            graph = JsonParser.LoadJsonStationGraph("./test_files/station_topology.json");
            Assert.IsNotNull(graph);
            Assert.IsTrue(graph.CheckStationGraph());
            Assert.AreEqual(2, graph.GetVertices().Count());
            Assert.AreEqual(VertexType.INPUT, graph.GetVertices().ToList().Find((Vertex v) => { return v.getId().Equals(0); }).GetVertexType());
            Assert.AreEqual(VertexType.OUTPUT, graph.GetVertices().ToList().Find((Vertex v) => { return v.getId().Equals(1); }).GetVertexType());
            Assert.AreEqual(1, graph.GetInputVertices().Count());
            Assert.AreEqual(1, graph.GetOutputVertices().Count());
        }
        [TestMethod]
        public void SimpleScheduleJsonTest()
        {
            StationGraph graph = null;
            graph = JsonParser.LoadJsonStationGraph("./test_files/station_topology.json");

            TrainSchedule schedule = null;
            schedule = JsonParser.LoadJsonTrainSchedule("./test_files/schedule.json", graph);
            Assert.IsNotNull(schedule);

            /*Assert.IsNotNull(singleTrainSchedule);
            Assert.AreEqual(0, schedule[0].GetSingleTrainSchedule().GetTimeArrival());
            Assert.AreEqual(5, schedule[0].GetSingleTrainSchedule().GetTimeDeparture());
            Assert.AreEqual(4, schedule[0].GetSingleTrainSchedule().GetTimeStop());*/
        }
        [TestMethod]
        public void SimpleStationWorkPlanTest()
        {
            StationGraph graph = JsonParser.LoadJsonStationGraph("./test_files/station_topology2.json");
            TrainSchedule schedule = JsonParser.LoadJsonTrainSchedule("./test_files/schedule2.json", graph);

            Solver solver = new(graph, 5);
            var workPlan = solver.CalculateWorkPlan(schedule);
            var trainPlatforms = workPlan.TrainPlatforms;
            JsonParser.SaveJsonStationWorkPlan("./SAVED_station_work_plan.json", workPlan);

            StationWorkPlan parsedWorkPlan = JsonParser.LoadJsonStationWorkPlan("./SAVED_station_work_plan.json", graph);
            var parsedTrainPlatforms = parsedWorkPlan.TrainPlatforms;
            for (int i = 0; i < parsedTrainPlatforms.Count; i++)
            {
                Tuple<Train, SingleTrainSchedule> solved = trainPlatforms.Keys.ElementAt(i);
                Tuple<Train, SingleTrainSchedule> parsed = parsedTrainPlatforms.Keys.ElementAt(i);

                Train train = solved.Item1;
                SingleTrainSchedule trainSchedule = solved.Item2;
                Train parsedTrain = parsed.Item1;
                SingleTrainSchedule parsedTrainSchedule = parsed.Item2;
                Assert.AreEqual(train.GetLength(), parsedTrain.GetLength());
                Assert.AreEqual(trainSchedule.GetVertexIn().getId(), parsedTrainSchedule.GetVertexIn().getId());
                Assert.AreEqual(trainSchedule.GetVertexOut().getId(), parsedTrainSchedule.GetVertexOut().getId());
                Assert.AreEqual(train.GetTrainType(), parsedTrain.GetTrainType());
                Assert.AreEqual(trainSchedule.GetTimeArrival(), parsedTrainSchedule.GetTimeArrival());
                Assert.AreEqual(trainSchedule.GetTimeDeparture(), parsedTrainSchedule.GetTimeDeparture());
                Assert.AreEqual(trainPlatforms[solved].GetStart()?.getId(), parsedTrainPlatforms[parsed].GetStart()?.getId());
                Assert.AreEqual(trainPlatforms[solved].GetEnd()?.getId(), parsedTrainPlatforms[parsed].GetEnd()?.getId());

            }
        }
    }
}
