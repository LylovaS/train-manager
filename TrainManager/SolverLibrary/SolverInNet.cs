using Google.OrTools.Sat;
using SolverLibrary.Algorithms;
using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary
{
    public class SolverInNet
    {
        private StationNet stationNet;
        private int timeInaccuracy;

        public SolverInNet(StationNet stationNet, int timeInaccuracy)
        {
            this.timeInaccuracy = timeInaccuracy;
            this.stationNet = stationNet;
        }

        public StationNetWorkPlan CalculateWorkPlan(TrainSheduleInNet scheduleInNet)
        {
            if (!stationNet.checkStationNet())
            {
                throw new Exception("Something wrong with station net");
            }

         
            StationNetWorkPlan workPlan = new StationNetWorkPlan();

            Dictionary<StationGraph, PathCalculator> pathCalculators = new();
            Dictionary<InputVertex, StationGraph> inputVertexToStation = new();
            Dictionary<OutputVertex, StationGraph> outputVertexToStation = new();
            foreach (var station in stationNet.GetStations())
            {
                if (pathCalculators.ContainsKey(station))
                {
                    throw new Exception("net contains two equals stations");
                }
                pathCalculators[station] = new PathCalculator(station);
                foreach (InputVertex inputVertex in station.GetInputVertices())
                {
                    inputVertexToStation[inputVertex] = station;
                }
                foreach (OutputVertex outputVertex in pathCalculators[station].outputVertexes) 
                {
                    outputVertexToStation[outputVertex] = station;
                }
            }

            Dictionary<InputVertex, int> lengthOfExternalConnectionWtihVertex = new();
            Dictionary<InputVertex, OutputVertex> previousVertexOutsideOfStation = new();
            foreach (var connection in stationNet.GetConnections())
            {
                lengthOfExternalConnectionWtihVertex[connection.Item1] = connection.Item3;
                previousVertexOutsideOfStation[connection.Item1] = connection.Item2;
            }

            List<List<TrainInfoOnStation>> infos = new();
            // infos[trainId][stationId] - набор информации о  данном поезде на данной станции
            int trainsCnt = scheduleInNet.GetSchedule().Count;
            for (int trainId = 0; trainId < trainsCnt; ++trainId)
            {
                var singleScheduleInNet = scheduleInNet.GetSchedule()[trainId];
                var stationsVertexes = calcPossibleInOutPointForTrain(singleScheduleInNet, pathCalculators, stationNet.GetConnections());
                infos.Add(new());
                for (int stationId = 0; stationId < singleScheduleInNet.MovementPath.Count; ++stationId)
                {
                    infos[trainId].Add(new());
                    infos[trainId][stationId].inputVertexes = stationsVertexes[stationId].Item1;
                    infos[trainId][stationId].outputVertexes = stationsVertexes[stationId].Item2;
                    if (stationId == 0)
                    {
                        infos[trainId][stationId].timeOfEnter = singleScheduleInNet.StartTime;
                    } else
                    {
                        infos[trainId][stationId].timeOfEnter = infos[trainId][stationId - 1].timeOfExit;
                        int maxConnectionLen = 0;
                        foreach (InputVertex inputVertex in infos[trainId][stationId].inputVertexes)
                        {
                            maxConnectionLen = Math.Max(maxConnectionLen, lengthOfExternalConnectionWtihVertex[inputVertex]);
                        }
                        int trainExternalSpeed = singleScheduleInNet.Train.GetSpeed();
                        infos[trainId][stationId].timeOfEnter += (maxConnectionLen + trainExternalSpeed - 1) / trainExternalSpeed;
                    }
                    infos[trainId][stationId].timeOfExit = infos[trainId][stationId].timeOfEnter + singleScheduleInNet.MovementPath[stationId].StopTime;
                    int maxInternalPathLength = pathCalculators[singleScheduleInNet.MovementPath[stationId].Station].maxLengthOfPath(
                        infos[trainId][stationId].inputVertexes, infos[trainId][stationId].outputVertexes,
                        singleScheduleInNet.Train.GetLength(), singleScheduleInNet.MovementPath[stationId].PlatformType);
                    int trainInternalSpeed = singleScheduleInNet.Train.GetSpeed();
                    infos[trainId][stationId].timeOfExit += (maxInternalPathLength + singleScheduleInNet.Train.GetLength() + trainInternalSpeed - 1) / trainInternalSpeed;
                }
            }

            CpModel model = new CpModel();
            // variables[trainId][stationId][varId]
            List<List<List<BoolVar>>> vars = new();
            // meaningOfVariable[var] = {trainId, stationId, Vertex}
            // и означает, что если данная переменна true, то поезд trainId проходит на станции stationId через вершину Vertex;
            Dictionary<BoolVar, Tuple<int, int, Vertex>> meaningOfVar = new();
            for (int trainId = 0; trainId < trainsCnt; ++trainId)
            {
                vars.Add(new());
                for (int stationId = 0; stationId < infos[trainId].Count; ++stationId)
                {
                    vars[trainId].Add(new());
                    var info = infos[trainId][stationId];

                    List<ILiteral> varsForChooseExactlyOne = new();
                    foreach (InputVertex inputVertex in info.inputVertexes)
                    {
                        BoolVar var = model.NewBoolVar($"{trainId}, {stationId}, {inputVertex.getId()}");
                        vars[trainId][stationId].Add(var);
                        meaningOfVar[var] = new(trainId, stationId, inputVertex);
                        varsForChooseExactlyOne.Add(var);
                    }
                    model.AddExactlyOne(varsForChooseExactlyOne);
                    varsForChooseExactlyOne = new();
                    foreach (OutputVertex outputVertex in info.outputVertexes)
                    {
                        BoolVar var = model.NewBoolVar($"{trainId}, {stationId}, {outputVertex.getId()}");
                        vars[trainId][stationId].Add(var);
                        meaningOfVar[var] = new(trainId, stationId, outputVertex);
                        varsForChooseExactlyOne.Add(var);
                    }
                    model.AddExactlyOne(varsForChooseExactlyOne);
                }
            }

            // Добавляем consrtraint в которых описывается структура графа.
            List<ILiteral> varsForOr = new(2); varsForOr.Add(null); varsForOr.Add(null);
            for (int trainId = 0; trainId < trainsCnt; ++trainId)
            {
                for (int stationId = 0; stationId < infos[trainId].Count; ++stationId)
                {
                    SingleTrainScheduleInNet singleTrainSchedule = scheduleInNet.GetSchedule()[trainId];
                    PathCalculator calculator = pathCalculators[singleTrainSchedule.MovementPath[stationId].Station];

                    // Добавляем условия на внешние ребра.
                    if (stationId != 0)
                    {
                        for (int inputId = 0; inputId < infos[trainId][stationId].inputVertexes.Count; ++inputId)
                        {
                            OutputVertex previousVertex = previousVertexOutsideOfStation[infos[trainId][stationId].inputVertexes[inputId]];
                            int outputId = infos[trainId][stationId - 1].outputVertexes.FindIndex(v => v == previousVertex);
                            outputId += infos[trainId][stationId - 1].inputVertexes.Count;
                            model.AddImplication(vars[trainId][stationId - 1][outputId], vars[trainId][stationId][inputId]);
                            model.AddImplication(vars[trainId][stationId][inputId], vars[trainId][stationId - 1][outputId]);
                        }
                    }

                    // Добавляем условия на внутренние пути на станции.
                    List<InputVertex> inputs = new(1); inputs.Add(null);
                    List<OutputVertex> outputs = new(1); outputs.Add(null);
                    for (int inputId = 0; inputId < infos[trainId][stationId].inputVertexes.Count; ++inputId)
                    {
                        InputVertex inputVertex = infos[trainId][stationId].inputVertexes[inputId];
                        inputs[0] = inputVertex;
                        for (int outputId = 0; outputId < infos[trainId][stationId].outputVertexes.Count; ++outputId)
                        {
                            OutputVertex outputVertex = infos[trainId][stationId].outputVertexes[outputId];
                            outputs[0] = outputVertex;
                            if (calculator.maxLengthOfPath(inputs, outputs,
                                singleTrainSchedule.Train.GetLength(), singleTrainSchedule.MovementPath[stationId].PlatformType) == 0)
                            {
                                // Если оказались здесь, то нет пути по станции которая проходит через нужную платформу
                                varsForOr[0] = vars[trainId][stationId][inputId].Not();
                                varsForOr[1] = vars[trainId][stationId][outputId + infos[trainId][stationId].inputVertexes.Count].Not();
                                model.AddBoolOr(varsForOr);
                            }
                        }
                    }

                }
            }

            // Добавляем constraint на пересечение во времени и месте въезда/выезда.
            for (int trainId0 = 0; trainId0 < trainsCnt; ++trainId0)
            {
                SingleTrainScheduleInNet schedule0 = scheduleInNet.GetSchedule()[trainId0];
                for (int trainId1 = trainId0 + 1; trainId1 < trainsCnt; ++trainId1)
                {
                    SingleTrainScheduleInNet schedule1 = scheduleInNet.GetSchedule()[trainId1];
                    for (int stationId0 = 0; stationId0 < infos[trainId0].Count; ++stationId0)
                    {
                        StopPointOfPath stopPoint0 = schedule0.MovementPath[stationId0];
                        for (int stationId1 = 0; stationId1 < infos[trainId1].Count; ++stationId1)
                        {
                            StopPointOfPath stopPoint1 = schedule1.MovementPath[stationId1];
                            if (stopPoint0.Station != stopPoint1.Station) { continue; }

                            // для въездов
                            for (int inputId0 = 0; inputId0 < infos[trainId0][stationId0].inputVertexes.Count; ++inputId0)
                            {
                                InputVertex inputVertex0 = infos[trainId0][stationId0].inputVertexes[inputId0];
                                for (int inputId1 = 0; inputId1 < infos[trainId1][stationId1].inputVertexes.Count; ++inputId1)
                                {
                                    InputVertex inputVertex1 = infos[trainId1][stationId1].inputVertexes[inputId1];
                                    if (inputVertex0 != inputVertex1) { continue; }

                                    Tuple<int, int> blockingTime0 = calcBlockingTimeForVertex(infos[trainId0][stationId0].timeOfEnter, schedule0.Train);
                                    Tuple<int, int> blockingTime1 = calcBlockingTimeForVertex(infos[trainId1][stationId1].timeOfEnter, schedule1.Train);
                                    if (HelpFunctions.hasIntervalsIntersection(blockingTime0, blockingTime1))
                                    {
                                        varsForOr[0] = vars[trainId0][stationId0][inputId0].Not();
                                        varsForOr[1] = vars[trainId1][stationId1][inputId1].Not();
                                        model.AddBoolOr(varsForOr);
                                    }
                                }
                            }
                            // для выъездов
                            for (int outputId0 = 0; outputId0 < infos[trainId0][stationId0].outputVertexes.Count; ++outputId0)
                            {
                                OutputVertex outputVertex0 = infos[trainId0][stationId0].outputVertexes[outputId0];
                                for (int outputId1 = 0; outputId1 < infos[trainId1][stationId1].outputVertexes.Count; ++outputId1)
                                {
                                    OutputVertex outputVertex1 = infos[trainId1][stationId1].outputVertexes[outputId1];
                                    if (outputVertex0 != outputVertex1) { continue; }

                                    Tuple<int, int> blockingTime0 = calcBlockingTimeForVertex(infos[trainId0][stationId0].timeOfExit, schedule0.Train);
                                    Tuple<int, int> blockingTime1 = calcBlockingTimeForVertex(infos[trainId1][stationId1].timeOfExit, schedule1.Train);
                                    if (HelpFunctions.hasIntervalsIntersection(blockingTime0, blockingTime1))
                                    {
                                        varsForOr[0] = vars[trainId0][stationId0][infos[trainId0][stationId0].inputVertexes.Count + outputId0].Not();
                                        varsForOr[1] = vars[trainId1][stationId1][infos[trainId1][stationId1].inputVertexes.Count + outputId1].Not();
                                        model.AddBoolOr(varsForOr);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Добавляем constraint на невозможность обгона на ребре между станциями
            for (int trainId0 = 0; trainId0 < trainsCnt; ++trainId0)
            {
                SingleTrainScheduleInNet schedule0 = scheduleInNet.GetSchedule()[trainId0];
                for (int trainId1 = trainId0 + 1; trainId1 < trainsCnt; ++trainId1)
                {
                    SingleTrainScheduleInNet schedule1 = scheduleInNet.GetSchedule()[trainId1];
                    for (int stationId0 = 1; stationId0 < infos[trainId0].Count; ++stationId0)
                    {
                        StopPointOfPath stopPoint0 = schedule0.MovementPath[stationId0];
                        for (int stationId1 = 1; stationId1 < infos[trainId1].Count; ++stationId1)
                        {
                            StopPointOfPath stopPoint1 = schedule1.MovementPath[stationId1];
                            if (stopPoint0 != stopPoint1) { continue; }
                            for (int inputId0 = 0; inputId0 < infos[trainId0][stationId0].inputVertexes.Count; ++inputId0)
                            {
                                InputVertex inputVertex0 = infos[trainId0][stationId0].inputVertexes[inputId0];
                                Tuple<int, int> blockingTimeOutput0 = calcBlockingTimeForVertex(infos[trainId0][stationId0 - 1].timeOfExit, schedule0.Train);
                                Tuple<int, int> blockingTimeInput0 = calcBlockingTimeForVertex(infos[trainId0][stationId0].timeOfEnter, schedule0.Train);
                                for (int inputId1 = 0; inputId1 < infos[trainId1][stationId1].inputVertexes.Count; ++inputId1)
                                {
                                    InputVertex inputVertex1 = infos[trainId1][stationId1].inputVertexes[inputId1];
                                    Tuple<int, int> blockingTimeOutput1 = calcBlockingTimeForVertex(infos[trainId1][stationId1 - 1].timeOfExit, schedule1.Train);
                                    Tuple<int, int> blockingTimeInput1 = calcBlockingTimeForVertex(infos[trainId1][stationId1].timeOfEnter, schedule1.Train);
                                    if (inputVertex0 != inputVertex1) { continue; }
                                    int idFirstOnOutput = -1;
                                    if (blockingTimeOutput0.Item2 < blockingTimeOutput1.Item1)
                                    {
                                        idFirstOnOutput = 0;
                                    }
                                    if (blockingTimeOutput1.Item2 < blockingTimeOutput0.Item1)
                                    {
                                        idFirstOnOutput = 1;
                                    }
                                    if (idFirstOnOutput == -1) { continue; }
                                    int idFirstOnInput = -1;
                                    if (blockingTimeInput0.Item2 < blockingTimeInput1.Item1)
                                    {
                                        idFirstOnInput = 0;
                                    }
                                    if (blockingTimeInput1.Item2 < blockingTimeInput0.Item1)
                                    {
                                        idFirstOnInput = 1;
                                    }
                                    if (idFirstOnInput == -1) { continue; }
                                    if (idFirstOnInput == idFirstOnOutput) { continue; }
                                    varsForOr[0] = vars[trainId0][stationId0][inputId0].Not();
                                    varsForOr[1] = vars[trainId1][stationId1][inputId1].Not();
                                    model.AddBoolOr(varsForOr);
                                }
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

            Dictionary<StationGraph, TrainSchedule> schedules = new();
            Dictionary<Train, SingleTrainScheduleInNet> tmpTrainToOrigSchedule = new();
            foreach (StationGraph station in stationNet.GetStations())
            {
                schedules[station] = new TrainSchedule(station);
            }
            for (int trainId = 0; trainId < trainsCnt; ++trainId)
            {
                SingleTrainScheduleInNet schedule = scheduleInNet.GetSchedule()[trainId]; 
                Train origTrain = schedule.Train;
                for (int stationId = 0; stationId < infos[trainId].Count; ++stationId)
                {
                    StationGraph station = schedule.MovementPath[stationId].Station;
                    InputVertex? input = null;
                    OutputVertex? output = null;
                    int inputCnt = infos[trainId][stationId].inputVertexes.Count;
                    for (int inputId = 0; inputId < inputCnt; ++inputId)
                    {
                        if (solver.Value(vars[trainId][stationId][inputId]) == 1) {
                            input = infos[trainId][stationId].inputVertexes[inputId];
                            break;
                        }
                    }
                    for (int outputId = 0; outputId < infos[trainId][stationId].outputVertexes.Count; ++outputId)
                    {
                        if (solver.Value(vars[trainId][stationId][inputCnt + outputId]) == 1)
                        {
                            output = infos[trainId][stationId].outputVertexes[outputId];
                            break;
                        }
                    }
                    if (input is null || output is null)
                    {
                        throw new Exception("error in solver algorythm ((( solution finded but no input or output vertex");
                    }
                    SingleTrainSchedule tmpSchedule = new(infos[trainId][stationId].timeOfEnter, infos[trainId][stationId].timeOfExit,
                        schedule.MovementPath[stationId].StopTime, input, output);
                    Train tmpTrain = new Train(origTrain.GetLength(), origTrain.GetSpeed(), schedule.MovementPath[stationId].PlatformType);
                    tmpTrainToOrigSchedule[tmpTrain] = schedule;
                    if (!schedules[station].TryAddTrainSchedule(tmpTrain, tmpSchedule))
                    {
                        throw new Exception("error in solver algorythm ((( problem with adding tmp schedules");
                    }
                }
            }

            foreach (StationGraph station in stationNet.GetStations())
            {
                Solver stationSolver = new Solver(station, timeInaccuracy);
                StationWorkPlan solution = stationSolver.CalculateWorkPlan(schedules[station]);

                workPlan.Schedules[station] = new();
                foreach (var i in schedules[station].GetSchedule())
                {
                    workPlan.Schedules[station][tmpTrainToOrigSchedule[i.Key]] = i.Value;    
                }

                var trainPlatforms = solution.TrainPlatforms;
                Dictionary<Tuple<Train, SingleTrainSchedule>, Edge> origTrainPlatfroms = new();
                foreach (var i in trainPlatforms)
                {
                    Train origTrain = tmpTrainToOrigSchedule[i.Key.Item1].Train;
                    origTrainPlatfroms[new(origTrain, i.Key.Item2)] = i.Value;
                }
                solution.TrainPlatforms = origTrainPlatfroms;
                workPlan.Plans[station] = solution;
            }

            return workPlan;
        }

        private Tuple<int, int> calcBlockingTimeForVertex(int time, Train train)
        {
            int speed = train.GetSpeed();
            return new(time - timeInaccuracy, time + timeInaccuracy + (train.GetLength() + speed - 1) / speed);
        }
        private static List<Tuple<List<InputVertex>, List<OutputVertex>>> calcPossibleInOutPointForTrain(
            SingleTrainScheduleInNet trainSchedule,
            Dictionary<StationGraph, PathCalculator> pathCalculators,
            List<Tuple<InputVertex, OutputVertex, int>> externalConnections)
        {
            Dictionary<OutputVertex, InputVertex> nextVertexOutsideOfStation = new();
            foreach (var connection in externalConnections)
            {
                nextVertexOutsideOfStation[connection.Item2] = connection.Item1;
            }

            List<StopPointOfPath> movementPath = trainSchedule.MovementPath;

            List<Tuple<List<InputVertex>, List<OutputVertex>>> result = new();
            List<InputVertex> inputVertices = new();
            List<OutputVertex> outputVertices = new();
            for (int i = 0; i < movementPath.Count; ++i)
            {
                StationGraph station = movementPath[i].Station;

                inputVertices = new();
                if (i == 0)
                {
                    inputVertices.Add(trainSchedule.Start);
                } else
                {
                    foreach (var externalCon in externalConnections)
                    {
                        if (outputVertices.Contains(externalCon.Item2)
                            && station.GetInputVertices().Contains(externalCon.Item1))
                        {
                            inputVertices.Add(externalCon.Item1);
                        }
                    }
                }
                if (inputVertices.Count == 0)
                {
                    throw new Exception("for some train, there is no track with the right criteria (movementPath is bad)");
                }

                outputVertices = new();
                PathCalculator calculator = pathCalculators[station];
                foreach (var platform in calculator.platformsWithDirection)
                {
                    Edge platformEdge = HelpFunctions.findEdge(platform.Item1, platform.Item2);
                    if (!HelpFunctions.checkPlatfrom(
                        platformEdge,
                        movementPath[i].PlatformType,
                        trainSchedule.Train.GetLength()
                        )) { continue; }

                    foreach (var pathFromPlatform in calculator.pathsStartFromPlatform[platform]) 
                    {
                        int pathCntVertexes = pathFromPlatform.GetVertices().Count;
                        OutputVertex outputVertex = (OutputVertex) pathFromPlatform.GetVertices()[pathCntVertexes - 1];
                        if (outputVertices.Contains(outputVertex)) { continue; }
                        if (i != movementPath.Count - 1)
                        {
                            if (!nextVertexOutsideOfStation.ContainsKey(outputVertex)) { continue; }
                            InputVertex nextVertex = nextVertexOutsideOfStation[outputVertex];
                            if (!movementPath[i + 1].Station.GetInputVertices().Contains(nextVertex)) { continue; }
                        }
                        bool hasGoodInputVertex = false;
                        foreach (InputVertex inputVertex in inputVertices)
                        {
                            hasGoodInputVertex |= pathCalculators[station].hasPathFromInputVertexToPlatform(inputVertex, platform);
                        }
                        if (hasGoodInputVertex)
                        {
                            outputVertices.Add(outputVertex);
                        }
                    }
                }
                if (outputVertices.Count == 0)
                {
                    throw new Exception("for some train, there is no track with the right criteria (movementPath is bad)");
                }

                result.Add(new(inputVertices, outputVertices));
            }
            return result;
        }

        private class TrainInfoOnStation
        {
            // список подходящих входов на станция для какого-то поезда
            public List<InputVertex> inputVertexes;
            // список подходящих выходов со станции для какого-то поезда
            public List<OutputVertex> outputVertexes;
            // время въезда на станцию
            public int timeOfEnter;
            // время выезда со станции
            public int timeOfExit;
        }
    }
}
