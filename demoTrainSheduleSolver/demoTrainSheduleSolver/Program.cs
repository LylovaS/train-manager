// Copyright 2010-2022 Google LLC
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// [START program]
// [START import]
using System;
using System.Collections;
using System.Threading.Tasks;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.Sat;
using IntervalVar = Google.OrTools.Sat.IntervalVar;
using IntVar = Google.OrTools.Sat.IntVar;

// [END import]

public enum TrainType{ PASSENGER, CARGO, NONE };
public class Train
{
    public int timeArival, timeDeparture, timeStop, len, speed;
    public int vertexIn;
    public int vertexOut;
    public TrainType type;
    public Train(int timeArrival, int timeDeparture, int timeStop, int len, int speed, int vertexIn, int vertexOut, TrainType type)
    {
        this.timeArival= timeArrival;
        this.timeStop= timeStop;
        this.timeDeparture= timeDeparture;
        this.type = type;
        this.len = len;
        this.speed = speed;
        this.vertexIn = vertexIn;
        this.vertexOut = vertexOut;
    }
}

public class ShedulerGraphVertex
{
    public int edgeId;
    public int vertexBegin;
    public int vertexEnd;
    public int edgeLen;
    public List<ShedulerGraphVertex> adjacentVert;
    ShedulerGraphVertex(int edgeId,  int vertexBegin, int vertexEnd, int edgeLen)
    {
        this.edgeLen = edgeLen;
        this.vertexBegin = vertexBegin;
        this.vertexEnd = vertexEnd;
        this.edgeId = edgeId;
    }
}

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

public class sampleInput
{
    public Path[] paths;
    public Train[] trains;
    public Vertex[] vertices;
    public bool[,] hasPathsIntersection;
    public int[] platformsId;

    public sampleInput()
    {
        Vertex[] vertices =
        {
            null,
            new Vertex(TrainType.NONE, 100),
            new Vertex(TrainType.CARGO, 500),
            new Vertex(TrainType.NONE, 100),
            new Vertex(TrainType.NONE, 50),
            new Vertex(TrainType.NONE, 200),
            new Vertex(TrainType.CARGO, 700),
            new Vertex(TrainType.NONE, 300)
        };
        this.vertices = vertices;
        int[] platformsId = { 2, 6 };
        this.platformsId = platformsId;
        Train[] trains =
        {
            new Train(0, 70, 40, 400, 200, 1, 3, TrainType.CARGO),
            new Train(10, 70, 40, 600, 200, 8, 5, TrainType.CARGO),
            new Train(70, 200, 60, 500, 200, 8, 5, TrainType.CARGO)
        };
        Path[] paths =
        {
            new Path(1, 2, 600),
            new Path(2, 3, 600),
            new Path(8, 6, 1000),
            new Path(8, 2, 850),
            new Path(2, 5, 750),
            new Path(6, 5, 900)
        };
        this.paths = paths;
        this.trains = trains;
        bool[,] interse =
        {
            { false, true, false, true, true, false },
            { true, false, false, true, true, false },
            { false, false, false, true, false, true },
            { true, true, true, false, true, false},
            { true, true, false, true, false, true },
            { false, false, true, false, true, false }
        };
        this.hasPathsIntersection = interse;
    }

}

public class SimpleSatProgram
{
    static bool checkPathFromAtoBexistence(Path[] paths, int a, int b)
    {
        bool res = false;
        foreach (Path p in paths) 
        {
            if (p.start == a && p.end == b)
            {
                res = true;
                break;
            }
        }
        return res;
    }

    static Path findPathFromAtoB(Path[] paths, int a, int b)
    {
        foreach (Path p in paths)
        {
            if (p.start == a && p.end == b)
            {
                return p;
            }
        }
        return null;
    }

    static void Main()
    {
        CpModel model = new CpModel();

        sampleInput data = new sampleInput();
        int numTrains = data.trains.Length;
        int numPlatforms = data.platformsId.Length;
        Path[] paths = data.paths;
        Train[] trains = data.trains;
        Vertex[] vertices = data.vertices;
        bool[,] hasPathsIntersection = data.hasPathsIntersection;
        int[] platformsId = data.platformsId;

    BoolVar[,] trainGoesThroughPlatf = new BoolVar[numTrains, numPlatforms];
        for (int trainId = 0; trainId < numTrains; trainId++)
        {
            for (int platNum = 0; platNum < numPlatforms; platNum++) 
            {
                trainGoesThroughPlatf[trainId, platNum] = model.NewBoolVar($"x[{trainId}, {platNum})]");
            }
        }
        for (int trainId = 0;trainId < numTrains; trainId++)
        {
            List<ILiteral> platforms = new List<ILiteral>();
            for (int platNum = 0; platNum < numPlatforms; platNum++)
            {
                platforms.Add(trainGoesThroughPlatf[trainId, platNum]);
            }
            model.AddExactlyOne(platforms);
        }

        for (int trainId = 0; trainId < numTrains; trainId++)
        {
            List<ILiteral> goodPlatforms = new List<ILiteral>();
            Train train = trains[trainId];
            for (int platNum = 0; platNum < numPlatforms; platNum++)
            {
                Vertex platformVertex = vertices[platformsId[platNum]];
                if (platformVertex.platformType == train.type &&
                    platformVertex.len >= train.len &&
                    checkPathFromAtoBexistence(paths, train.vertexIn, platformsId[platNum]) &&
                    checkPathFromAtoBexistence(paths, platformsId[platNum], train.vertexOut))
                {
                    goodPlatforms.Add(trainGoesThroughPlatf[trainId, platNum]);
                }
                
            }

            model.AddExactlyOne(goodPlatforms);
        }

        // model.Add(trainGoesThroughPlatf[0, 0] && trainGoesThroughPlatf[1, 1] == trainGoesThroughPlatf[0, 0]);
         model.Add(trainGoesThroughPlatf[2, 0] == 1).OnlyEnforceIf(trainGoesThroughPlatf[0, 1].Not());
        ILiteral[] kek = { trainGoesThroughPlatf[0, 0] };
        model.AddAtLeastOne( kek);
        for (int train0id = 0; train0id < numTrains; train0id++)
        {
            Train train0 = trains[train0id];
            for (int plat0id = 0; plat0id < numPlatforms; plat0id++)
            {
                Path path0FromIn = findPathFromAtoB(paths, train0.vertexIn, plat0id);
                Path path0ToOut = findPathFromAtoB(paths, plat0id, train0.vertexOut);
                for (int train1id = 0; train1id < numPlatforms;  train1id++)
                {
                    for (int plat1id = 0; plat1id < numPlatforms; plat1id++)
                    {

                    }
                }
            }
        }

        CpSolver solver = new CpSolver();
        CpSolverStatus status = solver.Solve(model);
        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            Console.WriteLine("solution found.\n");
            for (int trainId = 0; trainId < numTrains; trainId++)
            {
                for (int platNum = 0; platNum < numPlatforms; platNum++)
                {
                    Console.Write(solver.Value(trainGoesThroughPlatf[trainId, platNum]));
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("No solution found.");
        }
        /*  
                // Console.WriteLine($"{t1.timeStop}");

                // Creates the variables.
                // [START variables]
                int num_vals = 110;

                IntVar x = model.NewIntVar(0, num_vals - 1, "x");
                IntVar y = model.NewIntVar(0, num_vals - 1, "y");
                IntVar z = model.NewIntVar(0, num_vals - 1, "z");
                long[] longs = { 3, 10, 15 };
                Google.OrTools.Util.Domain d = Google.OrTools.Util.Domain.FromValues(longs);
                x = model.NewIntVarFromDomain(d, "kek");
                // [END variables]

                // Creates the constraints.
                // [START constraints]
                int[] kek = { 0, 1, 2, 3 };

                IntervalVar interval = model.NewIntervalVar(x, 15, y, "x_y");
                IntervalVar interval2 = model.NewIntervalVar(1, 10, z, "x_z");
                IntervalVar[] aaa = { interval, interval2 };
                model.AddNoOverlap(aaa);
                // model.Add(x != y).OnlyEnforceIf(false);
                model.Add((x + z) != (x + y));
                model.Add(y != 1);
                model.NewBoolVar("kek");
                model.AddDivisionEquality(x, y, 2);

                // [END constraints]

                // Creates a solver and solves the model.
                // [START solve]
                CpSolver solver = new CpSolver();
                CpSolverStatus status = solver.Solve(model);
                // [END solve]

                // [START print_solution]
                if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
                {
                    Console.WriteLine("x = " + solver.Value(x));
                    Console.WriteLine("y = " + solver.Value(y));
                    Console.WriteLine("z = " + solver.Value(z));
                }
                else
                {
                    Console.WriteLine("No solution found.");
                }
                // [END print_solution]
                */
    }
}
// [END program]
