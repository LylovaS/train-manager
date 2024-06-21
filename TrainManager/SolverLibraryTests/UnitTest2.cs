using SolverLibrary;
using SolverLibrary.JsonDoc;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;

namespace SolverLibraryTests
{
    [TestClass]
    public class SolverTest
    {
        [TestMethod]
        public void SolveTest()
        {
            StationGraph graph =  JsonParser.LoadJsonStationGraph("./test_files/station_topology2.json");
            Assert.IsTrue(graph.CheckStationGraph());
            TrainSchedule schedule = JsonParser.LoadJsonTrainSchedule("./test_files/schedule2.json", graph);

            Solver solver = new(graph, 5);
            var workPlan = solver.CalculateWorkPlan(schedule);
            Assert.AreEqual(schedule.GetSchedule().Count(), workPlan.TrainPlatforms.Count);
        }
    }
}