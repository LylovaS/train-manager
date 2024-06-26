﻿using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;

namespace SolverLibrary.Model
{
    public class TrainSchedule
    {
        private Dictionary<Train, SingleTrainSchedule> schedule;
        private StationGraph? stationGraph;
        private HashSet<TrainType> trainTypes;

        public TrainSchedule(StationGraph stationGraph, int capacity = 5)
        {
            if (!stationGraph.CheckStationGraph())
            {
                throw new ArgumentException("Invalid station graph.");
            }
            this.stationGraph = stationGraph;
            schedule = new Dictionary<Train, SingleTrainSchedule>(capacity);
            trainTypes = new HashSet<TrainType>(stationGraph.GetEdgeTypes());
        }

        public TrainSchedule Clone()
        {
            TrainSchedule clone = new TrainSchedule(stationGraph);
            foreach (var s in schedule)
            {
                SingleTrainSchedule scheduleClone = new SingleTrainSchedule(s.Value.GetTimeArrival(), s.Value.GetTimeDeparture(), 
                    s.Value.GetTimeStop(), s.Value.GetVertexIn(), s.Value.GetVertexOut());
                clone.TryAddTrainSchedule(s.Key, scheduleClone);
            }
            return clone;
        }

        public Dictionary<Train, SingleTrainSchedule> GetSchedule()
        {
            return schedule;
        }

        public bool SetStationGraph(StationGraph stationGraph)
        {
            if (stationGraph.CheckStationGraph())
            {
                this.stationGraph = stationGraph;
                return true;
            }
            return false;
        }

        public bool CheckIfTrainScheduleExists(Train train)
        {
            return schedule.TryGetValue(train, out _);
        }

        public bool TryAddTrainSchedule(Train train, SingleTrainSchedule singleSchedule)
        {
            // check train
            if (CheckIfTrainScheduleExists(train))
            {
                return false;
            }
            if (!trainTypes.Contains(train.GetTrainType()) && singleSchedule.GetTimeStop() != 0)
            {
                return false;
            }
            // check schedule 
            Vertex start = singleSchedule.GetVertexIn();
            //InputVertex start = singleSchedule.GetVertexIn();
            OutputVertex end = singleSchedule.GetVertexOut();
            if (!stationGraph.GetInputVertices().Contains(start) || !stationGraph.GetOutputVertices().Contains(end))
            {
                return false;
            }
            CheckModifiedTrainSchedule(train, singleSchedule);
            schedule[train] = singleSchedule;
            return true;
        }
        public void RemoveTrainSchedule(Train train)
        {
            schedule.Remove(train);
        }

        public bool ChangeTrainSchedule(Train train, SingleTrainSchedule singleSchedule)
        {
            CheckModifiedTrainSchedule(train, singleSchedule);
            schedule[train] = singleSchedule;
            return false;
        }

        public bool CheckModifiedTrainSchedule(Train train, SingleTrainSchedule singleSchedule)
        {
            foreach (SingleTrainSchedule s in schedule.Values)
            {
                if (s.GetVertexIn == singleSchedule.GetVertexIn && s.GetTimeArrival == singleSchedule.GetTimeArrival)
                {
                    return false;
                }
                else if (s.GetVertexOut == singleSchedule.GetVertexOut && s.GetTimeDeparture == singleSchedule.GetTimeDeparture)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
