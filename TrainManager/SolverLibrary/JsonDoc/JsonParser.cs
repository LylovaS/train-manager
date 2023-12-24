using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SolverLibrary.Model;
using SolverLibrary.Model.Graph;
using SolverLibrary.Model.Graph.VertexTypes;
using SolverLibrary.Model.TrainInfo;

namespace SolverLibrary.JsonDoc
{
    public class JsonParser
    {
        public static List<Train> LoadJsonTrains(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string json = sr.ReadToEnd();
                var settings = new JsonSerializerSettings { Converters = new JsonConverter[] { new TypePrefixEnumConverter() } };
                //var json = JsonConvert.DeserializeObject<List<Train>>(Formatting.Indented, settings);
                List<Train>? trains = JsonConvert.DeserializeObject<List<Train>>(json, settings);
                sr.Close();
                if (trains != null)
                {
                    return trains;
                }
                else
                {
                    throw new Exception("Couldn't deserialize trains.");
                }
            }
        }

        public class JsonStationGraph
        {
            private List<JsonVertex> vertices;
            private List<JsonEdge> edges;
            public JsonStationGraph(List<JsonVertex> vertices, List<JsonEdge> edges)
            {
                this.vertices = vertices;
                this.edges = edges;
            }
            public List<JsonVertex> GetVertices() { return vertices; }
            public List<JsonEdge> GetEdges() { return edges; }
        }
        public class JsonVertex
        {
            private int id;
            private VertexType vertexType;
            private List<Tuple<int, int>> edgeConnections;

            public JsonVertex(int id, VertexType vertexType, List<Tuple<int, int>> edgeConnections)
            {
                this.id = id;
                this.vertexType = vertexType;
                this.edgeConnections = edgeConnections;
            }
            public int GetId() { return id; }
            public VertexType GetVertexType() { return vertexType; }
            public List<Tuple<int, int>> GetEdgeConnections() { return edgeConnections; }

        }
        public class JsonEdge
        {
            private int length;
            private int startId;
            private int endId;
            private TrainType edgeType;

            public JsonEdge(int length, int startId, int endId, TrainType edgeType)
            {
                this.length = length;
                this.startId = startId;
                this.endId = endId;
                this.edgeType = edgeType;
            }
            public int GetLength() { return length; }
            public int GetStartId() { return startId; }
            public int GetEndId() { return endId; }
            public TrainType GetEdgeType() { return edgeType; }
        }

        public static StationGraph LoadJsonStationGraph(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string json = sr.ReadToEnd();
                var settings = new JsonSerializerSettings { Converters = new JsonConverter[] { new TypePrefixEnumConverter() } };
                JsonStationGraph? jsonGraph = JsonConvert.DeserializeObject<JsonStationGraph>(json, settings);
                sr.Close();
                if (jsonGraph != null)
                {
                    List<JsonVertex> jsonVertices = jsonGraph.GetVertices();
                    List<Vertex> vertices = new List<Vertex>();
                    foreach (JsonVertex v in jsonVertices)
                    {
                        switch (v.GetVertexType())
                        {
                            case VertexType.INPUT:
                                {
                                    vertices.Add(new InputVertex(v.GetId()));
                                    break;
                                }
                            case VertexType.OUTPUT:
                                {
                                    vertices.Add(new OutputVertex(v.GetId()));
                                    break;
                                }
                            case VertexType.DEADEND:
                                {
                                    vertices.Add(new DeadEndVertex(v.GetId()));
                                    break;
                                }
                            case VertexType.TRAFFIC:
                                {
                                    vertices.Add(new TrafficLightVertex(v.GetId()));
                                    break;
                                }
                            case VertexType.CONNECTION:
                                {
                                    vertices.Add(new ConnectionVertex(v.GetId()));
                                    break;
                                }
                            case VertexType.SWITCH:
                                {
                                    vertices.Add(new SwitchVertex(v.GetId()));
                                    break;
                                }
                        }
                    }
                    List<Edge> edges = new List<Edge>();
                    foreach (JsonEdge e in jsonGraph.GetEdges())
                    {
                        edges.Add(new Edge(e.GetLength(),
                            vertices.ElementAt(e.GetStartId()),
                            vertices.ElementAt(e.GetEndId()),
                            e.GetEdgeType()));
                    }
                    foreach (JsonVertex v in jsonVertices)
                    {
                        List<Tuple<int, int>> connections = v.GetEdgeConnections();
                        switch (v.GetVertexType())
                        {
                            case VertexType.INPUT:
                                {
                                    Edge? e1 = connections.ElementAt(0).Item1 >= 0 ? edges.ElementAt(connections.ElementAt(0).Item1) : null;
                                    Edge? e2 = connections.ElementAt(0).Item2 >= 0 ? edges.ElementAt(connections.ElementAt(0).Item2) : null;
                                    ((InputVertex)vertices.ElementAt(v.GetId())).SetEdge(e1 == null ? e2 : e1);
                                    break;
                                }
                            case VertexType.OUTPUT:
                                {
                                    Edge? e1 = connections.ElementAt(0).Item1 >= 0 ? edges.ElementAt(connections.ElementAt(0).Item1) : null;
                                    Edge? e2 = connections.ElementAt(0).Item2 >= 0 ? edges.ElementAt(connections.ElementAt(0).Item2) : null;
                                    ((OutputVertex)vertices.ElementAt(v.GetId())).SetEdge(e1 == null ? e2 : e1);
                                    break;
                                }
                            case VertexType.DEADEND:
                                {
                                    Edge? e1 = connections.ElementAt(0).Item1 >= 0 ? edges.ElementAt(connections.ElementAt(0).Item1) : null;
                                    Edge? e2 = connections.ElementAt(0).Item2 >= 0 ? edges.ElementAt(connections.ElementAt(0).Item2) : null;
                                    ((DeadEndVertex)vertices.ElementAt(v.GetId())).SetEdge(e1 == null ? e2 : e1);
                                    break;
                                }
                            case VertexType.TRAFFIC:
                                {
                                    Edge e1 = edges.ElementAt(connections.ElementAt(0).Item1);
                                    Edge e2 = edges.ElementAt(connections.ElementAt(0).Item2);
                                    ((TrafficLightVertex)vertices.ElementAt(v.GetId())).SetEdges(e1, e2);
                                    break;
                                }
                            case VertexType.CONNECTION:
                                {
                                    Edge e1 = edges.ElementAt(connections.ElementAt(0).Item1);
                                    Edge e2 = edges.ElementAt(connections.ElementAt(0).Item2);
                                    ((ConnectionVertex)vertices.ElementAt(v.GetId())).SetEdges(e1, e2);
                                    break;
                                }
                            case VertexType.SWITCH:
                                {
                                    Edge e1 = edges.ElementAt(connections.ElementAt(0).Item1);
                                    Edge e2 = edges.ElementAt(connections.ElementAt(0).Item2);
                                    Edge e3 = edges.ElementAt(connections.ElementAt(1).Item1);
                                    if (e3 == e2 || e3 == e1) { e3 = edges.ElementAt(connections.ElementAt(1).Item2); }
                                    ((SwitchVertex)vertices.ElementAt(v.GetId())).SetEdges(e1, e2, e3);
                                    break;
                                }
                        }
                    }
                    StationGraph graph = new StationGraph();
                    foreach (Vertex v in vertices)
                    {
                        graph.TryAddVerticeWithEdges(v);
                    }
                    return graph;
                }
                else
                {
                    throw new Exception("Couldn't parse station topology.");
                }
            }
        }

        public class JsonSingleSchedule
        {
            private Train train;
            private int timeArrival, timeDeparture, timeStop;
            private int vertexIn, vertexOut;

            public JsonSingleSchedule(Train train, int timeArrival, int timeDeparture, int timeStop, int vertexIn, int vertexOut)
            {
                this.train = train;
                this.timeArrival = timeArrival;
                this.timeDeparture = timeDeparture;
                this.timeStop = timeStop;
                this.vertexIn = vertexIn;
                this.vertexOut = vertexOut;
            }
            public Train GetTrain() { return train; }
            public int GetTimeArrival() { return timeArrival; }
            public int GetTimeDeparture() { return timeDeparture; }
            public int GetTimeStop() { return timeStop; }
            public int GetVertexIn() { return vertexIn; }
            public int GetVertexOut() { return vertexOut; }
        }

        public static TrainSchedule LoadJsonTrainSchedule(string filename, StationGraph stationGraph)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string json = sr.ReadToEnd();
                var settings = new JsonSerializerSettings { Converters = new JsonConverter[] { new TypePrefixEnumConverter() } };
                List<JsonSingleSchedule>? jsonSchedule = JsonConvert.DeserializeObject<List<JsonSingleSchedule>>(json, settings);
                sr.Close();
                if (jsonSchedule != null)
                {
                    TrainSchedule schedule = new TrainSchedule(stationGraph);
                    List<Vertex> vertices = stationGraph.GetVertices().ToList();
                    foreach (JsonSingleSchedule s in jsonSchedule)
                    {
                        InputVertex? v1 = (InputVertex)vertices.Find((Vertex v) => { return v.getId().Equals(s.GetVertexIn()); });
                        OutputVertex? v2 = (OutputVertex)vertices.Find((Vertex v) => { return v.getId().Equals(s.GetVertexOut()); });
                        if (v1 != null && v2 != null)
                        {
                            SingleTrainSchedule singleSchedule = new SingleTrainSchedule(s.GetTimeArrival(),
                                s.GetTimeDeparture(), s.GetTimeStop(),
                                v1, v2);
                            schedule.TryAddTrainSchedule(s.GetTrain(), singleSchedule);
                        }
                    }
                    return schedule;
                }
                else
                {
                    throw new Exception("Couldn't deserialize train schedule.");
                }
            }
        }
    }
    public class TypePrefixEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isNullable = (Nullable.GetUnderlyingType(objectType) != null);
            Type enumType = (Nullable.GetUnderlyingType(objectType) ?? objectType);
            if (!enumType.IsEnum)
                throw new JsonSerializationException(string.Format("type {0} is not a enum type", enumType.FullName));
            var prefix = enumType.Name + "_";

            if (reader.TokenType == JsonToken.Null)
            {
                if (!isNullable)
                    throw new JsonSerializationException();
                return null;
            }

            // Strip the prefix from the enum components (if any).
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.String)
            {
                token = (JValue)string.Join(", ", token.ToString().Split(',').Select(s => s.Trim()).Select(s => s.StartsWith(prefix) ? s.Substring(prefix.Length) : s).ToArray());
            }

            using (var subReader = token.CreateReader())
            {
                while (subReader.TokenType == JsonToken.None)
                    subReader.Read();
                return base.ReadJson(subReader, objectType, existingValue, serializer); // Use base class to convert
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var array = new JArray();
            using (var tempWriter = array.CreateWriter())
                base.WriteJson(tempWriter, value, serializer);
            var token = array.Single();

            if (token.Type == JTokenType.String && value != null)
            {
                var enumType = value.GetType();
                var prefix = enumType.Name + "_";
                token = (JValue)string.Join(", ", token.ToString().Split(',').Select(s => s.Trim()).Select(s => (!char.IsNumber(s[0]) && s[0] != '-') ? prefix + s : s).ToArray());
            }

            token.WriteTo(writer);
        }
    }
}
