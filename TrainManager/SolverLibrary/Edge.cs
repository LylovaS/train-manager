namespace SolverLibrary
{
    public class Edge
    {
        private int length;
        private Vertex[] ends;
        private bool blocked;

        public Edge(int length, Vertex end1, Vertex end2)
        {
            this.length = length;
            Vertex[] vertices = {end1, end2};
            this.ends = vertices;
            this.blocked = false;
        }

        public bool isBlocked() { return blocked; }

        public void block() { blocked = true; }

        public void unblock() { blocked = false; }
    }
}