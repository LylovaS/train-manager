using System;
using System.Collections;
using System.Threading.Tasks;
using Google.OrTools.ConstraintSolver;
using Google.OrTools.Sat;
using IntervalVar = Google.OrTools.Sat.IntervalVar;
using IntVar = Google.OrTools.Sat.IntVar;


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
            new Vertex(TrainType.PASSENGER, 700),
            new Vertex(TrainType.NONE, 300)
        };
        this.vertices = vertices;
        int[] platformsId = { 2, 6 };
        this.platformsId = platformsId;
        Train[] trains =
        {
            new Train(0, 70, 40, 400, 200, 1, 3, TrainType.CARGO),
            new Train(10, 70, 40, 600, 200, 8, 5, TrainType.PASSENGER),
            new Train(68, 200, 60, 500, 200, 8, 5, TrainType.CARGO)
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

    static int findIdOfPathFromAtoB(Path[] paths, int a, int b)
    {
        for (int ans = 0; ans < paths.Length; ++ans)
        {
            if (paths[ans].start == a && paths[ans].end == b)
            {
                return ans;
            }
        }
        return -1;
    }

    static bool hasTimeIntervalsIntersection(int t0_begin, int t0_end, int t1_begin, int t1_end)
    {
        return Math.Min(t0_end, t1_end) >= Math.Max(t0_begin, t1_begin);
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
                Path path0FromIn = findPathFromAtoB(paths, train.vertexIn, platformsId[platNum]);
                Path path0ToOut = findPathFromAtoB(paths, platformsId[platNum], train.vertexOut);
                if (path0FromIn == null || path0ToOut == null)
                {
                    continue;
                } 

                int t0 = train.timeArival, t1 = t0 + (path0FromIn.length + train.speed - 1) / train.speed;
                int t3 = train.timeDeparture, t2 = t3 - (path0ToOut.length + train.speed - 1) / train.speed;
                Vertex platformVertex = vertices[platformsId[platNum]];
                if (platformVertex.platformType == train.type &&
                    platformVertex.len >= train.len &&
                    checkPathFromAtoBexistence(paths, train.vertexIn, platformsId[platNum]) &&
                    checkPathFromAtoBexistence(paths, platformsId[platNum], train.vertexOut) &&
                    t1 + train.timeStop <= t2 )
                {
                    goodPlatforms.Add(trainGoesThroughPlatf[trainId, platNum]);
                }
                
            }

            model.AddExactlyOne(goodPlatforms);
        }

        // model.Add(trainGoesThroughPlatf[0, 0] && trainGoesThroughPlatf[1, 1] == trainGoesThroughPlatf[0, 0]);
       //  model.Add(trainGoesThroughPlatf[2, 0] == 1).OnlyEnforceIf(trainGoesThroughPlatf[0, 1].Not());
       // ILiteral[] kek = { trainGoesThroughPlatf[0, 0] };
      //  model.AddAtLeastOne( kek);
        for (int train0id = 0; train0id < numTrains; train0id++)
        {
            Train train0 = trains[train0id];
            for (int plat0id = 0; plat0id < numPlatforms; plat0id++)
            {
                Path path0FromIn = findPathFromAtoB(paths, train0.vertexIn, platformsId[plat0id]);
                int p0inId = findIdOfPathFromAtoB(paths, train0.vertexIn, platformsId[plat0id]);
                Path path0ToOut = findPathFromAtoB(paths, platformsId[plat0id], train0.vertexOut);
                int p0outId = findIdOfPathFromAtoB(paths, platformsId[plat0id], train0.vertexOut);
                if (path0FromIn == null || path0ToOut == null)
                {
                    continue;
                }
                int t0_0 = train0.timeArival, t0_1 = t0_0 + (path0FromIn.length + train0.speed - 1) / train0.speed;
                int t0_3 = train0.timeDeparture, t0_2 = t0_3 - (path0ToOut.length + train0.speed - 1) / train0.speed;

                for (int train1id = train0id + 1; train1id < numTrains;  train1id++)
                {
                    Train train1 = trains[train1id];
                    for (int plat1id = 0; plat1id < numPlatforms; plat1id++)
                    {
                        Path path1FromIn = findPathFromAtoB(paths, train1.vertexIn, platformsId[plat1id]);
                        int p1inId = findIdOfPathFromAtoB(paths, train1.vertexIn, platformsId[plat1id]);
                        Path path1ToOut = findPathFromAtoB(paths, platformsId[plat1id], train1.vertexOut);
                        int p1outId = findIdOfPathFromAtoB(paths, platformsId[plat1id], train1.vertexOut);
                        if (path1FromIn == null || path1ToOut == null)
                        {
                            continue;
                        }
                        int t1_0 = train1.timeArival, t1_1 = t1_0 + (path1FromIn.length + train1.speed - 1) / train1.speed;
                        int t1_3 = train1.timeDeparture, t1_2 = t1_3 - (path1ToOut.length + train1.speed - 1) / train1.speed;

                        if ((plat0id == plat1id && hasTimeIntervalsIntersection(t0_1, t0_2, t1_1, t1_2)) ||
                            (hasPathsIntersection[p0inId, p1inId] && hasTimeIntervalsIntersection(t0_0, t0_1, t1_0, t1_1)) ||
                            (hasPathsIntersection[p0outId, p1outId] && hasTimeIntervalsIntersection(t0_2, t0_3, t1_2, t1_3))
                            ) {
                            ILiteral[] boolVars = { trainGoesThroughPlatf[train0id, plat0id].Not(), trainGoesThroughPlatf[train1id, plat1id].Not() };
                            model.AddBoolOr(boolVars);
                        }
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

            for (int trainId = 0; trainId < numTrains; trainId++)
            {
                for (int platNum = 0; platNum < numPlatforms; platNum++)
                {
                    if (solver.Value(trainGoesThroughPlatf[trainId, platNum]) == 1)
                    {
                        Console.WriteLine($"Train {trainId} goes through platform {platNum}");
                    }
                }
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
