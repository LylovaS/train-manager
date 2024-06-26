﻿using SolverLibrary.Model.TrainInfo;
using SolverLibrary.Model.Graph.VertexTypes;
using Newtonsoft.Json;

namespace SolverLibrary.Model.Graph
{
    public class Edge
    {
        [JsonProperty(Order = 1)]
        private int id;
        [JsonProperty(Order = 2)]
        private int length;
        [JsonProperty(PropertyName = "startId", Order = 3)]
        private Vertex? start;
        [JsonProperty(PropertyName = "endId", Order = 3)]
        private Vertex? end;
        private bool blocked;
        [JsonProperty(Order = 5)]
        private TrainType edgeType;

        [JsonConstructor]
        public Edge(int id, int length, Vertex? start, Vertex? end, TrainType edgeType)
        {
            this.id = id;
            SetLength(length);
            this.start = start;
            this.end = end;
            this.edgeType = edgeType;
            blocked = false;
        }
        public int getId() { return id; }
        public int GetLength() { return length; }
        public void SetLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentException("Edge length must be positive.");
            }
            this.length = length;
        }
        public Vertex? GetStart() { return start; }
        public void SetStart(Vertex? start) { this.start = start; }
        public Vertex? GetEnd() { return end; }
        public void SetEnd(Vertex? end) { this.end = end; }
        public bool IsBlocked() { return blocked; }
        public void Block() { blocked = true; }
        public void Unblock() { blocked = false; }
        public void SetEdgeType(TrainType edgeType) { this.edgeType = edgeType; }
        public TrainType GetEdgeType() { return edgeType; }

        public Vertex? GetVertexOut(Vertex vertexIn)
        {
            if (vertexIn == start)
            {
                return end;
            }
            else if (vertexIn == end)
            {
                return start;
            }
            return null;
        }
    }
}