using SolverLibrary.Model.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model.TrainInfo
{
    // Точка остановки поезда в пути поезда по сети станций
    public class StopPointOfPath
    {
        // Продолжительность остановки
        private int stopTime;
        // Станция на которой надо остановиться
        private StationGraph station;
        // Тип платформы на которой надо остановиться
        private TrainType platformType;
        public StopPointOfPath(int stopTime, StationGraph station, TrainType platformType)
        {
            this.stopTime = stopTime;
            this.station = station;
            this.platformType = platformType;
        }

        public int StopTime { get => stopTime; set => stopTime = value; }
        public StationGraph Station { get => station; set => station = value; }
        public TrainType PlatformType { get => platformType; set => platformType = value; }
    }
}
