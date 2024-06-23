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
        private static StationGraph graph;
        private static TrainSchedule schedule;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            graph = JsonParser.LoadJsonStationGraph("./test_files/station_topology2.json");
            schedule = JsonParser.LoadJsonTrainSchedule("./test_files/schedule2.json", graph);
        }

        [TestMethod]
        public void CalculateWorkPlanTest()
        {
            Assert.IsTrue(graph.CheckStationGraph());

            Solver solver = new(graph, 5);
            var workPlan = solver.CalculateWorkPlan(schedule);
            Assert.AreEqual(schedule.GetSchedule().Count(), workPlan.TrainPlatforms.Count);
        }

        [TestMethod]
        public void MatchWorkPlanTest()
        {
            Solver solver = new(graph, 5);
            var workPlan = solver.CalculateWorkPlan(schedule);
            Assert.IsTrue(solver.matchWorkplanToStation(workPlan, schedule));

            // swap one platform in workplan with another suitable platform
            Edge? edge1 = graph.GetEdges().Where(e => e.getId() == 10).FirstOrDefault();
            workPlan.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(2).Key] = edge1;
            Assert.AreEqual(workPlan.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(2).Key], edge1);
            Assert.IsTrue(solver.matchWorkplanToStation(workPlan, schedule));

            // swap one platform in workplan with a platform which is not suitable 
            Edge? edge2 = graph.GetEdges().Where(e => e.getId() == 19).FirstOrDefault();
            workPlan.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(1).Key] = edge2;
            Assert.AreEqual(workPlan.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(1).Key], edge2);
            Assert.IsFalse(solver.matchWorkplanToStation(workPlan, schedule));
        }

        [TestMethod]
        public void RecalculateWorkPlanTest()
        {
            Solver solver = new(graph, 5);
            var workPlan = solver.CalculateWorkPlan(schedule);

            Edge? edge1 = graph.GetEdges().Where(e => e.getId() == 10).FirstOrDefault();
            workPlan.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(2).Key] = edge1;
            Assert.AreSame(workPlan.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(2).Key], edge1);
            StationWorkPlan workPlan2 = solver.RecalculateStationWorkPlan(workPlan, schedule);
            Assert.AreSame(workPlan2.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(2).Key], edge1);

            Edge? edge2 = graph.GetEdges().Where(e => e.getId() == 19).FirstOrDefault();
            workPlan2.TrainPlatforms[workPlan2.TrainPlatforms.ElementAt(1).Key] = edge2;
            Assert.AreSame(workPlan2.TrainPlatforms[workPlan2.TrainPlatforms.ElementAt(1).Key], edge2);
            StationWorkPlan workPlan3 = solver.RecalculateStationWorkPlan(workPlan2, schedule);
            Assert.AreSame(workPlan3.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(2).Key], edge1);
            Assert.AreNotSame(workPlan3.TrainPlatforms[workPlan3.TrainPlatforms.ElementAt(1).Key], edge2);
        }

        [TestMethod]
        public void RecalculateInRealTimeTest()
        {
            Solver solver = new(graph, 5);
            var workPlan = solver.CalculateWorkPlan(schedule);

            // consider that one train is at the station and has passed its stop platform
            Edge? edge1 = graph.GetEdges().Where(e => e.getId() == 12).FirstOrDefault();
            Train arrivedTrain = schedule.GetSchedule().Keys.Where(t => t.GetTrainType() == TrainType.PASSENGER).FirstOrDefault();
            Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>> arrivedTrainPos = new Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>>();
            arrivedTrainPos.Add(
                arrivedTrain,
                new(new(edge1.GetEnd(), edge1.GetStart()), 2310));
            Dictionary<Train, bool> passedStopPlatform = new Dictionary<Train, bool>();
            passedStopPlatform.Add(arrivedTrain, true);

            Edge? edge2 = graph.GetEdges().Where(e => e.getId() == 19).FirstOrDefault();
            workPlan.TrainPlatforms[workPlan.TrainPlatforms.ElementAt(1).Key] = edge2;

            StationWorkPlan workPlan2 = solver.RecalculateStationWorkPlan(workPlan, schedule, arrivedTrainPos, passedStopPlatform);
            Assert.AreNotSame(workPlan2.TrainPlatforms[workPlan2.TrainPlatforms.ElementAt(1).Key], edge2);
            Assert.AreSame(workPlan2.TrainPlatforms[workPlan2.TrainPlatforms.ElementAt(2).Key], edge1);

            // consider that one train is at the station and has NOT passed its stop platform yet
            Edge? edge3 = graph.GetEdges().Where(e => e.getId() == 24).FirstOrDefault();
            arrivedTrainPos = new Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>>();
            arrivedTrainPos.Add(
                arrivedTrain,
                new(new(edge3.GetEnd(), edge3.GetStart()), 151));
            passedStopPlatform = new Dictionary<Train, bool>();
            passedStopPlatform.Add(arrivedTrain, false);

            // block vertex further on the way
            Edge? edgeBroken = graph.GetEdges().Where(e => e.getId() == 12).FirstOrDefault();
            edgeBroken.GetEnd().Block();
            // edge with id=10 is a platform that train will have to go through
            Edge? edge4 = graph.GetEdges().Where(e => e.getId() == 10).FirstOrDefault();
            StationWorkPlan workPlan3 = solver.RecalculateStationWorkPlan(workPlan, schedule, arrivedTrainPos, passedStopPlatform);
            Assert.AreSame(workPlan3.TrainPlatforms[workPlan3.TrainPlatforms.ElementAt(2).Key], edge4);
        }
    }
}