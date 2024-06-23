using SolverLibrary.Model.TrainInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class TrainSheduleInNet
    {
        private List<SingleTrainScheduleInNet> schedule = new();

        public TrainSheduleInNet() { }

        public void AddSingleTrainSchedule(SingleTrainScheduleInNet trainSchedule)
        {
            schedule.Add(trainSchedule);
        }

        public List<SingleTrainScheduleInNet> GetSchedule()
        {
            return schedule;
        }
    }
}
