using Microsoft.Data.Sqlite;
using OSMRoadMeshChunkGenerator.Models;

namespace OSMRoadMeshChunkGenerator
{
    public class NodeStore : IDisposable
    {
        private readonly SqliteConnection _conn;

        private readonly SqliteCommand _cmd;

        private readonly SqliteParameter _id;
        public int CacheCount
        {
            get
            {
                return _cache.Count;
            }
        }

        private readonly Dictionary<long, NodeData>
            _cache = new(500000);

        public NodeStore(
            string database)
        {
            _conn = new SqliteConnection($"Data Source={database}");

            _conn.Open();

            _cmd = _conn.CreateCommand();

            _cmd.CommandText =
            """
        SELECT Lat,Lon
        FROM Nodes
        WHERE Id = $id
        """;

            _id = _cmd.CreateParameter();

            _id.ParameterName = "$id";

            _cmd.Parameters.Add(_id);
        }

        public bool TryGetNode(long nodeId, out NodeData node)
        {
            if (_cache.TryGetValue(nodeId, out node))
            {
                return true;
            }

            _id.Value = nodeId;

            using var reader = _cmd.ExecuteReader();

            if (!reader.Read())
            {
                node = default!;
                return false;
            }

            node =
                new NodeData(
                    (float)reader.GetDouble(0),
                    (float)reader.GetDouble(1));

            if (_cache.Count >= 500000)
            {
                _cache.Clear();
            }

            _cache[nodeId] = node;

            return true;
        }

        public void Dispose()
        {
            _conn.Dispose();
        }
    }
}
