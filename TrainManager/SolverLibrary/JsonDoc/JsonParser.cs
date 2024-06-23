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
            [JsonProperty(Order = 1)]
            private List<JsonVertex> vertices;
            [JsonProperty(Order = 2)]
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
            [JsonProperty(Order = 1)]
            private int id;
            [JsonProperty(Order = 2)]
            private VertexType vertexType;
            [JsonProperty(Order = 3)]
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
            [JsonProperty(Order = 1)]
            private int id;
            [JsonProperty(Order = 2)]
            private int length;
            [JsonProperty(Order = 3)]
            private int startId;
            [JsonProperty(Order = 4)]
            private int endId;
            [JsonProperty(Order = 5)]
            private TrainType edgeType;

            public JsonEdge(int id, int length, int startId, int endId, TrainType edgeType)
            {
                this.id = id;
                this.length = length;
                this.startId = startId;
                this.endId = endId;
                this.edgeType = edgeType;
            }
            public int GetId() { return id; }
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
                var settings = new JsonSerializerSettings { Converters = new JsonConverter[] { 
                    new TypePrefixEnumConverter()
                } };
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
                        edges.Add(new Edge(e.GetId(), e.GetLength(),
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

        public static void SaveJsonStationGraph(string filename, StationGraph stationGraph)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                var settings = new JsonSerializerSettings { 
                    Converters = new JsonConverter[] { new TypePrefixEnumConverter() }
                };
                List<JsonVertex> jVertices = new List<JsonVertex>();
                foreach (Vertex v in stationGraph.GetVertices())
                {
                    List<Tuple<int,int>> edgeConnections = new List<Tuple<int,int>>();
                    foreach(Tuple<Edge?, Edge?> pair in v.GetEdgeConnections())
                    {
                        edgeConnections.Add(new Tuple<int, int>(
                            pair.Item1?.getId() == null ? -1 : pair.Item1.getId(),
                            pair.Item2?.getId() == null ? -1 : pair.Item2.getId()));
                    }
                    jVertices.Add(new JsonVertex(
                        v.getId(),
                        v.GetVertexType(),
                        edgeConnections
                        ));
                }
                List<JsonEdge> jEdges = new List<JsonEdge>();
                foreach (Edge e in stationGraph.GetEdges())
                {
                    jEdges.Add(new JsonEdge(
                        e.getId(),
                        e.GetLength(), 
                        e.GetStart().getId(), 
                        e.GetEnd().getId(), 
                        e.GetEdgeType()
                        ));
                }
                JsonStationGraph jGraph = new JsonStationGraph(
                    jVertices.OrderBy(v => v.GetId()).ToList(), 
                    jEdges.OrderBy(e => e.GetId()).ToList());

                string json = JsonConvert.SerializeObject(jGraph, Formatting.Indented, settings);
                if (json != null)
                {
                    sw.Write(json);
                    sw.Close();
                    return;
                }
                else
                {
                    sw.Close();
                    throw new Exception("Couldn't serialize station topology.");
                }
            }
        }

        public class JsonSingleSchedule
        {
            [JsonProperty(Order = 1)]
            private Train train;
            [JsonProperty(Order = 2)]
            private int timeArrival;
            [JsonProperty(Order = 3)]
            private int timeDeparture;
            [JsonProperty(Order = 4)]
            private int timeStop;
            [JsonProperty(Order = 5)]
            private int vertexIn;
            [JsonProperty(Order = 6)]
            private int vertexOut;

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

        public static void SaveJsonTrainSchedule(string filename, TrainSchedule trainSchedule)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                var settings = new JsonSerializerSettings
                {
                    Converters = new JsonConverter[] { new TypePrefixEnumConverter() }
                };
                List<JsonSingleSchedule> jSchedule = new List<JsonSingleSchedule>();
                foreach (KeyValuePair<Train, SingleTrainSchedule> pair in trainSchedule.GetSchedule())
                {
                    JsonSingleSchedule jss = new JsonSingleSchedule(
                        pair.Key,
                        pair.Value.GetTimeArrival(),
                        pair.Value.GetTimeDeparture(),
                        pair.Value.GetTimeStop(),
                        pair.Value.GetVertexIn().getId(),
                        pair.Value.GetVertexOut().getId());
                    jSchedule.Add(jss);
                }

                string json = JsonConvert.SerializeObject(jSchedule, Formatting.Indented, settings);
                if (json != null)
                {
                    sw.Write(json);
                    sw.Close();
                    return;
                }
                else
                {
                    sw.Close();
                    throw new Exception("Couldn't serialize train schedule.");
                }
            }
        }

        public class JsonSingleStationWorkPlan
        {
            [JsonProperty(Order = 1)]
            private Train train;
            //private JsonSingleSchedule schedule;
            [JsonProperty(Order = 2)]
            private JsonEdge edge;

            public JsonSingleStationWorkPlan(Train train, JsonEdge edge)
            {
                this.train = train;
                //this.schedule = schedule;
                this.edge = edge;
            }
            public Train GetTrain() { return train; }
            public JsonEdge GetEdge() { return edge; }
        }

        public static StationWorkPlan LoadJsonStationWorkPlan(string filename, StationGraph stationGraph)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string json = sr.ReadToEnd();
                var settings = new JsonSerializerSettings
                {
                    Converters = new JsonConverter[] {
                        new TypePrefixEnumConverter()
                }
                };
                List<JsonSingleStationWorkPlan> jWorkPlan = JsonConvert.DeserializeObject<List<JsonSingleStationWorkPlan>>(json, settings);
                StationWorkPlan workPlan = new StationWorkPlan();
                List<Vertex> vertices = stationGraph.GetVertices().ToList();
                foreach (JsonSingleStationWorkPlan p in jWorkPlan)
                {
                    Edge? edge = stationGraph.GetEdges().ToList().Find((Edge e) => { return e.getId().Equals(p.GetEdge().GetId()); });
                    if (edge != null)
                    {
                        workPlan.AddTrainWithPlatform(p.GetTrain(), edge);
                    }
                }
                sr.Close();
                if (workPlan != null)
                {
                    return workPlan;
                }
                else
                {
                    throw new Exception("Couldn't deserialize train schedule.");
                }
            }
        }

        public static void SaveJsonStationWorkPlan(string filename, StationWorkPlan workPlan)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                var settings = new JsonSerializerSettings
                {
                    Converters = new JsonConverter[] {
                        new TypePrefixEnumConverter()
                    }
                };
                List<JsonSingleStationWorkPlan> jWorkPlan = new List<JsonSingleStationWorkPlan>();
                foreach (KeyValuePair<Train, Edge> pair in workPlan.trainPlatforms)
                {
                    Train train = pair.Key;
                    Edge edge = pair.Value;
                    JsonEdge jEdge = new JsonEdge(
                        edge.getId(), 
                        edge.GetLength(),
                        edge.GetStart().getId(),
                        edge.GetEnd().getId(),
                        edge.GetEdgeType());
                    JsonSingleStationWorkPlan jsp = new JsonSingleStationWorkPlan(
                        train,
                        jEdge);
                    jWorkPlan.Add(jsp);
                }

                string json = JsonConvert.SerializeObject(jWorkPlan, Formatting.Indented, settings);
                //string json = JsonConvert.SerializeObject(workPlan, Formatting.Indented, settings);
                if (json != null)
                {
                    sw.Write(json);
                    sw.Close();
                    return;
                }
                else
                {
                    sw.Close();
                    throw new Exception("Couldn't serialize station work plan.");
                }
            }
        }
    }
    public class TypePrefixEnumConverter : StringEnumConverter
    {
        public override object? ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isNullable = (Nullable.GetUnderlyingType(objectType) != null);
            Type enumType = (Nullable.GetUnderlyingType(objectType) ?? objectType);
            if (!enumType.IsEnum)
                throw new JsonSerializationException(string.Format("type {0} is not an enum type", enumType.FullName));
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
            if (value != null && value is Vertex)
            {
                JObject jVertex = new JObject
                {
                    { "id", ((Vertex)value).getId() }
                };
                jVertex.WriteTo(writer);
                return;
            }
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
