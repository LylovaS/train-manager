
namespace SolverLibrary.Model
{
    public class Edge
    {
        private int length;
        private Vertex start, end;
        private bool blocked;

        public Edge(int length, Vertex start, Vertex end)
        {
            this.length = length;
            this.start = start;
            this.end = end;
            blocked = false;
        }
        public int GetLength() { return length; }
        public void SetLength(int length) { this.length = length; }
        public Vertex GetStart() { return start; }
        public void SetStart(Vertex start) { this.start = start; }
        public Vertex GetEnd() { return end; }
        public void SetEnd(Vertex end) { this.end = end; }
        public bool IsBlocked() { return blocked; }
        public void Block() { blocked = true; }
        public void Unblock() { blocked = false; }
    }
}