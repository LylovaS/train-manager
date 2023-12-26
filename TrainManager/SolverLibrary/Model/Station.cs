using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolverLibrary.Model.Graph.VertexTypes;

namespace SolverLibrary.Model
{
    public class Station
    {
        private List<InputVertex> inputs = new List<InputVertex>();
        public List<OutputVertex> outputs = new List<OutputVertex>();
        public List<DeadEndVertex> deadEndVertices = new List<DeadEndVertex>();
        
        public Station() { }
        public void AddInputVertex(InputVertex v) {  inputs.Add(v); }
        public void CalculateAchivableComponents()
        {
            outputs.Clear();
            deadEndVertices.Clear();

            HashSet<Vertex> usedVertices = new HashSet<Vertex>();
            Queue<Vertex> queue = new Queue<Vertex>(inputs);
            while (queue.Count > 0) 
            {  
                Vertex v = queue.Dequeue();
                if (usedVertices.Contains(v))
                {
                    continue;
                }
                usedVertices.Add(v);
                foreach (var i in v.GetEdgeConnections())
                {
                    if (i.Item1 != null)
                    {
                        AddVertexInCalc(i.Item1.GetStart(), usedVertices, queue);
                        AddVertexInCalc(i.Item1.GetEnd(), usedVertices, queue);
                    }
                    if (i.Item2 != null)
                    {
                        AddVertexInCalc(i.Item2.GetStart(), usedVertices, queue);
                        AddVertexInCalc(i.Item2.GetEnd(), usedVertices, queue);
                    }
                }
            }
        }

        private void AddVertexInCalc(Vertex v, HashSet<Vertex> usedVertices, Queue<Vertex> queue) 
        {
            if (usedVertices.Contains(v))
            {
                return;
            }
            if (v.GetVertexType() == VertexType.OUTPUT)
            {
                outputs.Add((OutputVertex)v);
            }
            if (v.GetVertexType() == VertexType.DEADEND)
            {
                deadEndVertices.Add((DeadEndVertex)v);
            }
            queue.Append(v);
        }
    }
}
