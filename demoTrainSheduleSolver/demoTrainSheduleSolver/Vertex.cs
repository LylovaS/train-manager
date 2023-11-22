using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Vertex
{
    public TrainType platformType;
    public int len;
    public Vertex(TrainType platformType, int len)
    {
        this.platformType = platformType;
        this.len = len;
    }
}