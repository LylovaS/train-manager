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
    }
}
