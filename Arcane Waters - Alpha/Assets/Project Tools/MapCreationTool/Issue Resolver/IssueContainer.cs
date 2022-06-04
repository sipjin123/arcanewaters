using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Linq;

namespace MapCreationTool.IssueResolving
{
   public class IssueContainer : MonoBehaviour
   {
      [Header("Provide tiles of forest biome")]
      [SerializeField]
      private WrongLayerIssue[] wrongLayerIssues = new WrongLayerIssue[0];
      [SerializeField]
      private TileBase[] tilesSharedBetweenPrefabs = new TileBase[0];
      [SerializeField]
      private UnnecessaryTileIssue[] unnecessaryTileIssues = new UnnecessaryTileIssue[0];
      [SerializeField]
      private RemovedDataFieldIssue[] removedDataFieldIssues = new RemovedDataFieldIssue[0];
      [SerializeField]
      private WrongDataFieldValueIssue[] wrongDataFieldValueIssues = new WrongDataFieldValueIssue[0];

      public Dictionary<TileBase, WrongLayerIssue> wrongLayer { get; private set; }
      public Dictionary<TileBase, TileNotPrefabIssue> tileToPrefab { get; private set; }
      public Dictionary<TileBase, UnnecessaryTileIssue> unnecessaryTiles { get; private set; }
      public Dictionary<GameObject, HashSet<string>> removedDataFields { get; private set; }
      public Dictionary<GameObject, (string field, string desiredValue)> wrongDataFieldValues { get; private set; }

      public void fillDataStructures () {
         if (AssetSerializationMaps.biomeSpecific == null) {
            AssetSerializationMaps.load();
         }

         HashSet<TileBase> dontMakePrefabEntries = new HashSet<TileBase>();
         foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
            if (biome == Biome.Type.None) {
               continue;
            }
            foreach (TileBase tile in tilesSharedBetweenPrefabs) {
               TileBase t = AssetSerializationMaps.getTile(
                  AssetSerializationMaps.getIndex(tile, Biome.Type.Forest),
                  biome);
               dontMakePrefabEntries.Add(t);
            }
         }

         unnecessaryTiles = new Dictionary<TileBase, UnnecessaryTileIssue>();
         foreach (UnnecessaryTileIssue issue in unnecessaryTileIssues) {
            foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
               if (biome == Biome.Type.None) {
                  continue;
               }
               TileBase t = AssetSerializationMaps.getTile(
                     AssetSerializationMaps.getIndex(issue.tile, Biome.Type.Forest),
                     biome);
               unnecessaryTiles.Add(t, new UnnecessaryTileIssue {
                  tile = t,
                  layer = issue.layer,
                  sublayer = issue.sublayer
               });
            }
         }

         wrongLayer = new Dictionary<TileBase, WrongLayerIssue>();
         foreach (WrongLayerIssue wl in wrongLayerIssues) {
            foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
               if (biome == Biome.Type.None) {
                  continue;
               }

               TileBase tile = AssetSerializationMaps.getTile(
                  AssetSerializationMaps.getIndex(wl.tileBase, Biome.Type.Forest),
                  biome);

               if (!wrongLayer.ContainsKey(tile)) {
                  wrongLayer.Add(tile, new WrongLayerIssue {
                     tileBase = tile,
                     fromLayer = wl.fromLayer,
                     fromSublayer = wl.fromSublayer,
                     toLayer = wl.toLayer,
                     toSublayer = wl.toSublayer
                  });
               }
            }
         }
         tileToPrefab = new Dictionary<TileBase, TileNotPrefabIssue>();
         TilesNotPrefabIssueConfig[] prefabConfigs = GetComponentsInChildren<TilesNotPrefabIssueConfig>();
         WrongLayerIssueConfig[] wrongLayerConfigs = GetComponentsInChildren<WrongLayerIssueConfig>();
         foreach (List<(int x, int y)> cluster in findClusters()) {
            TilesNotPrefabIssueConfig prefabConfig = configInCluster(cluster, prefabConfigs);
            WrongLayerIssueConfig wrongLayerConfig = configInCluster(cluster, wrongLayerConfigs);
            TileBase[,] tiles = getTiles(cluster);
            foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
               if (biome == Biome.Type.None) {
                  continue;
               }

               TileBase[,] biomeTiles = new TileBase[tiles.GetLength(0), tiles.GetLength(1)];
               for (int i = 0; i < tiles.GetLength(0); i++) {
                  for (int j = 0; j < tiles.GetLength(1); j++) {
                     if (tiles[i, j] != null) {
                        biomeTiles[i, j] = AssetSerializationMaps.getTile(
                           AssetSerializationMaps.getIndex(tiles[i, j], Biome.Type.Forest),
                           biome);
                     }
                  }
               }

               for (int i = 0; i < biomeTiles.GetLength(0); i++) {
                  for (int j = 0; j < biomeTiles.GetLength(1); j++) {
                     if (biomeTiles[i, j] != null) {
                        if (prefabConfig != null && !tileToPrefab.ContainsKey(biomeTiles[i, j]) && !dontMakePrefabEntries.Contains(biomeTiles[i, j])) {
                           tileToPrefab.Add(biomeTiles[i, j], new TileNotPrefabIssue {
                              allTiles = biomeTiles,
                              tileIndex = (i, j),
                              layer = prefabConfig.layer,
                              sublayer = prefabConfig.sublayer,
                              prefab = prefabConfig.prefab,
                              prefabOffset = prefabConfig.prefabOffset
                           });
                        } else if (wrongLayerConfig != null && !wrongLayer.ContainsKey(biomeTiles[i, j])) {
                           wrongLayer.Add(biomeTiles[i, j], new WrongLayerIssue {
                              tileBase = biomeTiles[i, j],
                              fromLayer = wrongLayerConfig.fromLayer,
                              toLayer = wrongLayerConfig.toLayer,
                              fromSublayer = wrongLayerConfig.fromSublayer,
                              toSublayer = wrongLayerConfig.toSublayer
                           });
                        }
                     }
                  }
               }
            }
         }

         removedDataFields = removedDataFieldIssues.GroupBy(df => df.prefab).ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(df => df.dataField)));
         wrongDataFieldValues = wrongDataFieldValueIssues.ToDictionary(wd => wd.prefab, wd => (wd.dataField, wd.desiredValue));
      }

      private TileBase[,] getTiles (List<(int x, int y)> indexes) {
         Tilemap tilemap = GetComponentInChildren<Tilemap>();
         (int x, int y) min = (indexes.Min(c => c.x), indexes.Min(c => c.y));
         (int x, int y) max = (indexes.Max(c => c.x), indexes.Max(c => c.y));

         TileBase[,] result = new TileBase[max.x - min.x + 1, max.y - min.y + 1];

         foreach ((int x, int y) index in indexes) {
            result[index.x - min.x, index.y - min.y] = tilemap.GetTile(new Vector3Int(index.x, index.y, 0));
         }

         return result;
      }

      private T configInCluster<T> (List<(int x, int y)> cluster, IEnumerable<T> configs) where T : MonoBehaviour {
         (float x, float y) min = (cluster.Min(c => c.x), cluster.Min(c => c.y));
         (float x, float y) max = (cluster.Max(c => c.x) + 1, cluster.Max(c => c.y) + 1);
         foreach (T config in configs) {
            Vector3 p = config.transform.position;
            if (p.x >= min.x && p.y >= min.y && p.x <= max.x && p.y <= max.y) {
               return config;
            }
         }

         return null;
      }

      private IEnumerable<List<(int x, int y)>> findClusters () {
         Tilemap tilemap = GetComponentInChildren<Tilemap>();
         Vector2Int mSize = new Vector2Int(tilemap.size.x, tilemap.size.y);
         bool[,] proccessed = new bool[mSize.x, mSize.y];
         bool[,] tileMatrix = new bool[mSize.x, mSize.y];

         for (int i = 0; i < mSize.x; i++) {
            for (int j = 0; j < mSize.y; j++) {
               tileMatrix[i, j] = tilemap.HasTile(new Vector3Int(i, j, 0));
            }
         }

         for (int i = 0; i < mSize.x; i++) {
            for (int j = 0; j < mSize.y; j++) {
               if (!tileMatrix[i, j] || proccessed[i, j]) continue;

               proccessed[i, j] = true;

               List<(int x, int y)> result = new List<(int x, int y)>();
               Queue<(int x, int y)> q = new Queue<(int x, int y)>();

               q.Enqueue((i, j));

               while (q.Count > 0) {
                  (int x, int y) cur = q.Dequeue();

                  if (cur.x < mSize.x - 1 && !proccessed[cur.x + 1, cur.y] && tileMatrix[cur.x + 1, cur.y]) {
                     q.Enqueue((cur.x + 1, cur.y));
                     proccessed[cur.x + 1, cur.y] = true;
                  }
                  if (cur.x > 0 && !proccessed[cur.x - 1, cur.y] && tileMatrix[cur.x - 1, cur.y]) {
                     q.Enqueue((cur.x - 1, cur.y));
                     proccessed[cur.x - 1, cur.y] = true;
                  }
                  if (cur.y < mSize.y - 1 && !proccessed[cur.x, cur.y + 1] && tileMatrix[cur.x, cur.y + 1]) {
                     q.Enqueue((cur.x, cur.y + 1));
                     proccessed[cur.x, cur.y + 1] = true;
                  }
                  if (cur.y > 0 && !proccessed[cur.x, cur.y - 1] && tileMatrix[cur.x, cur.y - 1]) {
                     q.Enqueue((cur.x, cur.y - 1));
                     proccessed[cur.x, cur.y - 1] = true;
                  }
                  result.Add(cur);
               }

               yield return result;
            }
         }
      }

      [Serializable]
      public struct WrongLayerIssue
      {
         public TileBase tileBase;
         public string fromLayer;
         public string toLayer;
         public int fromSublayer;
         public int toSublayer;
      }

      public struct TileNotPrefabIssue
      {
         public TileBase[,] allTiles;
         public (int x, int y) tileIndex;
         public string layer;
         public int sublayer;
         public GameObject prefab;
         public Vector2 prefabOffset;

         public (int x, int y) tileMatrixSize => (allTiles.GetLength(0), allTiles.GetLength(1));
      }

      [Serializable]
      public struct UnnecessaryTileIssue
      {
         public TileBase tile;
         public string layer;
         public int sublayer;
      }

      [Serializable]
      public struct RemovedDataFieldIssue
      {
         public GameObject prefab;
         public string dataField;
      }

      [Serializable]
      public struct WrongDataFieldValueIssue
      {
         public GameObject prefab;
         public string dataField;
         public string desiredValue;
      }
   }
}
