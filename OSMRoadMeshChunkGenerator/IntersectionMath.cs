using OSMRoadMeshChunkGenerator.Models;
using System.Numerics;

namespace OSMRoadMeshChunkGenerator
{
    public class IntersectionMesh
    {
        public long NodeId { get; set; }
        public List<Vector2> PolygonCorners { get; set; } = new();
    }

    public static class IntersectionMath
    {
        public static IntersectionMesh GenerateIntersection2(RoadNode node)
        {
            var mesh = new IntersectionMesh
            {
                NodeId = node.Id
            };

            var center = new Vector2(node.Position.X, node.Position.Z);

            var borderPoints = new List<Vector2>();

            foreach (var edge in node.ConnectedEdges)
            {
                PointF nextPointRaw = edge.NodeA == node
                    ? (edge.PathPoints.Count > 1
                        ? edge.PathPoints[1]
                        : edge.NodeB.Position)
                    : (edge.PathPoints.Count > 1
                        ? edge.PathPoints[^2]
                        : edge.NodeA.Position);

                var dir = new Vector2(
                    nextPointRaw.X - center.X,
                    nextPointRaw.Z - center.Y);

                if (dir.LengthSquared() < 0.0001f)
                    continue;

                dir = Vector2.Normalize(dir);

                float halfWidth = edge.Width * 0.5f;

                var right = new Vector2(-dir.Y, dir.X);

                borderPoints.Add(center + right * halfWidth);
                borderPoints.Add(center - right * halfWidth);
            }

            if (borderPoints.Count < 3)
                return mesh;

            borderPoints = borderPoints
                .OrderBy(p =>
                    MathF.Atan2(
                        p.Y - center.Y,
                        p.X - center.X))
                .ToList();

            const float minDistance = 0.1f;

            var filtered = new List<Vector2>();

            foreach (var p in borderPoints)
            {
                bool tooClose = filtered.Any(
                    existing =>
                        Vector2.Distance(existing, p) < minDistance);

                if (!tooClose)
                    filtered.Add(p);
            }

            mesh.PolygonCorners.AddRange(filtered);

            return mesh;
        }
        public static IntersectionMesh GenerateIntersection(RoadNode node)
        {
            var mesh = new IntersectionMesh { NodeId = node.Id };
            var center = new Vector2(node.Position.X, node.Position.Z);
            var roadData = new List<RoadRay>();

            foreach (var edge in node.ConnectedEdges)
            {
                PointF nextPointRaw = edge.NodeA == node
                    ? (edge.PathPoints.Count > 1 ? edge.PathPoints[1] : edge.NodeB.Position)
                    : (edge.PathPoints.Count > 1 ? edge.PathPoints[^2] : edge.NodeA.Position);

                var dir = new Vector2(nextPointRaw.X - center.X, nextPointRaw.Z - center.Y);
                if (dir.LengthSquared() == 0) continue;

                dir = Vector2.Normalize(dir);
                float angle = MathF.Atan2(dir.Y, dir.X);

                roadData.Add(new RoadRay { Edge = edge, Direction = dir, Angle = angle, HalfWidth = edge.Width / 2f });
            }

            roadData = roadData.OrderByDescending(r => r.Angle).ToList();

            int count = roadData.Count;

            if (count == 1)
            {
                var r = roadData[0];
                var right = new Vector2(-r.Direction.Y, r.Direction.X) * r.HalfWidth;
                mesh.PolygonCorners.Add(center + right);
                mesh.PolygonCorners.Add(center - right);
                return mesh;
            }

            for (int i = 0; i < count; i++)
            {
                var current = roadData[i];
                var next = roadData[(i + 1) % count];

                var currentRightDir = new Vector2(-current.Direction.Y, current.Direction.X);
                var nextLeftDir = new Vector2(next.Direction.Y, -next.Direction.X);

                var currentEdgePoint = center + (currentRightDir * current.HalfWidth);
                var nextEdgePoint = center + (nextLeftDir * next.HalfWidth);

                if (TryIntersectLines(currentEdgePoint, current.Direction, nextEdgePoint, next.Direction, out Vector2 corner))
                {
                    float dot = Vector2.Dot(current.Direction, next.Direction);
                    dot = Math.Clamp(dot, -1f, 1f);

                    float angle = MathF.Acos(dot);

                    var centerToCorner = corner - center;
                    bool isAhead = Vector2.Dot(centerToCorner, current.Direction) > 0 || Vector2.Dot(centerToCorner, next.Direction) > 0;

                    const float MITER_LIMIT = 4.0f;

                    float miterLength =
                        current.HalfWidth / MathF.Max(0.001f, MathF.Sin(angle * 0.5f));

                    if (miterLength > current.HalfWidth * MITER_LIMIT || !isAhead)
                    {
                        mesh.PolygonCorners.Add(currentEdgePoint);
                        mesh.PolygonCorners.Add(nextEdgePoint);
                    }
                    else
                    {
                        mesh.PolygonCorners.Add(corner);
                    }
                }
                else
                {
                    mesh.PolygonCorners.Add(currentEdgePoint);
                }
            }

            return mesh;
        }

        private class RoadRay
        {
            public RoadEdge Edge { get; set; }
            public Vector2 Direction { get; set; }
            public float Angle { get; set; }
            public float HalfWidth { get; set; }
        }

        private static bool TryIntersectLines(Vector2 p1, Vector2 dir1, Vector2 p2, Vector2 dir2, out Vector2 intersection)
        {
            intersection = Vector2.Zero;

            float crossProduct = (dir1.X * dir2.Y) - (dir1.Y * dir2.X);

            if (MathF.Abs(crossProduct) < 0.001f)
                return false;

            Vector2 diff = p2 - p1;
            float t1 = ((diff.X * dir2.Y) - (diff.Y * dir2.X)) / crossProduct;

            intersection = p1 + (dir1 * t1);
            return true;
        }
    }
}
