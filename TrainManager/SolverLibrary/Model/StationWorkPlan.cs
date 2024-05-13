﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.PlanUnit;
using SolverLibrary.Model.TrainInfo;

namespace SolverLibrary.Model
{
    public class StationWorkPlan
    {
        private List<SwitchPlanUnit> SwitchUnits = new List<SwitchPlanUnit>();
        private List<TrafficLightPlanUnit> TrafficUnits = new List<TrafficLightPlanUnit>();
        [JsonProperty]
        public readonly Dictionary<Tuple<Train, SingleTrainSchedule>, Edge> trainPlatforms = new();

        public StationWorkPlan() { }


        internal void AddTrainWithPlatform(Train train, SingleTrainSchedule schedule,Edge platform)
        {
            trainPlatforms[new(train, schedule)] = platform;
        }
        internal void AddSwitchPlanUnit(SwitchPlanUnit switchPlanUnit)
        {
            SwitchUnits.Add(switchPlanUnit);
        }


        internal void AddTrafficLightPlanUnit(TrafficLightPlanUnit trafficLightPlanUnit)
        {
            TrafficUnits.Add(trafficLightPlanUnit);
        }

        public List<TrafficLightPlanUnit> GetTrafficLightPlanUnits() {
            TrafficUnits.Sort();
            return TrafficUnits;
        }

        public List<SwitchPlanUnit> GetSwitchPlanUnits()
        {
            SwitchUnits.Sort();
            return SwitchUnits;
        }
    }
}
