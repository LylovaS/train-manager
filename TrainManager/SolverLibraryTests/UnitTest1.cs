using SolverLibrary;

namespace SolverLibraryTests
{
    [TestClass]
    public class VertexTest
    {
        [TestMethod]
        public void TrafficVertex()
        {
            TrafficLightVertex v = new TrafficLightVertex();
            Vertex v2 = v;
            Assert.AreEqual(v.getVertexType(), vertexType.TRAFFIC);
            Assert.AreEqual(v2.getVertexType(), vertexType.TRAFFIC);
            Assert.IsFalse(v2.isBlocked());
            Assert.IsFalse(v.isBlocked());
            v2.block();
            Assert.IsTrue(v2.isBlocked());
            Assert.IsTrue(v.isBlocked());
            v2.unblock();
            Assert.IsFalse(v2.isBlocked());
            Assert.IsFalse(v.isBlocked());
        }
    }
}