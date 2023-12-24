using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;

namespace SolverLibraryTests
{
    [TestClass]
    public class VertexTest
    {
        [TestMethod]
        public void TrafficVertex()
        {
            TrafficLightVertex v = new TrafficLightVertex(10);
            Vertex v2 = v;
            Assert.AreEqual(v.GetVertexType(), VertexType.TRAFFIC);
            Assert.AreEqual(v2.GetVertexType(), VertexType.TRAFFIC);
            Assert.IsFalse(v2.IsBlocked());
            Assert.IsFalse(v.IsBlocked());
            v2.Block();
            Assert.IsTrue(v2.IsBlocked());
            Assert.IsTrue(v.IsBlocked());
            v2.Unblock();
            Assert.IsFalse(v2.IsBlocked());
            Assert.IsFalse(v.IsBlocked());
        }
    }
    [TestClass]
    public class StationGraphTest
    {
        [TestMethod]
        public void SimpleStationTest()
        {
            StationGraph station = new StationGraph();
            InputVertex vertex = new InputVertex(0);
            OutputVertex vertex1 = new OutputVertex(1);
            Edge edge01 = new Edge(1, vertex, vertex1, TrainType.NONE);
            vertex.SetEdge(edge01);
            vertex1.SetEdge(edge01);
            station.TryAddVerticeWithEdges(vertex);
            station.TryAddVerticeWithEdges(vertex1);
            bool check = station.CheckStationGraph();
            Assert.IsTrue(check);
        }
        [TestMethod]
        public void CorrectStationTest()
        {
            StationGraph station = new StationGraph();
            InputVertex vertex = new InputVertex(0);
            SwitchVertex vertex1 = new SwitchVertex(1);
            ConnectionVertex vertex2 = new ConnectionVertex(2);
            ConnectionVertex vertex3 = new ConnectionVertex(3);
            OutputVertex vertex4 = new OutputVertex(4);
            OutputVertex vertex5 = new OutputVertex(5);
            Edge edge01 = new Edge(1, vertex, vertex1, TrainType.NONE);
            Edge edge12 = new Edge(1, vertex1, vertex2, TrainType.NONE);
            Edge edge25 = new Edge(1, vertex2, vertex5, TrainType.NONE);
            Edge edge13 = new Edge(1, vertex1, vertex3, TrainType.NONE);
            Edge edge34 = new Edge(1, vertex3, vertex4, TrainType.NONE);
            vertex.SetEdge(edge01);
            vertex1.SetEdges(edge01, edge12, edge13);
            vertex2.SetEdges(edge12, edge25);
            vertex3.SetEdges(edge13, edge34);
            vertex4.SetEdge(edge34);
            vertex5.SetEdge(edge25);

            station.TryAddVerticeWithEdges(vertex);
            station.TryAddVerticeWithEdges(vertex1);
            station.TryAddVerticeWithEdges(vertex2);
            station.TryAddVerticeWithEdges(vertex3);
            station.TryAddVerticeWithEdges(vertex4);
            station.TryAddVerticeWithEdges(vertex5);
            bool check = station.CheckStationGraph();
            Assert.IsTrue(check);
        }
        [TestMethod]
        public void IncorrectStationTest()
        {
            StationGraph station = new StationGraph();
            InputVertex vertex = new InputVertex(0);
            SwitchVertex vertex1 = new SwitchVertex(1);
            ConnectionVertex vertex2 = new ConnectionVertex(2);
            ConnectionVertex vertex3 = new ConnectionVertex(3);
            OutputVertex vertex4 = new OutputVertex(4);
            OutputVertex vertex5 = new OutputVertex(5);
            Edge edge01 = new Edge(1, vertex, vertex1, TrainType.NONE);
            Edge edge12 = new Edge(1, vertex1, vertex2, TrainType.NONE);
            Edge edge25 = new Edge(1, vertex2, vertex5, TrainType.NONE);
            Edge edge14 = new Edge(1, vertex1, vertex4, TrainType.NONE);
            vertex.SetEdge(edge01);
            vertex1.SetEdges(edge01, edge12, edge14);
            vertex2.SetEdges(edge12, edge25);
            vertex4.SetEdge(edge14);
            vertex5.SetEdge(edge25);

            station.TryAddVerticeWithEdges(vertex);
            station.TryAddVerticeWithEdges(vertex1);
            station.TryAddVerticeWithEdges(vertex2);
            station.TryAddVerticeWithEdges(vertex3);
            station.TryAddVerticeWithEdges(vertex4);
            station.TryAddVerticeWithEdges(vertex5);
            bool check = station.CheckStationGraph();
            Assert.IsFalse(check);
        }
    }
}