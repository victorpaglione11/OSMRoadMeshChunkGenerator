using OSMRoadMeshChunkGenerator.Models;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace OSMRoadMeshChunkGenerator
{
    public class GraphBuilder
    {
        public RoadGraph BuildGraph(string pbfFile, NodeStore nodeStore)
        {
            var graph = new RoadGraph();
            var nodeUsageCount = new Dictionary<long, int>();

            using (var file = File.OpenRead(pbfFile))
            {
                var source = new PBFOsmStreamSource(file);
                foreach (var osm in source)
                {
                    if (osm is not Way way || DetectFeatureType(way) != FeatureType.Road)
                        continue;

                    foreach (long nodeId in way.Nodes)
                    {
                        if (nodeUsageCount.ContainsKey(nodeId))
                            nodeUsageCount[nodeId]++;
                        else
                            nodeUsageCount[nodeId] = 1;
                    }
                }
            }

            using (var file = File.OpenRead(pbfFile))
            {
                var source = new PBFOsmStreamSource(file);
                foreach (var osm in source)
                {
                    if (osm is not Way way || DetectFeatureType(way) != FeatureType.Road)
                        continue;

                    float roadWidth = GetRoadWidth(way.Tags, FeatureType.Road);
                    way.Tags.TryGetValue("highway", out var highwayType);

                    RoadNode lastIntersection = null;
                    List<PointF> currentSegmentPoints = new List<PointF>();

                    for (int i = 0; i < way.Nodes.Length; i++)
                    {
                        long nodeId = way.Nodes[i];

                        if (!nodeStore.TryGetNode(nodeId, out var osmNode))
                            continue;

                        var pos = CoordinateConverter.LatLonToWorld(osmNode.Lat, osmNode.Lon);
                        currentSegmentPoints.Add(pos);

                        bool isIntersection = (i == 0 || i == way.Nodes.Length - 1 || nodeUsageCount[nodeId] > 1);

                        if (isIntersection)
                        {
                            if (!graph.Nodes.TryGetValue(nodeId, out RoadNode graphNode))
                            {
                                graphNode = new RoadNode(nodeId, pos);
                                graph.Nodes[nodeId] = graphNode;
                            }

                            if (lastIntersection != null && lastIntersection != graphNode)
                            {
                                var edge = new RoadEdge(lastIntersection, graphNode, roadWidth, highwayType);

                                edge.PathPoints.AddRange(currentSegmentPoints);

                                lastIntersection.ConnectedEdges.Add(edge);
                                graphNode.ConnectedEdges.Add(edge);

                                graph.Edges.Add(edge);
                            }

                            lastIntersection = graphNode;
                            currentSegmentPoints.Clear();

                            currentSegmentPoints.Add(pos);
                        }
                    }
                }
            }

            return graph;
        }

        public static FeatureType DetectFeatureType(Way way)
        {
            var tags = way.Tags;

            if (tags.ContainsKey("highway"))
                return FeatureType.Road;

            if (tags.ContainsKey("building"))
                return FeatureType.Building;

            if (tags.ContainsKey("railway"))
                return FeatureType.Railway;

            if (tags.ContainsKey("waterway") ||
                (tags.TryGetValue("natural", out var n) && n == "water"))
                return FeatureType.Water;

            if ((tags.TryGetValue("landuse", out var l) && l == "forest") ||
                (tags.TryGetValue("natural", out var n2) && n2 == "wood"))
                return FeatureType.Forest;

            return FeatureType.None;
        }
        public static float GetRoadWidth(TagsCollectionBase tags, FeatureType type)
        {
            if (type != FeatureType.Road)
                return 0;

            if (!tags.TryGetValue("highway", out var h))
                return 4f;

            return h switch
            {
                "motorway" => 10.5f,
                "motorway_link" => 7.5f,

                "trunk" => 9.5f,
                "trunk_link" => 6.8f,

                "primary" => 8.0f,
                "primary_link" => 6.0f,

                "secondary" => 6.5f,
                "secondary_link" => 5.0f,

                "tertiary" => 5.5f,
                "tertiary_link" => 4.2f,

                "residential" => 3.8f,

                "unclassified" => 3.5f,

                "service" => 2.5f,

                "track" => 2.8f,

                "pedestrian" => 1.5f,

                "footway" => 1.0f,

                _ => 3.5f
            };
        }
    }
}
