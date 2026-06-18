using System.Drawing;

namespace OSMRoadMeshChunkGenerator.Models
{
    public class RoadNode
    {
        public long Id { get; set; }
        public PointF Position { get; set; }

        public List<RoadEdge> ConnectedEdges { get; set; } = new();

        public RoadNode(long id, PointF position)
        {
            Id = id;
            Position = position;
        }
    }

    public class RoadEdge
    {
        public RoadNode NodeA { get; set; }
        public RoadNode NodeB { get; set; }

        public List<PointF> PathPoints { get; set; } = new();

        public float Width { get; set; }
        public string HighwayType { get; set; }

        public RoadEdge(RoadNode a, RoadNode b, float width, string highwayType)
        {
            NodeA = a;
            NodeB = b;
            Width = width;
            HighwayType = highwayType;
        }

        public RoadNode GetOppositeNode(RoadNode node)
        {
            if (node == NodeA) return NodeB;
            if (node == NodeB) return NodeA;
            return null;
        }
    }

    public class RoadGraph
    {
        public Dictionary<long, RoadNode> Nodes { get; set; } = new();
        public List<RoadEdge> Edges { get; set; } = new();
    }
}
