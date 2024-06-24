using SolverLibrary;
using SolverLibrary.JsonDoc;
using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibraryTests
{
    [TestClass]
    public class SolverInNetTest
    {
        [TestMethod]
        public void SolveTest()
        {
            StationGraph station1 = JsonParser.LoadJsonStationGraph("./test_files/station_topology2.json");
            Assert.IsTrue(station1.CheckStationGraph());
            StationGraph station2 = JsonParser.LoadJsonStationGraph("./test_files/station_topology2.json");
            Assert.IsTrue(station2.CheckStationGraph());
            OutputVertex outFromStation1 = FindVertexById(station1.GetOutputVertices(), 26);
            OutputVertex outFromStation2 = FindVertexById(station2.GetOutputVertices(), 0);
            InputVertex inToStation1 = FindVertexById(station1.GetInputVertices(), 27);
            InputVertex inToStation2 = FindVertexById(station2.GetInputVertices(), 1);
            StationNet stationNet = new StationNet();
            stationNet.AddStation(station1); stationNet.AddStation(station2);
            stationNet.AddStationsConnection(inToStation1, outFromStation2, 10000);
            stationNet.AddStationsConnection(inToStation2, outFromStation1, 10000);

            TrainSheduleInNet scheduleInNet = new TrainSheduleInNet();
            Train train1 = new Train(450, 3, TrainType.CARGO);
            Train train2 = new Train(600, 3, TrainType.PASSENGER);
            SingleTrainScheduleInNet schedule1 = new(train1, FindVertexById(station1.GetInputVertices(), 1), 0);
            SingleTrainScheduleInNet schedule2 = new(train2, FindVertexById(station1.GetInputVertices(), 1), 500);
            schedule1.AddPointInMovementPath(new(300, station1, TrainType.CARGO));
            schedule1.AddPointInMovementPath(new(0, station2, TrainType.NONE));
            schedule2.AddPointInMovementPath(new(300, station1, TrainType.PASSENGER));
            schedule2.AddPointInMovementPath(new(100, station2, TrainType.PASSENGER));
            scheduleInNet.AddSingleTrainSchedule(schedule1);
            scheduleInNet.AddSingleTrainSchedule(schedule2);

            var solver = new SolverInNet(stationNet, 10);
            var result = solver.CalculateWorkPlan(scheduleInNet);
            Assert.AreEqual(result.Plans[station1].TrainPlatforms[train1].GetEdgeType(), TrainType.CARGO);
            Assert.AreEqual(result.Plans[station1].TrainPlatforms[train2].GetEdgeType(), TrainType.PASSENGER);
            Assert.AreEqual(result.Plans[station2].TrainPlatforms[train2].GetEdgeType(), TrainType.PASSENGER);
            Assert.AreEqual(FindVertexById(station1.GetInputVertices(), 1), result.Schedules[station1][schedule1].GetVertexIn());
            Assert.AreEqual(FindVertexById(station1.GetOutputVertices(), 26), result.Schedules[station1][schedule1].GetVertexOut());
            Assert.AreEqual(FindVertexById(station2.GetInputVertices(), 1), result.Schedules[station2][schedule1].GetVertexIn());
            Assert.AreEqual(FindVertexById(station2.GetOutputVertices(), 26), result.Schedules[station2][schedule1].GetVertexOut());

            Assert.AreEqual(FindVertexById(station1.GetInputVertices(), 1), result.Schedules[station1][schedule2].GetVertexIn());
            Assert.AreEqual(FindVertexById(station1.GetOutputVertices(), 26), result.Schedules[station1][schedule2].GetVertexOut());
            Assert.AreEqual(FindVertexById(station2.GetInputVertices(), 1), result.Schedules[station2][schedule2].GetVertexIn());
            Assert.AreEqual(FindVertexById(station2.GetOutputVertices(), 26), result.Schedules[station2][schedule2].GetVertexOut());

            Console.WriteLine("Result for train1:");
            Console.WriteLine($"platform id on station1: {result.Plans[station1].TrainPlatforms[train1].getId()}");
            Console.WriteLine($"platform id on station2: {result.Plans[station2].TrainPlatforms[train1].getId()}");
            Console.WriteLine("Result for train2:");
            Console.WriteLine($"platform id on station1: {result.Plans[station1].TrainPlatforms[train2].getId()}");
            Console.WriteLine($"platform id on station2: {result.Plans[station2].TrainPlatforms[train2].getId()}");
        }

        private static OutputVertex FindVertexById(HashSet<OutputVertex> vertices, int id) {
            foreach (var vertex in vertices)
            {
                if (vertex.getId() == id) return vertex;
            }
            return null;
        }
        private static InputVertex FindVertexById(HashSet<InputVertex> vertices, int id)
        {
            foreach (var vertex in vertices)
            {
                if (vertex.getId() == id) return vertex;
            }
            return null;
        }
    }
}
