
/*
Mesh Generation Pipeline

OSM -> RoadNode/RoadEdge -> PathPoints
-> TriangulateEdge() gera os triângulos da rua
-> MeshChunker separa a geometria por chunk
-> ChunkWriter salva em .bin
-> Unity carrega os chunks e recria as Meshes

Resultado:
- Geometria pronta para renderização
- Estrutura compatível com streaming por chunk
- Base pronta para pathfinding usando o grafo RoadNode/RoadEdge (A*, Dijkstra, etc)
*/



using OSMRoadMeshChunkGenerator;
using OSMRoadMeshChunkGenerator.Models;
using System.Numerics;

const float ChunkSize = 5000f;

string pbfFile = "south-korea.osm.pbf"; // Get on Geofabrik

Log("Iniciando geração de malha");

if (!File.Exists(pbfFile))
{
    Log("ERRO: Arquivo PBF não encontrado", true);
    return;
}

Log("Criando diretório de saída...");
Directory.CreateDirectory("Output");

Log("Inicializando componentes");
NodeStore nodeStore = new("nodes.db"); //Generate nodes.db using PBF file from Geofabrik
ChunkWriter chunkWriter = new("Output");
GraphBuilder graphBuilder = new();
MeshChunker meshChunker = new MeshChunker(ChunkSize);

Log($"Construindo o grafo a partir do arquivo: {pbfFile} (Isso pode levar algum tempo)...");
var graph = graphBuilder.BuildGraph(pbfFile, nodeStore);
Log($"Grafo construído com sucesso. Total de Edges: {graph.Edges.Count} | Total de Nodes: {graph.Nodes.Count}");

Log("Iniciando a triangulação das edges...");
int edgeCount = 0;
foreach (var edge in graph.Edges)
{
    List<Vector3> edgeTriangles = MeshTriangulator.TriangulateEdge(edge);
    meshChunker.AddTriangles(edgeTriangles, edge.HighwayType, edge.Width);

    edgeCount++;
    if (edgeCount % 50000 == 0)
    {
        Log($"Progresso das Edges: {edgeCount}/{graph.Edges.Count} processadas");
    }
}
Log($"Finalizada a triangulação de todas as {edgeCount} Edges");

Log("Iniciando a triangulação dos cruzamentos (Nodes/Intersections)...");
int nodeCount = 0;
foreach (var node in graph.Nodes.Values)
{
    var poly = IntersectionMath.GenerateIntersection2(node);
    List<Vector3> intersectionTriangles = MeshTriangulator.TriangulateIntersection(poly);

    string intersectionType = "residential";
    float intersectionWidth = 4f;

    if (node.ConnectedEdges.Count > 0)
    {
        intersectionType = node.ConnectedEdges[0].HighwayType;
        intersectionWidth = node.ConnectedEdges[0].Width;
    }

    meshChunker.AddTriangles(intersectionTriangles, intersectionType, intersectionWidth);

    nodeCount++;
    if (nodeCount % 50000 == 0)
    {
        Log($"Progresso dos Nodes: {nodeCount}/{graph.Nodes.Count} processados.");
    }
}
Log($"Finalizada a triangulação de todos os {nodeCount} Nodes");

Log("Exportando os chunks gerados para o disco...");
meshChunker.ExportAll(chunkWriter, FeatureType.Road);

Log("Processo concluído com sucesso! Verifique a pasta 'Output'");

static void Log(string message, bool isError = false)
{
    string timestamp = DateTime.Now.ToString("HH:mm:ss");
    if (isError)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{timestamp}] [ERROR] {message}");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine($"[{timestamp}] [INFO] {message}");
    }
}