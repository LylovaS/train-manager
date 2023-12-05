using SolverLibrary.Model;

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
            Assert.AreEqual(v.GetVertexType(), vertexType.TRAFFIC);
            Assert.AreEqual(v2.GetVertexType(), vertexType.TRAFFIC);
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
}