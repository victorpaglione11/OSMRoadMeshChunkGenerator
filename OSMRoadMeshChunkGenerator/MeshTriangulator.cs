using OSMRoadMeshChunkGenerator.Models;
using System.Numerics;

namespace OSMRoadMeshChunkGenerator
{
    public static class MeshTriangulator
    {
        public static List<Vector3> TriangulateIntersection(IntersectionMesh intersection)
        {
            List<Vector3> triangles = new();
            var corners = intersection.PolygonCorners;

            if (corners == null || corners.Count < 3)
                return triangles;

            Vector3 pivot = new Vector3(corners[0].X, 0f, corners[0].Y);

            for (int i = 1; i < corners.Count - 1; i++)
            {
                Vector3 p2 = new Vector3(corners[i].X, 0f, corners[i].Y);
                Vector3 p3 = new Vector3(corners[i + 1].X, 0f, corners[i + 1].Y);

                // Ordem Sentido Horário??? DEBUG dentro da unity depois
                triangles.Add(pivot);
                triangles.Add(p2);
                triangles.Add(p3);
            }

            return triangles;
        }

        public static List<Vector3> TriangulateEdge(RoadEdge edge)
        {
            List<Vector3> triangles = new();

            var points = edge.PathPoints;

            if (points == null || points.Count < 2)
                return triangles;

            //FOR DEBUG
            //for (int i = 0; i < points.Count - 1; i++)
            //{
            //    float length =
            //        Vector2.Distance(
            //            new Vector2(points[i].X, points[i].Z),
            //            new Vector2(points[i + 1].X, points[i + 1].Z));

            //    if (length > 50f)
            //    {
            //        Console.WriteLine(
            //            $"LONG SEGMENT {length:F2}m");

            //        Console.WriteLine(
            //            $"{points[i].X},{points[i].Z}");

            //        Console.WriteLine(
            //            $"{points[i + 1].X},{points[i + 1].Z}");
            //    }
            //}

            float halfWidth = edge.Width * 0.5f;

            Vector3[] leftVertices = new Vector3[points.Count];
            Vector3[] rightVertices = new Vector3[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 current =
                    new(points[i].X, points[i].Z);

                Vector2 dirPrev;
                Vector2 dirNext;

                if (i == 0)
                {
                    dirPrev =
                        Vector2.Normalize(
                            new Vector2(
                                points[1].X - points[0].X,
                                points[1].Z - points[0].Z));

                    dirNext = dirPrev;
                }
                else if (i == points.Count - 1)
                {
                    dirPrev =
                        Vector2.Normalize(
                            new Vector2(
                                points[i].X - points[i - 1].X,
                                points[i].Z - points[i - 1].Z));

                    dirNext = dirPrev;
                }
                else
                {
                    dirPrev =
                        Vector2.Normalize(
                            new Vector2(
                                points[i].X - points[i - 1].X,
                                points[i].Z - points[i - 1].Z));

                    dirNext =
                        Vector2.Normalize(
                            new Vector2(
                                points[i + 1].X - points[i].X,
                                points[i + 1].Z - points[i].Z));
                }

                Vector2 normalPrev =
                    new(-dirPrev.Y, dirPrev.X);

                Vector2 normalNext =
                    new(-dirNext.Y, dirNext.X);

                Vector2 miter = normalPrev + normalNext;

                if (miter.LengthSquared() < 0.0001f)
                {
                    miter = normalPrev;
                }
                else
                {
                    miter = Vector2.Normalize(miter);
                }

                float dot = Vector2.Dot(miter, normalPrev);

                float miterLength =
                    dot > 0.0001f
                    ? halfWidth / dot
                    : halfWidth;

                // Evita miter spikes
                miterLength =
                    MathF.Min(
                        miterLength,
                        halfWidth * 2.0f);

                Vector2 left =
                    current - miter * miterLength;

                Vector2 right =
                    current + miter * miterLength;

                leftVertices[i] =
                    new Vector3(
                        left.X,
                        0f,
                        left.Y);

                rightVertices[i] =
                    new Vector3(
                        right.X,
                        0f,
                        right.Y);


            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 l0 = leftVertices[i];
                Vector3 r0 = rightVertices[i];

                Vector3 l1 = leftVertices[i + 1];
                Vector3 r1 = rightVertices[i + 1];

                triangles.Add(l0);
                triangles.Add(l1);
                triangles.Add(r1);

                triangles.Add(l0);
                triangles.Add(r1);
                triangles.Add(r0);
            }

            return triangles;
        }
    }
}
