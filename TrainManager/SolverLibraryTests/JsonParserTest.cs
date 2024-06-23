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
            var trainPlatforms = workPlan.trainPlatforms;
            JsonParser.SaveJsonStationWorkPlan("./SAVED_station_work_plan.json", workPlan);

            var dictSchedule = schedule.GetSchedule();

            StationWorkPlan parsedWorkPlan = JsonParser.LoadJsonStationWorkPlan("./SAVED_station_work_plan.json", graph);
            var parsedTrainPlatforms = parsedWorkPlan.trainPlatforms;
            for (int i = 0; i < parsedTrainPlatforms.Count; i++)
            {
                Train solvedTrain = trainPlatforms.Keys.ElementAt(i);
                Train parsedTrain = parsedTrainPlatforms.Keys.ElementAt(i);

                //SingleTrainSchedule solvedSchedule = dictSchedule[solvedTrain];
                //SingleTrainSchedule parsedSchedule = dictSchedule[parsedTrain];
                Assert.AreEqual(solvedTrain.GetLength(), parsedTrain.GetLength());
                //Assert.AreEqual(solvedSchedule.GetVertexIn().getId(), parsedSchedule.GetVertexIn().getId());
                //Assert.AreEqual(solvedSchedule.GetVertexOut().getId(), parsedSchedule.GetVertexOut().getId());
                Assert.AreEqual(solvedTrain.GetTrainType(), parsedTrain.GetTrainType());
                //Assert.AreEqual(solvedSchedule.GetTimeArrival(), parsedSchedule.GetTimeArrival());
                //Assert.AreEqual(solvedSchedule.GetTimeDeparture(), parsedSchedule.GetTimeDeparture());
                Assert.AreEqual(trainPlatforms[solvedTrain].GetStart()?.getId(), parsedTrainPlatforms[parsedTrain].GetStart()?.getId());
                Assert.AreEqual(trainPlatforms[solvedTrain].GetEnd()?.getId(), parsedTrainPlatforms[parsedTrain].GetEnd()?.getId());

            }
        }
    }
}
