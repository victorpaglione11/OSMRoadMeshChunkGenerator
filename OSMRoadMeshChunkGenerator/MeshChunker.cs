using OSMRoadMeshChunkGenerator.Models;
using System.Numerics;

namespace OSMRoadMeshChunkGenerator
{
    public class MeshChunker
    {
        private class ChunkMesh
        {
            public string HighwayType = "";
            public float Width;
            public List<PointF> Points = new();
        }

        private readonly Dictionary<(int ChunkX, int ChunkY), List<ChunkMesh>> _chunks = new();

        private readonly float _chunkSize;

        public MeshChunker(float chunkSize)
        {
            _chunkSize = chunkSize;
        }

        public void AddTriangles(List<Vector3> triangles,string highwayType,float width)
        {
            if (triangles == null || triangles.Count == 0)
                return;

            float minX = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxZ = float.MinValue;

            foreach (var v in triangles)
            {
                minX = MathF.Min(minX, v.X);
                minZ = MathF.Min(minZ, v.Z);

                maxX = MathF.Max(maxX, v.X);
                maxZ = MathF.Max(maxZ, v.Z);
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;

            int chunkX = (int)MathF.Floor(centerX / _chunkSize);
            int chunkY = (int)MathF.Floor(centerZ / _chunkSize);

            if (!_chunks.TryGetValue((chunkX, chunkY), out var meshes))
            {
                meshes = new List<ChunkMesh>();
                _chunks[(chunkX, chunkY)] = meshes;
            }

            ChunkMesh mesh = new()
            {
                HighwayType = highwayType ?? "unknown",
                Width = width
            };

            foreach (var v in triangles)
            {
                mesh.Points.Add(new PointF(v.X,v.Z));
            }

            meshes.Add(mesh);
        }

        public void ExportAll(ChunkWriter chunkWriter,FeatureType type)
        {
            foreach (var chunk in _chunks)
            {
                int chunkX = chunk.Key.ChunkX;
                int chunkY = chunk.Key.ChunkY;

                foreach (var mesh in chunk.Value)
                {
                    Metadata metadata = new()
                    {
                        HighwayType = mesh.HighwayType,
                        RoadWidth = mesh.Width
                    };

                    chunkWriter.Write(chunkX,chunkY,type,mesh.Points,metadata);
                }
            }
        }
    }
}
