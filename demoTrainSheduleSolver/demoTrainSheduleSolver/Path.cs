using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Path
{
    public int start;
    public int end;
    public int length;
    public Path(int start, int end, int length)
    {
        this.start = start;
        this.end = end;
        this.length = length;
    }
}
