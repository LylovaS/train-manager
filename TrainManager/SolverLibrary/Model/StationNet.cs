using SolverLibrary.Model.Graph;
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
        private List<Edge> _connections = new();
        public StationNet() {
        }

        public void AddStation(StationGraph station)
        {
            _stations.Add(station);
        }

        public void AddStationsConnection(Edge edge)
        {
            _connections.Add(edge);
        }

        public List<StationGraph> GetStations()
        {
            return _stations;
        }

        public List<Edge> GetConnections() 
        {
            return _connections;
        }
    }
}
