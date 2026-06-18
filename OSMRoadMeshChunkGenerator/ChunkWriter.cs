using OSMRoadMeshChunkGenerator.Models;
using System.Collections.Concurrent;

namespace OSMRoadMeshChunkGenerator
{
    public class ChunkWriter : IDisposable
    {
        private readonly string _root;

        private readonly ConcurrentDictionary<string, BinaryWriter> _writers = new();

        public ChunkWriter(string root)
        {
            _root = root;
        }

        public void Write(int chunkX, int chunkY, FeatureType type, List<PointF> points, Metadata metadata)
        {
            string category = GetCategory(type);
            string folder = Path.Combine(_root, category);

            Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, $"{chunkX}_{chunkY}.bin");

            var writer = _writers.GetOrAdd(filePath, path =>
                new BinaryWriter(
                    new BufferedStream(
                        new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)
                    )
                )
            );

            lock (writer)
            {
                // HEADER DO SEGMENTO
                writer.Write(chunkX);
                writer.Write(chunkY);
                writer.Write((int)type);

                // METADATA
                writer.Write(points.Count);
                switch (type)
                {
                    case FeatureType.Road:
                        writer.Write(metadata.HighwayType);// string contendo o tipo ex: motorway
                        writer.Write(metadata.RoadWidth);
                        break;
                }

                // GEOMETRIA
                foreach (var p in points)
                {
                    writer.Write(p.X);
                    writer.Write(p.Z);
                }

                writer.Flush();
            }
        }

        private static string GetCategory(FeatureType type)
        {
            return type switch
            {
                FeatureType.Road => "Roads",
                FeatureType.Building => "Buildings",
                FeatureType.Railway => "Railways",
                FeatureType.Water => "Water",
                FeatureType.Forest => "Forests",
                _ => "Unknown"
            };
        }

        public void Dispose()
        {
            foreach (var w in _writers.Values)
                w.Dispose();

            _writers.Clear();
        }
    }
}
