Resultado dentra da Unity:
<img width="1888" height="744" alt="Screenshot_74" src="https://github.com/user-attachments/assets/69653aac-e8ab-4026-946c-13220ddfc42a" />

Como usar:
Compile e coloque pbf e node.db no mesmo diretorio do executavel, basta executar e os arquivos das chunks deve aparecer dentro de Output/Roads

Pipeline:
1) OSM PARSING
   - Carrega Ways e Nodes do OSM.
   - Constrói o grafo viário (RoadNode e RoadEdge).

2) ROAD GEOMETRY
   - Cada RoadEdge possui uma lista de PathPoints.
   - Os PathPoints representam a linha central da rua.

3) TRIANGULATION
   - MeshTriangulator.TriangulateEdge()
     converte a linha central em uma faixa de triângulos.
   - Calcula normais laterais.
   - Gera vértices esquerdo/direito.
   - Cria os quads da pista.

4) CHUNKING
   - MeshChunker recebe os triângulos gerados.
   - Determina em qual chunk a geometria pertence.
   - Mantém cada malha separada.
   - Agrupa por chunk para exportação.

5) EXPORT
   - ChunkWriter grava os chunks em arquivos binários.
   - Cada registro contém:
       ChunkX
       ChunkY
       FeatureType
       Metadata (RoadType, Width)
       Lista de vértices dos triângulos

6) UNITY LOADING
   - ChunkDebugLoader lê o arquivo binário.
   - Reconstrói os vértices.
   - Cria MeshFilter, MeshRenderer e MeshCollider.
   - Organiza as ruas por tipo:
       ChunkRoads
         ├─ residential
         ├─ primary
         ├─ secondary
         └─ motorway
