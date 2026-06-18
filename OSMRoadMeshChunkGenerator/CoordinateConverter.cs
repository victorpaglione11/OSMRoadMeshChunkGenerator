using OSMRoadMeshChunkGenerator.Models;

namespace OSMRoadMeshChunkGenerator
{
    public static class CoordinateConverter
    {
        private const double EarthRadius = 6378137.0;
        private const double DegToRad = Math.PI / 180.0;

        private const double SeoulLat = 37.5662952;
        private const double SeoulLon = 126.9779451;

        private static readonly (double x, double z) SeoulOrigin = Project(SeoulLat, SeoulLon);

        public static PointF LatLonToWorld(double lat, double lon)
        {
            var (x, z) = Project(lat, lon);

            return new PointF(
                (float)(x - SeoulOrigin.x),
                (float)(z - SeoulOrigin.z)
            );
        }

        private static (double x, double z) Project(double lat, double lon)
        {
            double x = EarthRadius * (lon * DegToRad);

            double latRad = lat * DegToRad;
            double z = EarthRadius * Math.Log(Math.Tan(Math.PI / 4.0 + latRad / 2.0));

            return (x, z);
        }
    }
}
