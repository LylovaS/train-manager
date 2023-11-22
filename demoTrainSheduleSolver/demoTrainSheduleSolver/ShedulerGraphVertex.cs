using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ShedulerGraphVertex
{
    public int edgeId;
    public int vertexBegin;
    public int vertexEnd;
    public int edgeLen;
    public List<ShedulerGraphVertex> adjacentVert;
    ShedulerGraphVertex(int edgeId, int vertexBegin, int vertexEnd, int edgeLen)
    {
        this.edgeLen = edgeLen;
        this.vertexBegin = vertexBegin;
        this.vertexEnd = vertexEnd;
        this.edgeId = edgeId;
    }
}
