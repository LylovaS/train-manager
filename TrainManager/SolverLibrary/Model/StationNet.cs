using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverLibrary.Model
{
    public class StationNet
    {
        // все станции в сети
        private List<StationGraph> _stations = new();
        // пути соединяющие входы одной станции с выходом другой станции.
        private List<Tuple<InputVertex, OutputVertex, int>> _connections = new();
        public StationNet() {
        }

        public void AddStation(StationGraph station)
        {
            _stations.Add(station);
        }

        public void AddStationsConnection(InputVertex inputVertex, OutputVertex outputVertex, int length)
        {
            foreach (var connection in _connections)
            {
                if (connection.Item1 == inputVertex )
                {
                    throw new Exception("external connections already have a connection with such InputVertex");
                }
                if (connection.Item2 == outputVertex)
                {
                    throw new Exception("external connections already have a connection with such OutputVertex");
                }
            }
            _connections.Add(new(inputVertex, outputVertex, length));
        }

        public List<StationGraph> GetStations()
        {
            return _stations;
        }

        public List<Tuple<InputVertex, OutputVertex, int>> GetConnections() 
        {
            return _connections;
        }

        public bool checkStationNet()
        {
            foreach (var station in _stations)
            {
                if (!station.CheckStationGraph())
                {
                    return false;
                }
            }
            return true;
        }
    }
}
