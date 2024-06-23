using Google.Protobuf.Collections;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.TrainInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class StationNetWorkPlan
    {
        // Словарь в котором по станции получаем план работы станции
        private Dictionary<StationGraph, StationWorkPlan> plans = new();
        // Словарь в котором по станции получаем словарь, в котором по расписанию поезда из расписания в сети получаем расписание этого поезда на станции.
        private Dictionary<StationGraph, Dictionary<SingleTrainScheduleInNet, SingleTrainSchedule>> schedules = new();

        public Dictionary<StationGraph, StationWorkPlan> Plans { get => plans; set => plans = value; }
        public Dictionary<StationGraph, Dictionary<SingleTrainScheduleInNet, SingleTrainSchedule>> Schedules { get => schedules; set => schedules = value; }
    }
}
