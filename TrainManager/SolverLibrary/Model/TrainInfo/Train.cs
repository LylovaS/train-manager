using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model.TrainInfo
{
    public enum TrainType { PASSENGER, CARGO, NONE };
    public class Train
    {
        [JsonProperty(Order = 1)]
        private int length;
        [JsonProperty(Order = 2)]
        private int speed;
        [JsonProperty(Order = 3)]
        private TrainType trainType;
        public Train(int length, int speed, TrainType trainType)
        {
            SetLength(length);
            SetSpeed(speed);
            this.trainType = trainType;
        }
        public int GetLength() { return length; }
        public void SetLength(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentException("Train length must be positive.");
            }
            this.length = length;
        }
        public int GetSpeed() { return speed; }
        public void SetSpeed(int speed)
        {
            if (speed <= 0)
            {
                throw new ArgumentException("Train speed must be positive.");
            }
            this.speed = speed;
        }
        public TrainType GetTrainType() { return trainType; }
        public void SetTrainType(TrainType trainType) { this.trainType = trainType; }
    }
}
