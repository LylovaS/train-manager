using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum TrainType { PASSENGER, CARGO, NONE };
public class Train
{
    public int timeArival, timeDeparture, timeStop, len, speed;
    public int vertexIn;
    public int vertexOut;
    public TrainType type;
    public Train(int timeArrival, int timeDeparture, int timeStop, int len, int speed, int vertexIn, int vertexOut, TrainType type)
    {
        this.timeArival = timeArrival;
        this.timeStop = timeStop;
        this.timeDeparture = timeDeparture;
        this.type = type;
        this.len = len;
        this.speed = speed;
        this.vertexIn = vertexIn;
        this.vertexOut = vertexOut;
    }
}
