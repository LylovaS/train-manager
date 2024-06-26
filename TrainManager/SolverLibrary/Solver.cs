﻿using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;
using Google.OrTools.Sat;
using SolverLibrary.Model.PlanUnit;
using SolverLibrary.Algorithms;
using Constraint = Google.OrTools.Sat.Constraint;
using System.Linq;

namespace SolverLibrary
{
    public class Solver
    {
        private StationGraph station;
        private PathCalculator pathCalculator;
        private int timeInaccuracy;
        public Solver(StationGraph station, int timeInaccuracy) 
        {
            station.CheckStationGraph();
            this.timeInaccuracy = timeInaccuracy;
            this.station = station;
            //this.pathCalculator = new PathCalculator(station);
        }

        // Calculates station workplan for given train schedule (from scratch)
        public StationWorkPlan CalculateWorkPlan(TrainSchedule schedule)
        {
            // Check the graph for stupid errors
            if (!this.station.CheckStationGraph())
            {
                throw new Exception("Something wrong with graph");
            }
            Dictionary<Train, SingleTrainSchedule> dictSchedule = schedule.GetSchedule();
            HashSet<Vertex> inputVertices = new(station.GetInputVertices());

            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection = new();
            Dictionary<Vertex, List<GraphPath>> pathsStartFromVertex = new();
            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatform = new();
            HashSet<OutputVertex> outputVertexes = new();
            PathCalculator.calculatePathsFromIn(inputVertices, platformsWithDirection, pathsStartFromVertex);
            PathCalculator.calculatePathsFromPlatforms(platformsWithDirection, outputVertexes, pathsStartFromPlatform);
            // Calculate pathes that start from InputVertex and end on some platform
            // Calculate pathes that start from platform and end in OutputVertex

            CpModel model = new CpModel();

            // Enumerate trains and platforms
            Dictionary<Train, int> trainId = new();
            int trainsCnt = 0;
            foreach (var trainSchedule in dictSchedule)
            {
                if (trainId.ContainsKey(trainSchedule.Key))
                {
                    throw new Exception("Duplicate train in schedule");
                }
                trainId[trainSchedule.Key] = trainsCnt;
                trainsCnt++;
            }
            Dictionary<Tuple<Vertex, Vertex>, int> platformId = new();
            int platformsCnt = 0;
            foreach (var platform in platformsWithDirection)
            {
                platformId[platform] = platformsCnt;
                platformsCnt++;
            }

            //Find paths for each train condition
            Tuple<GraphPath?, GraphPath?>[,] trainConditionPaths = PathCalculator.calculateTrainConditionPaths(
                dictSchedule, pathsStartFromVertex, pathsStartFromPlatform,
                trainId, platformId);

            // Check that for each train we have at least one suitable and reachable platform 
            foreach (var train in dictSchedule.Keys)
            {
                bool hasSuitablePlat = false;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        hasSuitablePlat = true;
                    }
                }
                if (!hasSuitablePlat)
                {
                    throw new Exception("One of trains doesn't have a suitable and reachable platform");
                }
            }

            // Create BoolVar for each train condition
            BoolVar[,] trainGoesThroughPlatf = new BoolVar[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainGoesThroughPlatf[i, j] = model.NewBoolVar($"x[{i}, {j}]");
                }
            }

            // Add constraint for each train with his possible paths
            foreach (var train in dictSchedule.Keys)
            {
                List<ILiteral> goodConditons = new();
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        goodConditons.Add(trainGoesThroughPlatf[trainId[train], j]);
                    } else
                    {
                        model.AddAssumption(trainGoesThroughPlatf[trainId[train], j].Not());
                    }
                }
                model.AddExactlyOne(goodConditons);
            }

            PathTimeBlocker timeBlocker = new(timeInaccuracy);

            foreach (var train1 in dictSchedule.Keys)
            {
                for (int plat1 = 0; plat1 < platformsCnt; ++plat1)
                {
                    if (trainConditionPaths[trainId[train1], plat1].Item1 == null ||
                        trainConditionPaths[trainId[train1], plat1].Item2 == null)
                    {
                        continue;
                    }
                    var edgesTimeBlocks1 = timeBlocker.calculateEdgesTimeBlocking(
                                train1, dictSchedule[train1], 
                                trainConditionPaths[trainId[train1], plat1].Item1, 
                                trainConditionPaths[trainId[train1], plat1].Item2);
                    foreach (var train2 in dictSchedule.Keys)
                    {
                        if (train2 == train1)
                        {
                            continue;
                        }
                        for (int plat2 = 0; plat2 < platformsCnt; ++plat2)
                        {
                            if (trainConditionPaths[trainId[train2], plat2].Item1 == null ||
                                trainConditionPaths[trainId[train2], plat2].Item2 == null)
                            {
                                continue;
                            }

                            bool flag = true;
                            var edgesTimeBlocks2 = timeBlocker.calculateEdgesTimeBlocking(
                                train2, dictSchedule[train2],
                                trainConditionPaths[trainId[train2], plat2].Item1,
                                trainConditionPaths[trainId[train2], plat2].Item2);

                            foreach (var edge in edgesTimeBlocks1.Keys)
                            {
                                if (edgesTimeBlocks2.ContainsKey(edge)) {
                                    if (HelpFunctions.hasListsOfIntervalsIntersection(edgesTimeBlocks1[edge], edgesTimeBlocks2[edge]))
                                    {
                                        flag = false;
                                    }
                                }
                            }
                            if (!flag)
                            {
                                ILiteral[] boolVars = { trainGoesThroughPlatf[trainId[train1], plat1].Not(), trainGoesThroughPlatf[trainId[train2], plat2].Not() };
                                model.AddBoolOr(boolVars);
                            }
                        }
                    }
                }
            }

            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
            {
                throw new Exception("No solution ((((");
            }

            return ConstructPlan(solver, dictSchedule, trainConditionPaths, trainGoesThroughPlatf, trainId, platformsCnt);
        }

        // Recalculates workplan considering old workplan and new train schedule
        public StationWorkPlan RecalculateStationWorkPlan(StationWorkPlan plan, TrainSchedule trainSchedule)
        {
            // Check the graph for stupid errors
            if (!this.station.CheckStationGraph())
            {
                throw new Exception("Something wrong with graph");
            }
            Dictionary<Train, SingleTrainSchedule> dictSchedule = trainSchedule.GetSchedule();
            HashSet<Vertex> inputVertices = new(station.GetInputVertices());

            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection = new();
            Dictionary<Vertex, List<GraphPath>> pathsStartFromVertex = new();
            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatform = new();
            HashSet<OutputVertex> outputVertexes = new();
            PathCalculator.calculatePathsFromIn(inputVertices, platformsWithDirection, pathsStartFromVertex);
            PathCalculator.calculatePathsFromPlatforms(platformsWithDirection, outputVertexes, pathsStartFromPlatform);

            CpModel model = new CpModel();

            // Enumerate trains and platforms
            Dictionary<Train, int> trainId = new();
            int trainsCnt = 0;
            foreach (var schedule in dictSchedule)
            {
                if (trainId.ContainsKey(schedule.Key))
                {
                    throw new Exception("Duplicate train in schedule");
                }
                trainId[schedule.Key] = trainsCnt;
                trainsCnt++;
            }
            Dictionary<Tuple<Vertex, Vertex>, int> platformId = new();
            int platformsCnt = 0;
            foreach (var platform in platformsWithDirection)
            {
                platformId[platform] = platformsCnt;
                platformsCnt++;
            }

            //Find paths for each train condition
            Tuple<GraphPath?, GraphPath?>[,] trainConditionPaths = PathCalculator.calculateTrainConditionPaths(
                dictSchedule, pathsStartFromVertex, pathsStartFromPlatform,
                trainId, platformId);

            // Check that for each train we have at least one suitable and reachable platform 
            foreach (var train in dictSchedule.Keys)
            {
                bool hasSuitablePlat = false;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        hasSuitablePlat = true;
                    }
                }
                if (!hasSuitablePlat)
                {
                    throw new Exception("One of train hasn't suitable and reachable platform");
                }
            }

            // Create BoolVar for each train condition
            BoolVar[,] trainGoesThroughPlatf = new BoolVar[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainGoesThroughPlatf[i, j] = model.NewBoolVar($"x[{i}, {j}]");
                }
            }

            // Add constraint for each train with his possible paths
            foreach (var train in dictSchedule.Keys)
            {
                List<ILiteral> goodConditons = new();
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        goodConditons.Add(trainGoesThroughPlatf[trainId[train], j]);
                    }
                    else
                    {
                        model.AddAssumption(trainGoesThroughPlatf[trainId[train], j].Not());
                    }
                }
                model.AddExactlyOne(goodConditons);
            }

            // Create BoolVar for each train condition
            Constraint[,] constraints = new Constraint[trainsCnt, platformsCnt];
            BoolVar[,] trainGoesThroughPlatfOld = new BoolVar[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainGoesThroughPlatfOld[i, j] = model.NewBoolVar($"y[{i}, {j}]");
                }
            }

            // add to conditions
            foreach (var platformSchedule in plan.TrainPlatforms)
            {
                Train train = platformSchedule.Key;
                Edge platform = platformSchedule.Value;
                int id;
                if (platformId.TryGetValue(new(platform.GetStart(), platform.GetEnd()), out id)
                    && trainConditionPaths[trainId[train], id].Item1 != null
                    && trainConditionPaths[trainId[train], id].Item2 != null)
                {
                    id = platformId[new(platform.GetStart(), platform.GetEnd())];
                }
                else if (platformId.TryGetValue(new(platform.GetEnd(), platform.GetStart()), out id)
                    && trainConditionPaths[trainId[train], id].Item1 != null
                    && trainConditionPaths[trainId[train], id].Item2 != null)
                {
                    id = platformId[new(platform.GetEnd(), platform.GetStart())];
                }
                int bound;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (j == id)
                    {
                        bound = 1;
                    }
                    else
                    {
                        bound = 0;
                    }
                    constraints[trainId[train], j] = model.Add(trainGoesThroughPlatfOld[trainId[train], j] == bound);
                }
            }

            IntVar[] diff = new IntVar[trainsCnt * platformsCnt];

            for (int i = 0; i < trainsCnt; i++)
            {
                for (int j = 0; j < platformsCnt; j++)
                {
                    diff[i * platformsCnt + j] = model.NewIntVar(0, 1, $"diff({i},{j})");
                }
            }

            for (int i = 0; i < trainsCnt; i++)
            {
                for (int j = 0; j < platformsCnt; j++)
                {
                    model.Add(diff[i * platformsCnt + j] == trainGoesThroughPlatf[i, j].NotAsExpr()).OnlyEnforceIf(trainGoesThroughPlatfOld[i, j]);
                    model.Add(diff[i * platformsCnt + j] == trainGoesThroughPlatf[i, j]).OnlyEnforceIf(trainGoesThroughPlatfOld[i, j].Not());
                }
            }
            LinearExpr sum = LinearExpr.Sum(diff);
            model.Minimize(sum);


            PathTimeBlocker timeBlocker = new(timeInaccuracy);

            foreach (var train1 in dictSchedule.Keys)
            {
                for (int plat1 = 0; plat1 < platformsCnt; ++plat1)
                {
                    if (trainConditionPaths[trainId[train1], plat1].Item1 == null ||
                        trainConditionPaths[trainId[train1], plat1].Item2 == null)
                    {
                        continue;
                    }
                    var edgesTimeBlocks1 = timeBlocker.calculateEdgesTimeBlocking(
                                train1, dictSchedule[train1],
                                trainConditionPaths[trainId[train1], plat1].Item1,
                                trainConditionPaths[trainId[train1], plat1].Item2);
                    foreach (var train2 in dictSchedule.Keys)
                    {
                        if (train2 == train1)
                        {
                            continue;
                        }
                        for (int plat2 = 0; plat2 < platformsCnt; ++plat2)
                        {
                            if (trainConditionPaths[trainId[train2], plat2].Item1 == null ||
                                trainConditionPaths[trainId[train2], plat2].Item2 == null)
                            {
                                continue;
                            }

                            bool flag = true;
                            var edgesTimeBlocks2 = timeBlocker.calculateEdgesTimeBlocking(
                                train2, dictSchedule[train2],
                                trainConditionPaths[trainId[train2], plat2].Item1,
                                trainConditionPaths[trainId[train2], plat2].Item2);

                            foreach (var edge in edgesTimeBlocks1.Keys)
                            {
                                if (edgesTimeBlocks2.ContainsKey(edge))
                                {
                                    if (HelpFunctions.hasListsOfIntervalsIntersection(edgesTimeBlocks1[edge], edgesTimeBlocks2[edge]))
                                    {
                                        flag = false;
                                    }
                                }
                            }
                            if (!flag)
                            {
                                ILiteral[] boolVars = { trainGoesThroughPlatf[trainId[train1], plat1].Not(), trainGoesThroughPlatf[trainId[train2], plat2].Not() };
                                model.AddBoolOr(boolVars);
                            }
                        }
                    }
                }
            }

            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
            {
                throw new Exception("No solution ((((");
            }

            return ConstructPlan(solver, dictSchedule, trainConditionPaths, trainGoesThroughPlatf, trainId, platformsCnt);
        }

        // Recalculates workplan in "real-time" simulation considering old workplan and new train schedule
        // value in arrivedTrainPos dictionary is a pair of directed edge and a timestamp of when the train has reached the edge 
        public StationWorkPlan RecalculateStationWorkPlan(StationWorkPlan plan, TrainSchedule trainSchedule, 
            Dictionary<Train, Tuple<Tuple<Vertex, Vertex>, int>> arrivedTrainsPos, Dictionary<Train, bool> passedStopPlatform)
        {
            // Check the graph for stupid errors
            if (!this.station.CheckStationGraph())
            {
                throw new Exception("Something wrong with graph");
            }
            Dictionary<Train, SingleTrainSchedule> dictSchedule = new(trainSchedule.GetSchedule());
            TrainSchedule scheduleCopy = trainSchedule.Clone();
            Dictionary<Train, SingleTrainSchedule> dictScheduleCopy = new(scheduleCopy.GetSchedule());
            
            HashSet<Vertex> inputVertices = new HashSet<Vertex>(station.GetInputVertices());
            HashSet<Train> trainsPassedPlatforms = new();
            
            // update trains' schedule to calculate paths
            foreach (Train train in arrivedTrainsPos.Keys)
            {
                Vertex start = arrivedTrainsPos[train].Item1.Item1;
                Vertex end = arrivedTrainsPos[train].Item1.Item2;
                Edge currentEdge = HelpFunctions.findEdge(start, end);                
                int arrivalTime = arrivedTrainsPos[train].Item2;
                if (passedStopPlatform[train] && currentEdge != plan.TrainPlatforms[train])
                {
                    dictScheduleCopy[train].SetTimeStop(0);
                    trainsPassedPlatforms.Add(train);
                }
                else
                {
                    dictScheduleCopy[train].SetVertexIn(start);
                    inputVertices.Add(start);
                }
                dictScheduleCopy[train].SetTimeArrival(arrivalTime);
            }
 
            // calculate paths for all trains except those which have already passed their stop platforms
            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection = new();
            Dictionary<Vertex, List<GraphPath>> pathsStartFromVertex = new();
            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatform = new();
            HashSet<OutputVertex> outputVertexes = new();
            PathCalculator.calculatePathsFromIn(inputVertices, platformsWithDirection, pathsStartFromVertex);
            PathCalculator.calculatePathsFromPlatforms(platformsWithDirection, outputVertexes, pathsStartFromPlatform);

            // Enumerate trains and platforms
            Dictionary<Train, int> trainId = new();
            int trainsCnt = 0;
            foreach (var schedule in dictScheduleCopy)
            {
                if (trainId.ContainsKey(schedule.Key))
                {
                    throw new Exception("Duplicate train in schedule");
                }
                trainId[schedule.Key] = trainsCnt;
                trainsCnt++;
            }
            Dictionary<Tuple<Vertex, Vertex>, int> platformId = new();
            int platformsCnt = 0;
            foreach (var platform in platformsWithDirection)
            {
                platformId[platform] = platformsCnt;
                platformsCnt++;
            }

            // calculate paths from platforms for trains which have passed their stop platforms
            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection2 = new();
            foreach (var train in trainsPassedPlatforms)
            {
                platformsWithDirection2.Add(arrivedTrainsPos[train].Item1);
            }
            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatform2 = new();
            HashSet<OutputVertex> outputVertexes2 = new();
            PathCalculator.calculatePathsFromPlatforms(platformsWithDirection2, outputVertexes2, pathsStartFromPlatform2);
            foreach (var platform in platformsWithDirection2)
            {
                platformId[platform] = platformsCnt;
                platformsCnt++;
            }


            //Find paths for each train condition except for trains which have passed their stop platforms
            Tuple<GraphPath?, GraphPath?>[,] trainConditionPaths = PathCalculator.calculateTrainConditionPaths(
                dictScheduleCopy, pathsStartFromVertex, pathsStartFromPlatform,
                trainId, platformId);

            // update train condition paths for trains which have passed their stop platforms
            foreach (var train in trainsPassedPlatforms)
            { 
                for (int j = 0; j < platformsCnt; j++)
                {
                    trainConditionPaths[trainId[train], j] = new(null, null);
                }
                Vertex start = arrivedTrainsPos[train].Item1.Item1;
                Vertex end = arrivedTrainsPos[train].Item1.Item2;
                Edge platform = HelpFunctions.findEdge(start, end);
                GraphPath pathFromIn = new GraphPath(start, end);
                SingleTrainSchedule singleSchedule = dictScheduleCopy[train];
                foreach (var pathFromPlat in pathsStartFromPlatform2[new(start, end)])
                {
                    int travelTime = (pathFromIn.length + pathFromPlat.length + train.GetSpeed() - 1) / train.GetSpeed();
                    if (singleSchedule.GetTimeArrival() + singleSchedule.GetTimeStop() + travelTime <= singleSchedule.GetTimeDeparture())
                    {
                        trainConditionPaths[trainId[train], platformId[new(start, end)]] = new(pathFromIn, pathFromPlat);
                    }
                }
            }


            CpModel model = new CpModel();

            // Check that for each train we have at least one suitable and reachable platform 
            foreach (var train in dictSchedule.Keys)
            {
                bool hasSuitablePlat = false;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        hasSuitablePlat = true;
                    }
                }
                if (!hasSuitablePlat)
                {
                    throw new Exception("One of trains doesn't have a suitable and reachable platform");
                }
            }

            // Create BoolVar for each train condition
            BoolVar[,] trainGoesThroughPlatf = new BoolVar[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainGoesThroughPlatf[i, j] = model.NewBoolVar($"x[{i}, {j}]");
                }
            }

            // Add constraint for each train with his possible paths
            foreach (var train in dictSchedule.Keys)
            {
                List<ILiteral> goodConditons = new();
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (trainConditionPaths[trainId[train], j].Item1 != null &&
                        trainConditionPaths[trainId[train], j].Item2 != null)
                    {
                        goodConditons.Add(trainGoesThroughPlatf[trainId[train], j]);
                    }
                    else
                    {
                        model.AddAssumption(trainGoesThroughPlatf[trainId[train], j].Not());
                    }
                }
                model.AddExactlyOne(goodConditons);
            }

            // Create BoolVar for each train condition
            Constraint[,] constraints = new Constraint[trainsCnt, platformsCnt];
            BoolVar[,] trainGoesThroughPlatfOld = new BoolVar[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainGoesThroughPlatfOld[i, j] = model.NewBoolVar($"y[{i}, {j}]");
                }
            }

            // add to conditions
            foreach (var platformSchedule in plan.TrainPlatforms)
            {
                Train train = platformSchedule.Key;
                Edge platform = platformSchedule.Value;
                int id;
                if (platformId.TryGetValue(new(platform.GetStart(), platform.GetEnd()), out id)
                    && trainConditionPaths[trainId[train], id].Item1 != null
                    && trainConditionPaths[trainId[train], id].Item2 != null)
                {
                    id = platformId[new(platform.GetStart(), platform.GetEnd())];
                }
                else if (platformId.TryGetValue(new(platform.GetEnd(), platform.GetStart()), out id)
                    && trainConditionPaths[trainId[train], id].Item1 != null
                    && trainConditionPaths[trainId[train], id].Item2 != null)
                {
                    id = platformId[new(platform.GetEnd(), platform.GetStart())];
                }
                int bound;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (j == id)
                    {
                        bound = 1;
                    }
                    else
                    {
                        bound = 0;
                    }
                    constraints[trainId[train], j] = model.Add(trainGoesThroughPlatfOld[trainId[train], j] == bound);
                }
            }

            IntVar[] diff = new IntVar[trainsCnt * platformsCnt];

            for (int i = 0; i < trainsCnt; i++)
            {
                for (int j = 0; j < platformsCnt; j++)
                {
                    diff[i * platformsCnt + j] = model.NewIntVar(0, 1, $"diff({i},{j})");
                }
            }

            for (int i = 0; i < trainsCnt; i++)
            {
                for (int j = 0; j < platformsCnt; j++)
                {
                    model.Add(diff[i * platformsCnt + j] == trainGoesThroughPlatf[i, j].NotAsExpr()).OnlyEnforceIf(trainGoesThroughPlatfOld[i, j]);
                    model.Add(diff[i * platformsCnt + j] == trainGoesThroughPlatf[i, j]).OnlyEnforceIf(trainGoesThroughPlatfOld[i, j].Not());
                }
            }
            LinearExpr sum = LinearExpr.Sum(diff);
            model.Minimize(sum);


            PathTimeBlocker timeBlocker = new(timeInaccuracy);

            foreach (var train1 in dictSchedule.Keys)
            {
                for (int plat1 = 0; plat1 < platformsCnt; ++plat1)
                {
                    if (trainConditionPaths[trainId[train1], plat1].Item1 == null ||
                        trainConditionPaths[trainId[train1], plat1].Item2 == null)
                    {
                        continue;
                    }
                    var edgesTimeBlocks1 = timeBlocker.calculateEdgesTimeBlocking(
                                train1, 
                                dictScheduleCopy[train1],
                                trainConditionPaths[trainId[train1], plat1].Item1,
                                trainConditionPaths[trainId[train1], plat1].Item2);
                    foreach (var train2 in dictSchedule.Keys)
                    {
                        if (train2 == train1)
                        {
                            continue;
                        }
                        for (int plat2 = 0; plat2 < platformsCnt; ++plat2)
                        {
                            if (trainConditionPaths[trainId[train2], plat2].Item1 == null ||
                                trainConditionPaths[trainId[train2], plat2].Item2 == null)
                            {
                                continue;
                            }

                            bool flag = true;
                            var edgesTimeBlocks2 = timeBlocker.calculateEdgesTimeBlocking(
                                train2,
                                dictScheduleCopy[train2],
                                trainConditionPaths[trainId[train2], plat2].Item1,
                                trainConditionPaths[trainId[train2], plat2].Item2);

                            foreach (var edge in edgesTimeBlocks1.Keys)
                            {
                                if (edgesTimeBlocks2.ContainsKey(edge))
                                {
                                    if (HelpFunctions.hasListsOfIntervalsIntersection(edgesTimeBlocks1[edge], edgesTimeBlocks2[edge]))
                                    {
                                        flag = false;
                                    }
                                }
                            }
                            if (!flag)
                            {
                                ILiteral[] boolVars = { trainGoesThroughPlatf[trainId[train1], plat1].Not(), trainGoesThroughPlatf[trainId[train2], plat2].Not() };
                                model.AddBoolOr(boolVars);
                            }
                        }
                    }
                }
            }

            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
            {
                throw new Exception("No solution ((((");
            }

            return ConstructPlan(solver, dictSchedule, trainConditionPaths, trainGoesThroughPlatf, trainId, platformsCnt);
        }

        public bool matchWorkplanToStation(StationWorkPlan plan, TrainSchedule trainSchedule)
        {
            Dictionary<Train, SingleTrainSchedule> dictSchedule = trainSchedule.GetSchedule();
            HashSet<Vertex> inputVertices = new(station.GetInputVertices());

            HashSet<Tuple<Vertex?, Vertex>> platformsWithDirection = new();
            Dictionary<Vertex, List<GraphPath>> pathsStartFromVertex = new();
            Dictionary<Tuple<Vertex, Vertex>, List<GraphPath>> pathsStartFromPlatform = new();
            HashSet<OutputVertex> outputVertexes = new();
            PathCalculator.calculatePathsFromIn(inputVertices, platformsWithDirection, pathsStartFromVertex);
            PathCalculator.calculatePathsFromPlatforms(platformsWithDirection, outputVertexes, pathsStartFromPlatform);

            CpModel model = new CpModel();

            // Enumerate trains and platforms
            Dictionary<Train, int> trainId = new();
            int trainsCnt = 0;
            foreach (var s in dictSchedule)
            {
                if (trainId.ContainsKey(s.Key))
                {
                    throw new Exception("Duplicate train in schedule");
                }
                trainId[s.Key] = trainsCnt;
                trainsCnt++;
            }
            Dictionary<Tuple<Vertex, Vertex>, int> platformId = new();
            int platformsCnt = 0;
            foreach (var platform in platformsWithDirection)
            {
                platformId[platform] = platformsCnt;
                platformsCnt++;
            }

            //Find paths for each train condition
            Tuple<GraphPath?, GraphPath?>[,] trainConditionPaths = PathCalculator.calculateTrainConditionPaths(
                dictSchedule, pathsStartFromVertex, pathsStartFromPlatform,
                trainId, platformId);

            // Check that plan has assigned a suitable and reachable platform for each train 
            // and there is a path to and from the assigned platform
            foreach (var schedule in dictSchedule)
            {
                Train train = schedule.Key;
                SingleTrainSchedule singleSchedule = schedule.Value;
                Edge? platform;
                if (!plan.TrainPlatforms.TryGetValue(train, out platform) || platform == null
                    || !platform.GetEdgeType().Equals(train.GetTrainType()))
                {
                    throw new Exception($"Cannot assign given platform {platform.getId()} to train {trainId[train]}");
                    //return false;
                }
                Tuple<GraphPath?, GraphPath?> path = trainConditionPaths[trainId[train], platformId[new(platform.GetStart(), platform.GetEnd())]];
                if (path.Item1 == null || path.Item2 == null)
                {
                    path = trainConditionPaths[trainId[train], platformId[new(platform.GetEnd(), platform.GetStart())]];
                    if (path.Item1 == null || path.Item2 == null)
                    {
                        throw new Exception($"No full path to and from platform {platform.getId()}");
                        //return false;
                    }
                }
            }

            // Create BoolVar for each train condition
            BoolVar[,] trainGoesThroughPlatf = new BoolVar[trainsCnt, platformsCnt];
            for (int i = 0; i < trainsCnt; ++i)
            {
                for (int j = 0; j < platformsCnt; ++j)
                {
                    trainGoesThroughPlatf[i, j] = model.NewBoolVar($"x[{i}, {j}]");
                }
            }

            // add conditions according to plan
            foreach (var platformSchedule in plan.TrainPlatforms)
            {
                Train train = platformSchedule.Key;
                Edge platform = platformSchedule.Value;
                int id;
                if (platformId.TryGetValue(new(platform.GetStart(), platform.GetEnd()), out id)
                    && trainConditionPaths[trainId[train], id].Item1 != null 
                    && trainConditionPaths[trainId[train], id].Item2 != null)
                {
                    id = platformId[new(platform.GetStart(), platform.GetEnd())];
                }
                else if (platformId.TryGetValue(new(platform.GetEnd(), platform.GetStart()), out id)
                    && trainConditionPaths[trainId[train], id].Item1 != null
                    && trainConditionPaths[trainId[train], id].Item2 != null)
                {
                    id = platformId[new(platform.GetEnd(), platform.GetStart())];
                }
                List<ILiteral> goodConditons = new();
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (j == id)
                    {
                        goodConditons.Add(trainGoesThroughPlatf[trainId[train], j]);
                    }
                    else
                    {
                        model.AddAssumption(trainGoesThroughPlatf[trainId[train], j].Not());
                    }
                }
                model.AddExactlyOne(goodConditons);
            }

            PathTimeBlocker timeBlocker = new(timeInaccuracy);

            foreach (var train1 in dictSchedule.Keys)
            {
                for (int plat1 = 0; plat1 < platformsCnt; ++plat1)
                {
                    if (trainConditionPaths[trainId[train1], plat1].Item1 == null ||
                        trainConditionPaths[trainId[train1], plat1].Item2 == null)
                    {
                        continue;
                    }
                    var edgesTimeBlocks1 = timeBlocker.calculateEdgesTimeBlocking(
                                train1, dictSchedule[train1],
                                trainConditionPaths[trainId[train1], plat1].Item1,
                                trainConditionPaths[trainId[train1], plat1].Item2);
                    foreach (var train2 in dictSchedule.Keys)
                    {
                        if (train2 == train1)
                        {
                            continue;
                        }
                        for (int plat2 = 0; plat2 < platformsCnt; ++plat2)
                        {
                            if (trainConditionPaths[trainId[train2], plat2].Item1 == null ||
                                trainConditionPaths[trainId[train2], plat2].Item2 == null)
                            {
                                continue;
                            }

                            bool flag = true;
                            var edgesTimeBlocks2 = timeBlocker.calculateEdgesTimeBlocking(
                                train2, dictSchedule[train2],
                                trainConditionPaths[trainId[train2], plat2].Item1,
                                trainConditionPaths[trainId[train2], plat2].Item2);

                            foreach (var edge in edgesTimeBlocks1.Keys)
                            {
                                if (edgesTimeBlocks2.ContainsKey(edge))
                                {
                                    if (HelpFunctions.hasListsOfIntervalsIntersection(edgesTimeBlocks1[edge], edgesTimeBlocks2[edge]))
                                    {
                                        flag = false;
                                    }
                                }
                            }
                            if (!flag)
                            {
                                ILiteral[] boolVars = { trainGoesThroughPlatf[trainId[train1], plat1].Not(), trainGoesThroughPlatf[trainId[train2], plat2].Not() };
                                model.AddBoolOr(boolVars);
                            }
                        }
                    }
                }
            }


            // try to match trains with stopping platforms according to current plan
            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
            {
                //throw new Exception("Couldn't make a schedule for current plan.");
                return false;
            }

            //return ConstructPlan(solver, dictSchedule, trainConditionPaths, trainGoesThroughPlatf, trainId, platformsCnt);
            return true;
        }

        private StationWorkPlan ConstructPlan(
            CpSolver solver, 
            Dictionary<Train, SingleTrainSchedule> dictSchedule, 
            Tuple<GraphPath?, GraphPath?>[,] trainConditionPaths, 
            BoolVar[,] trainGoesThroughPlatf, 
            Dictionary<Train, int> trainId, 
            int platformsCnt
            )
        {
            StationWorkPlan plan = new();
            foreach (Train train in dictSchedule.Keys)
            {
                for (int platform = 0; platform < platformsCnt; ++platform)
                {
                    if (solver.Value(trainGoesThroughPlatf[trainId[train], platform]) == 1)
                    {
                        var tmp = trainConditionPaths[trainId[train], platform].Item1.GetVertices();
                        plan.AddTrainWithPlatform(train, HelpFunctions.findEdge(tmp[tmp.Count - 1], tmp[tmp.Count - 2]));
                    }
                }
            }

            foreach (Train train in dictSchedule.Keys)
            {
                var trainSchedule = dictSchedule[train];
                int platform = -1;
                for (int j = 0; j < platformsCnt; ++j)
                {
                    if (solver.Value(trainGoesThroughPlatf[trainId[train], j]) == 1)
                    {
                        platform = j; break;
                    }
                }
                var vertices = trainConditionPaths[trainId[train], platform].Item1.GetVertices();
                int time = (trainConditionPaths[trainId[train], platform].Item1.length + train.GetSpeed() - 1) / train.GetSpeed();
                for (int i = 1; i + 1 < vertices.Count; ++i)
                {
                    if (vertices[i].GetVertexType() == VertexType.TRAFFIC)
                    {
                        TrafficLightPlanUnit planUnit = new((TrafficLightVertex)vertices[i],
                            trainSchedule.GetTimeArrival() - timeInaccuracy,
                            trainSchedule.GetTimeArrival() + time + timeInaccuracy,
                            TrafficLightStatus.PASSING);
                        plan.AddTrafficLightPlanUnit(planUnit);
                    }
                    if (vertices[i].GetVertexType() == VertexType.SWITCH)
                    {
                        SwitchStatus switchStatus = SwitchStatus.PASSINGCON2;
                        SwitchVertex v = (SwitchVertex)vertices[i];
                        if (HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item1, new(vertices[i - 1], vertices[i])) &&
                            HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item2, new(vertices[i + 1], vertices[i])))
                        {
                            switchStatus = SwitchStatus.PASSINGCON1;
                        }
                        if (HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item1, new(vertices[i + 1], vertices[i])) &&
                            HelpFunctions.hasEdgeThatEndings(v.GetEdgeConnections()[0].Item2, new(vertices[i - 1], vertices[i])))
                        {
                            switchStatus = SwitchStatus.PASSINGCON1;
                        }
                        SwitchPlanUnit planUnit = new(v,
                            trainSchedule.GetTimeArrival() - timeInaccuracy,
                            trainSchedule.GetTimeArrival() + time + timeInaccuracy,
                            switchStatus);
                        plan.AddSwitchPlanUnit(planUnit);
                    }
                }
            }
            Dictionary<TrafficLightVertex, int> pastTime = new();
            List<TrafficLightPlanUnit> planUnits = new();
            foreach (var unit in plan.GetTrafficLightPlanUnits())
            {
                TrafficLightVertex v = unit.GetVertex();
                if (pastTime.ContainsKey(v))
                {
                    if (unit.GetBeginTime() - 1 >= pastTime[v] + 1)
                    {
                        planUnits.Add(new(v, pastTime[v] + 1, unit.GetBeginTime() - 1, TrafficLightStatus.STOP));
                    }
                }
                else
                {
                    planUnits.Add(new(v, int.MinValue, unit.GetBeginTime() - 1, TrafficLightStatus.STOP));
                }
                pastTime[v] = unit.GetEndTime();
            }
            foreach (var trLight in pastTime.Keys)
            {
                planUnits.Add(new(trLight, pastTime[trLight] + 1, int.MaxValue, TrafficLightStatus.STOP));
            }
            foreach (var unit in planUnits)
            {
                plan.AddTrafficLightPlanUnit(unit);
            }

            return plan;
        }
    }
}
