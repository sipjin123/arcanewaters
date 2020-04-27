using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text.RegularExpressions;
using System.Globalization;

namespace MapCreationTool.Serialization
{
   public partial class Serializer
   {
      public static string serialize (
          Dictionary<string, Layer> layers,
          List<PlacedPrefab> prefabs,
          Biome.Type biome,
          EditorType editorType,
          Vector2Int size,
          bool prettyPrint = false) {
         try {
            return serialize002(layers, prefabs, biome, editorType, size, prettyPrint);
         } catch (Exception ex) {
            Debug.LogError("Failed to serialize map.");
            throw ex;
         }
      }

      private static string serialize002 (
          Dictionary<string, Layer> layers,
          List<PlacedPrefab> prefabs,
          Biome.Type biome,
          EditorType editorType,
          Vector2Int size,
          bool prettyPrint = false) {
         Func<GameObject, int> prefabToIndex = (go) => { return AssetSerializationMaps.getIndex(go, biome); };
         Func<TileBase, Vector2Int> tileToIndex = (tile) => { return AssetSerializationMaps.getIndex(tile, biome); };

         //Make prefab serialization object
         Prefab001[] prefabsSerialized
             = prefabs.Select(p =>
                 new Prefab001 {
                    i = prefabToIndex(p.original),
                    x = p.placedInstance.transform.position.x,
                    y = p.placedInstance.transform.position.y,
                    data = p.data.Select(data => new DataField { k = data.Key, v = data.Value }).ToArray()
                 }
             ).ToArray();

         //Make layer serialization object
         List<Layer002> layersSerialized = new List<Layer002>();

         foreach (var layerkv in layers) {
            if (layerkv.Value.hasTilemap) {
               layersSerialized.Add(new Layer002 {
                  id = layerkv.Key,
                  tiles = serializeTiles(layerkv.Value, tileToIndex),
                  sublayers = new SubLayer001[0]
               });
            } else {
               Layer002 ls = new Layer002 {
                  id = layerkv.Key,
                  tiles = new Tile001[0],
                  sublayers = new SubLayer001[layerkv.Value.subLayers.Length]
               };
               for (int i = 0; i < layerkv.Value.subLayers.Length; i++) {
                  ls.sublayers[i] = new SubLayer001 {
                     index = i,
                     tiles = serializeTiles(layerkv.Value.subLayers[i], tileToIndex)
                  };
               }
               layersSerialized.Add(ls);
            }
         }

         Project002 project = new Project002 {
            version = "0.0.2",
            biome = biome,
            layers = layersSerialized.ToArray(),
            prefabs = prefabsSerialized,
            editorType = editorType,
            size = size
         };

         return JsonUtility.ToJson(project, prettyPrint);
      }

      public static DeserializedProject deserializeForEditor (string data) {
         int version = extractVersion(data);

         switch (version) {
            case 000002:
               return deserialize002(data, true);
            default:
               throw new Exception("Failed to identify the file version.");
         }
      }

      public static string serializeExport (
         Dictionary<string, Layer> layers,
         List<PlacedPrefab> prefabs,
         Biome.Type biome,
         EditorType editorType,
         EditorConfig config,
         Dictionary<TileBase, TileCollisionType> collisionDictionary,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         return serializeExport001(layers, prefabs, biome, editorType, config, collisionDictionary, editorOrigin, editorSize);
      }

      private static string serializeExport001 (
         Dictionary<string, Layer> layers,
         List<PlacedPrefab> prefabs,
         Biome.Type biome,
         EditorType editorType,
         EditorConfig config,
         Dictionary<TileBase, TileCollisionType> collisionDictionary,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         Func<GameObject, int> prefabToIndex = (go) => { return AssetSerializationMaps.getIndex(go, biome); };
         Func<TileBase, Vector2Int> tileToIndex = (tile) => { return AssetSerializationMaps.getIndex(tile, biome); };

         //Make prefab serialization object
         ExportedPrefab001[] prefabsSerialized
             = prefabs.Select(p =>
                 new ExportedPrefab001 {
                    i = prefabToIndex(p.original),
                    x = p.placedInstance.transform.position.x,
                    y = p.placedInstance.transform.position.y,
                    d = p.data.Select(data => new DataField { k = data.Key, v = data.Value }).ToArray(),
                    iz = (p.placedInstance.GetComponent<ZSnap>()?.inheritedOffsetZ ?? 0) * 0.16f
                 }
             ).ToArray();

         List<MidExportLayer> midExportLayers = formMidExportLayers(layers, tileToIndex, collisionDictionary, config, editorType, editorOrigin, editorSize);

         ExportedProject001 project = new ExportedProject001 {
            version = "0.0.1",
            biome = biome,
            layers = formExportedLayers(midExportLayers, editorSize, getCancelledTileCollisions(prefabs)).ToArray(),
            specialTileChunks = formSpecialTileChunks(midExportLayers, editorSize, editorType),
            editorType = editorType,
            prefabs = prefabsSerialized,
         };

         return JsonUtility.ToJson(project);
      }

      private static HashSet<Vector2Int> getCancelledTileCollisions (List<PlacedPrefab> prefabs) {
         HashSet<Vector2Int> result = new HashSet<Vector2Int>();

         foreach (PlacedPrefab placedPrefab in prefabs) {
            SpiderWebMapEditor web = placedPrefab.placedInstance.GetComponent<SpiderWebMapEditor>();
            LedgeMapEditor ledge = placedPrefab.placedInstance.GetComponent<LedgeMapEditor>();

            if (web != null) {
               Vector3 from = web.transform.position + Vector3.up + Vector3.left * SpiderWebMapEditor.width * 0.5f;
               Vector3 to = from + new Vector3(SpiderWebMapEditor.width, web.height);
               addFromToTiles(result, from, to);
            } else if (ledge != null) {
               Vector3 from = ledge.transform.position + new Vector3(-ledge.width * 0.5f, -ledge.height + 0.5f);
               Vector3 to = from + new Vector3(ledge.width, ledge.height);
               addFromToTiles(result, from, to);
            }
         }

         return result;
      }

      private static void addFromToTiles (HashSet<Vector2Int> set, Vector3 from, Vector3 to) {
         Vector3Int fromCell = new Vector3Int(Mathf.FloorToInt(from.x), Mathf.FloorToInt(from.y), 0);
         Vector3Int toCell = new Vector3Int(Mathf.CeilToInt(to.x), Mathf.CeilToInt(to.y), 0);

         for (int i = fromCell.x; i < toCell.x; i++) {
            for (int j = fromCell.y; j < toCell.y; j++) {
               if (!set.Contains(new Vector2Int(i, j))) {
                  set.Add(new Vector2Int(i, j));
               }
            }
         }
      }

      private static List<ExportedLayer001> formExportedLayers (List<MidExportLayer> midLayers, Vector2Int editorSize, HashSet<Vector2Int> forceIgnoreCollisions) {
         List<ExportedLayer001> result = new List<ExportedLayer001>();

         // Set collision flag to tiles inside mid export data structure
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               if (forceIgnoreCollisions.Contains(new Vector2Int(i - editorSize.x / 2, j - editorSize.x / 2))) {
                  continue;
               }

               bool hasWater = false;
               for (int k = midLayers.Count - 1; k >= 0; k--) {
                  if (Layer.isWater(midLayers[k].layer) && midLayers[k].tileMatrix[i, j] != null) {
                     hasWater = true;
                  }
               }

               for (int k = midLayers.Count - 1; k >= 0; k--) {

                  if (midLayers[k].tileMatrix[i, j] == null)
                     continue;

                  // If docks have no water under them, don't place colliders
                  if (midLayers[k].layer.CompareTo("dock") == 0 && !hasWater) {
                     if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelEnabled || midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelDisabled) {
                        break;
                     } else {
                        continue;
                     }
                  }

                  if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.Enabled) {
                     midLayers[k].tileMatrix[i, j].tile.c = 1;
                  } else if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelEnabled) {
                     midLayers[k].tileMatrix[i, j].tile.c = 1;
                     break;
                  } else if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelDisabled) {
                     break;
                  }
               }
            }
         }

         // Form final layers
         foreach (MidExportLayer midLayer in midLayers) {
            List<ExportedTile001> exportedTiles = new List<ExportedTile001>();
            for (int i = 0; i < midLayer.tileMatrix.GetLength(0); i++) {
               for (int j = 0; j < midLayer.tileMatrix.GetLength(1); j++) {
                  if (midLayer.tileMatrix[i, j] != null)
                     exportedTiles.Add(midLayer.tileMatrix[i, j].tile);
               }
            }
            result.Add(new ExportedLayer001 {
               name = midLayer.layer,
               z = midLayer.z,
               tiles = exportedTiles.ToArray(),
               type = midLayer.type,
               sublayer = midLayer.subLayer
            });
         }

         return result;
      }

      private static List<MidExportLayer> formMidExportLayers (
         Dictionary<string, Layer> layers,
         Func<TileBase, Vector2Int> tileToIndex,
         Dictionary<TileBase, TileCollisionType> collisionDictionary,
         EditorConfig config,
         EditorType editorType,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         // Form mid export layers. These are layers with tile matrixes, whose tiles also have the collision setup info attached
         List<MidExportLayer> midLayers = new List<MidExportLayer>();

         foreach (var layerkv in layers) {
            // Set offset for layers that are supposed to be on top of player
            float zOffset = config.getLayers(editorType).First(l => l.layer.CompareTo(layerkv.Key) == 0).zOffset;

            if (layerkv.Value.hasTilemap) {
               if (layerkv.Value.tileCount == 0)
                  continue;

               midLayers.Add(new MidExportLayer {
                  z = getZ(config.getIndex(layerkv.Key, editorType), 0) + zOffset,
                  layer = layerkv.Key,
                  tileMatrix = getTilesWithCollisions(layerkv.Value, tileToIndex, collisionDictionary, editorOrigin, editorSize),
                  type = config.getLayerConfig(editorType, layerkv.Key).layerType,
                  subLayer = 0
               });
            } else {
               for (int i = 0; i < layerkv.Value.subLayers.Length; i++) {
                  if (layerkv.Value.subLayers[i].tileCount == 0)
                     continue;

                  midLayers.Add(new MidExportLayer {
                     z = getZ(config.getIndex(layerkv.Key, editorType), i) + zOffset,
                     layer = layerkv.Key,
                     tileMatrix = getTilesWithCollisions(layerkv.Value.subLayers[i], tileToIndex, collisionDictionary, editorOrigin, editorSize),
                     type = config.getLayerConfig(editorType, layerkv.Key).layerType,
                     subLayer = i
                  });
               }
            }
         }

         return midLayers;
      }

      private static SpecialTileChunk[] formSpecialTileChunks (List<MidExportLayer> midLayers, Vector2Int editorSize, EditorType editorType) {
         Func<string, int, TileBase, bool> stairInclude = (layer, sublayer, tile) => Layer.isStair(layer);
         Func<string, int, TileBase, bool> waterfallInclude = (layer, sublayer, tile) => Layer.isWater(layer) && sublayer == 4 && editorType == EditorType.Area;
         Func<string, int, TileBase, bool> vineInclude = (layer, sublayer, tile) => Layer.isVine(layer);

         IEnumerable<SpecialTileChunk> stairs = formSquareChunks(midLayers, editorSize, SpecialTileChunk.Type.Stair, stairInclude);
         IEnumerable<SpecialTileChunk> waterfalls = formSquareChunks(midLayers, editorSize, SpecialTileChunk.Type.Waterfall, waterfallInclude);
         IEnumerable<SpecialTileChunk> vines = formSquareChunks(midLayers, editorSize, SpecialTileChunk.Type.Vine, vineInclude);
         IEnumerable<SpecialTileChunk> currents = formWaterCurrentChunks(midLayers, editorSize, editorType);

         Dictionary<TileBase, int> tileToRug = Palette.instance.paletteData.tileToRugType;

         IEnumerable<SpecialTileChunk> rugs = Enumerable.Range(1, 4).SelectMany(type => {
            return formSquareChunks(
               midLayers,
               editorSize,
               (SpecialTileChunk.Type) (type + 4),
               (layer, sublayer, tile) => Layer.isRug(layer) && tileToRug.ContainsKey(tile) && tileToRug[tile] == type);
         });

         return stairs.Union(waterfalls).Union(vines).Union(currents).Union(rugs).ToArray();
      }

      private static IEnumerable<SpecialTileChunk> formWaterCurrentChunks (List<MidExportLayer> midLayers, Vector2Int editorSize, EditorType editorType) {
         if (editorType != EditorType.Area) {
            yield break;
         }

         bool[,] matrix = new bool[editorSize.x, editorSize.y];
         List<(int, int)> currentIndexes = new List<(int, int)>();

         // Find all current and waterfall tiles and set them in the matrix
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               foreach (MidExportLayer layer in midLayers) {
                  if (layer.tileMatrix[i, j] != null) {
                     if (Layer.isWater(layer.layer) && (layer.subLayer == 4 || layer.subLayer == 5)) {
                        matrix[i, j] = true;
                        currentIndexes.Add((i, j));
                        break;
                     }
                  }
               }
            }
         }

         // Take all current and waterfall tiles and expand outwards in the matrix
         int expandBy = 2;
         foreach ((int x, int y) index in currentIndexes) {
            for (int i = index.x - expandBy; i <= index.x + expandBy; i++) {
               for (int j = index.y - expandBy; j <= index.y + expandBy; j++) {
                  if (i < 0 || j < 0 || i >= editorSize.x || j >= editorSize.y) {
                     continue;
                  }
                  if (Mathf.Sqrt(Mathf.Pow(index.x - i, 2) + Mathf.Pow(index.y - j, 2)) > expandBy) {
                     continue;
                  }
                  for (int k = midLayers.Count - 1; k >= 0; k--) {
                     if (midLayers[k].tileMatrix[i, j] != null) {
                        if (Layer.isWater(midLayers[k].layer)) {
                           matrix[i, j] = true;
                        }
                        break;
                     }
                  }
               }
            }
         }

         // Remove any current tiles, that have only 1 neighbour current tile, thus smoothing the area
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               if (getNeightbourCount(matrix, (i, j)) < 2) {
                  matrix[i, j] = false;
               }
            }
         }

         GameObject colContainer = new GameObject("TEMP_WATER_CURRENT_COLLIDER_CONTAINER");
         colContainer.transform.position = Vector3.zero;
         Rigidbody2D rb = colContainer.AddComponent<Rigidbody2D>();
         rb.bodyType = RigidbodyType2D.Static;
         CompositeCollider2D comp = colContainer.AddComponent<CompositeCollider2D>();
         comp.generationType = CompositeCollider2D.GenerationType.Manual;

         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               if (matrix[i, j]) {
                  GameObject colOb = new GameObject("Cell");
                  colOb.transform.parent = colContainer.transform;
                  colOb.transform.localPosition = Vector3.zero;
                  // Add either a box or a triangle, based on neighbour count
                  if (getNeightbourCount(matrix, (i, j)) == 2) {
                     PolygonCollider2D poly = colOb.AddComponent<PolygonCollider2D>();
                     poly.usedByComposite = true;

                     SidesInt sur = BrushStroke.surroundingCount(matrix, (i, j), SidesInt.uniform(1));
                     // Bot left corner of cell
                     Vector2 bl = new Vector2(i - editorSize.x * 0.5f, j - editorSize.y * 0.5f);
                     if (sur.left == 0 && sur.top == 0) {
                        poly.points = new Vector2[] { bl, bl + new Vector2(1, 1), bl + new Vector2(1, 0) };
                     } else if (sur.top == 0 && sur.right == 0) {
                        poly.points = new Vector2[] { bl, bl + new Vector2(0, 1), bl + new Vector2(1, 0) };
                     } else if (sur.right == 0 && sur.bot == 0) {
                        poly.points = new Vector2[] { bl, bl + new Vector2(0, 1), bl + new Vector2(1, 1) };
                     } else if (sur.bot == 0 && sur.left == 0) {
                        poly.points = new Vector2[] { bl + new Vector2(0, 1), bl + new Vector2(1, 1), bl + new Vector2(1, 0) };
                     }
                  } else {
                     BoxCollider2D box = colOb.AddComponent<BoxCollider2D>();
                     box.offset = new Vector2(i - editorSize.x * 0.5f + 0.5f, j - editorSize.y * 0.5f + 0.5f);
                     box.usedByComposite = true;
                  }
               }
            }
         }

         comp.GenerateGeometry();

         PointPath[] paths = new PointPath[comp.pathCount];
         for (int i = 0; i < comp.pathCount; i++) {
            Vector2[] points = new Vector2[comp.GetPathPointCount(i)];
            comp.GetPath(i, points);
            paths[i] = new PointPath { points = points };
         }

         UnityEngine.Object.Destroy(colContainer);

         yield return new SpecialTileChunk {
            type = SpecialTileChunk.Type.Current,
            position = Vector2.zero,
            paths = paths
         };
      }

      private static int getNeightbourCount (bool[,] m, (int x, int y) index) {
         int nh = 0;

         if (index.x > 0 && m[index.x - 1, index.y]) {
            nh++;
         }
         if (index.y > 0 && m[index.x, index.y - 1]) {
            nh++;
         }
         if (index.x < m.GetLength(0) - 1 && m[index.x + 1, index.y]) {
            nh++;
         }
         if (index.y < m.GetLength(1) - 1 && m[index.x, index.y + 1]) {
            nh++;
         }

         return nh;
      }

      private static IEnumerable<SpecialTileChunk> formSquareChunks (List<MidExportLayer> midLayers, Vector2Int editorSize, SpecialTileChunk.Type type, Func<string, int, TileBase, bool> include) {
         // Find out which tiles have to be included
         bool[,] matrix = new bool[editorSize.x, editorSize.y];

         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               foreach (MidExportLayer layer in midLayers) {
                  if (layer.tileMatrix[i, j] != null) {
                     if (include(layer.layer, layer.subLayer, layer.tileMatrix[i, j].tileBase)) {
                        matrix[i, j] = true;
                        break;
                     }
                  }
               }
            }
         }

         bool[,] used = new bool[editorSize.x, editorSize.y];

         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               if (!matrix[i, j] || used[i, j]) {
                  continue;
               }

               Vector2Int size = new Vector2Int(1, 1);
               used[i, j] = true;

               // Get the height of effected clump
               while (j + size.y < editorSize.y && matrix[i, j + size.y] && !used[i, j + size.y]) {
                  used[i, j + size.y] = true;
                  size.y++;
               }

               // Extend the effected clump horizontally
               while (i + size.x < editorSize.x) {
                  bool hasColumn = true;
                  // Check if next column is full
                  for (int h = 0; h < size.y; h++) {
                     if (!matrix[i + size.x, j + h] || used[i + size.x, j + h]) {
                        hasColumn = false;
                        break;
                     }
                  }

                  if (!hasColumn) {
                     break;
                  }

                  // If next column is full, extend and mark as used
                  for (int h = 0; h < size.y; h++) {
                     used[i + size.x, j + h] = true;
                  }
                  size.x++;
               }

               yield return new SpecialTileChunk {
                  type = type,
                  position = new Vector2(i, j) + (Vector2) size * 0.5f - (Vector2) editorSize * 0.5f,
                  size = size
               };
            }
         }
      }

      private static TileWithCollision[,] getTilesWithCollisions (
         Layer layer,
         Func<TileBase, Vector2Int> tileToIndex,
         Dictionary<TileBase, TileCollisionType> tileToCollision,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         TileWithCollision[,] tiles = new TileWithCollision[editorSize.x, editorSize.y];

         for (int i = 0; i < layer.size.x; i++) {
            for (int j = 0; j < layer.size.y; j++) {
               Vector3Int pos = new Vector3Int(i + layer.origin.x, j + layer.origin.y, 0);
               TileBase tileBase = layer.getTile(pos);
               if (tileBase != null) {
                  Vector2Int index = tileToIndex(tileBase);

                  TileCollisionType col = TileCollisionType.Disabled;
                  if (tileToCollision.TryGetValue(tileBase, out TileCollisionType foundCol))
                     col = foundCol;

                  tiles[pos.x - editorOrigin.x, pos.y - editorOrigin.y] = new TileWithCollision {
                     tile = new ExportedTile001 {
                        i = index.x,
                        j = index.y,
                        x = pos.x,
                        y = pos.y
                     },
                     tileBase = tileBase,
                     collisionType = col
                  };
               }
            }
         }

         return tiles;
      }

      public static float getZ (int layer, int sublayer) {
         return
            AssetSerializationMaps.layerZFirst +
            layer * AssetSerializationMaps.layerZMultip +
            sublayer * AssetSerializationMaps.sublayerZMultip;
      }

      private static DeserializedProject deserialize002 (string data, bool forEditor) {
         Project002 dt = JsonUtility.FromJson<Project002>(data);

         if (dt.biome == Biome.Type.None) {
            dt.biome = Biome.Type.Forest;
         }

         List<DeserializedProject.DeserializedPrefab> prefabs = new List<DeserializedProject.DeserializedPrefab>();
         List<DeserializedProject.DeserializedTile> tiles = new List<DeserializedProject.DeserializedTile>();

         Func<Vector2Int, TileBase> indexToTile = (index) => { return AssetSerializationMaps.getTile(index, dt.biome); };
         Func<int, GameObject> indexToPrefab = (index) => { return AssetSerializationMaps.getPrefab(index, dt.biome, forEditor); };

         foreach (var pref in dt.prefabs) {
            GameObject original = indexToPrefab(pref.i);
            if (original != AssetSerializationMaps.deletedPrefabMarker) {
               prefabs.Add(new DeserializedProject.DeserializedPrefab {
                  position = new Vector3(pref.x, pref.y, 0),
                  prefab = original,
                  dataFields = pref.data
               });
            }
         }

         foreach (var layer in dt.layers) {
            if (layer.sublayers.Length == 0) {
               foreach (var tile in layer.tiles) {
                  tiles.Add(new DeserializedProject.DeserializedTile {
                     layer = layer.id,
                     sublayer = null,
                     position = new Vector3Int(tile.x, tile.y, 0),
                     tile = indexToTile(new Vector2Int(tile.i, tile.j))
                  });
               }
            } else {
               for (int i = 0; i < layer.sublayers.Length; i++) {
                  foreach (var tile in layer.sublayers[i].tiles) {
                     tiles.Add(new DeserializedProject.DeserializedTile {
                        layer = layer.id,
                        sublayer = i,
                        position = new Vector3Int(tile.x, tile.y, 0),
                        tile = indexToTile(new Vector2Int(tile.i, tile.j))
                     });
                  }
               }
            }
         }

         return new DeserializedProject {
            prefabs = prefabs.ToArray(),
            tiles = tiles.ToArray(),
            biome = dt.biome,
            size = dt.size,
            editorType = dt.editorType
         }.fixPrefabFields();
      }

      private static int extractVersion (string data) {
         Regex rx = new Regex("\"[0-9].[0-9].[0-9]\"");
         Match match = rx.Match(data);

         if (!match.Success)
            throw new Exception("Could not identify the file version");

         string versionString = match.Groups[0].Value.Replace('\"', ' ');
         string[] versionNumbers = versionString.Split('.');

         return int.Parse(versionNumbers[0]) * 10000 + int.Parse(versionNumbers[1]) * 100 + int.Parse(versionNumbers[2]);
      }

      private static Tile001[] serializeTiles (Layer layer, Func<TileBase, Vector2Int> tileToIndex) {
         List<Tile001> tiles = new List<Tile001>();

         for (int i = 0; i < layer.size.x; i++) {
            for (int j = 0; j < layer.size.y; j++) {
               Vector3Int pos = new Vector3Int(i + layer.origin.x, j + layer.origin.y, 0);
               TileBase tile = layer.getTile(pos);
               if (tile != null) {
                  Vector2Int index = tileToIndex(tile);
                  tiles.Add(new Tile001 {
                     i = index.x,
                     j = index.y,
                     x = pos.x,
                     y = pos.y
                  });
               }
            }
         }
         return tiles.ToArray();
      }

      /// <summary>
      /// Used in the middle of map export process as a temporary layer
      /// </summary>
      private class MidExportLayer
      {
         public float z { get; set; }
         public string layer { get; set; }
         public int subLayer { get; set; }
         public TileWithCollision[,] tileMatrix { get; set; }
         public LayerType type { get; set; }
      }

      /// <summary>
      /// Used in the middle of map export process
      /// </summary>
      private class TileWithCollision
      {
         public ExportedTile001 tile { get; set; }
         public TileBase tileBase { get; set; }
         public TileCollisionType collisionType { get; set; }
      }
   }

   [Serializable]
   public class ExportedProject001
   {
      public string version;
      public Biome.Type biome;
      public EditorType editorType;
      public Vector2Int size;
      public ExportedPrefab001[] prefabs;
      public ExportedLayer001[] layers;
      public SpecialTileChunk[] specialTileChunks;

      public ExportedProject001 fixPrefabFields () {
         foreach (ExportedPrefab001 prefab in prefabs) {
            foreach (DataField field in prefab.d) {
               field.fixField(false);
            }
         }
         return this;
      }
   }

   [Serializable]
   public class ExportedPrefab001
   {
      public int i; // Prefab index
      public float x; // Prefab position x
      public float y; // Prefab position y
      public float iz; // Inherited Z position
      public DataField[] d; // The custom data of the prefab, defined as key-value pairs
   }

   [Serializable]
   public class ExportedLayer001
   {
      public string name;
      public int sublayer = 0;
      public float z;
      public ExportedTile001[] tiles;
      public LayerType type;
   }

   [Serializable]
   public class ExportedTile001
   {
      public int i; // Tile index x
      public int j; // Tile index y
      public int x; // Tile position x
      public int y; // Tile position y
      public int c; // Whether the tile should collide
   }

   [Serializable]
   public class SpecialTileChunk
   {
      public Type type;
      public Vector2 position;
      public Vector2 size;
      public PointPath[] paths;

      public enum Type
      {
         Stair = 1,
         Vine = 2,
         Waterfall = 3,
         Current = 4,
         Rug1 = 5,
         Rug2 = 6,
         Rug3 = 7,
         Rug4 = 8
      }
   }

   [Serializable]
   public class PointPath
   {
      public Vector2[] points;
   }

   public class DeserializedProject
   {
      public DeserializedPrefab[] prefabs;
      public DeserializedTile[] tiles;
      public Biome.Type biome;
      public EditorType editorType;
      public Vector2Int size;

      public DeserializedProject fixPrefabFields () {
         foreach (DeserializedPrefab prefab in prefabs) {
            foreach (DataField field in prefab.dataFields) {
               field.fixField(true);
            }
         }
         return this;
      }

      public class DeserializedPrefab
      {
         public GameObject prefab;
         public Vector3 position;
         public DataField[] dataFields;
      }

      public class DeserializedTile
      {
         public TileBase tile;
         public string layer;
         public int? sublayer;
         public Vector3Int position;
      }
   }

   [Serializable]
   public class Project002
   {
      public string version;
      public Biome.Type biome;
      public EditorType editorType;
      public Layer002[] layers;
      public Prefab001[] prefabs;
      public Vector2Int size;
   }

   [Serializable]
   public class Layer002
   {
      public string id;
      public Tile001[] tiles;
      public SubLayer001[] sublayers;
   }

   [Serializable]
   public class SubLayer001
   {
      public int index;
      public Tile001[] tiles;
   }

   [Serializable]
   public class Tile001
   {
      public int i; // Tile index x
      public int j; // Tile index y
      public int x; // Tile position x
      public int y; // Tile position y
   }

   [Serializable]
   public class Prefab001
   {
      public int i; // Prefab index
      public float x; // Prefab position x
      public float y; // Prefab position y
      public DataField[] data; // The custom data of the prefab, defined as key-value pairs
   }

   [Serializable]
   public class DataField
   {
      public static CultureInfo US_CULTURE => CultureInfo.CreateSpecificCulture("en-US");

      // For prefabs, the serializable data is saved as key-value pairs
      // Below are they keys for the defined key-value pairs
      public const string WARP_TARGET_MAP_KEY = "target map";
      public const string WARP_TARGET_SPAWN_KEY = "target spawn";
      public const string WARP_WIDTH_KEY = "width";
      public const string WARP_HEIGHT_KEY = "height";
      public const string WARP_ARRIVE_FACING_KEY = "arrive facing";

      public const string SPAWN_NAME_KEY = "name";
      public const string SPAWN_WIDTH_KEY = "width";
      public const string SPAWN_HEIGHT_KEY = "height";

      public const string CRITTER_RUN_DIRECTION_KEY = "run direction";

      public const string LAND_ENEMY_DATA_KEY = "enemy data";

      public const string ORE_SPOT_DATA_KEY = "ore id";
      public const string ORE_TYPE_DATA_KEY = "ore type";

      public const string SEA_ENEMY_DATA_KEY = "enemy data";

      public const string HOUSE_TARGET_MAP_KEY = "target map";
      public const string HOUSE_TARGET_SPAWN_KEY = "target spawn";

      public const string TUTORIAL_ITEM_STEP_ID_KEY = "step id";

      public const string MAP_SIGN_TYPE_KEY = "map sign type";
      public const string MAP_ICON_KEY = "map sign icon";

      public const string SECRETS_TYPE_ID = "secret type";
      public const string SECRETS_START_SPRITE = "secret start sprite";
      public const string SECRETS_INTERACT_SPRITE = "secret interact sprite";

      public const string NPC_DATA_KEY = "npc data";
      public const string NPC_SHOP_NAME_KEY = "shop name";
      public const string NPC_PANEL_TYPE_KEY = "panel type";

      public const string SPIDER_WEB_HEIGHT_KEY = "height";

      public const string TREASURE_SPOT_SPAWN_CHANCE_KEY = "spawn chance";

      public const string GENERIC_ACTION_TRIGGER_INTERACTION_TYPE = "interaction type";
      public const string GENERIC_ACTION_TRIGGER_ACTION_NAME = "action name";
      public const string GENERIC_ACTION_TRIGGER_WIDTH_KEY = "width";
      public const string GENERIC_ACTION_TRIGGER_HEIGHT_KEY = "height";

      public const string BOOK_ID_KEY = "book id";

      public const string LEDGE_WIDTH_KEY = "width";
      public const string LEDGE_HEIGHT_KEY = "height";

      public const string DISCOVERY_SPAWN_CHANCE = "spawn chance";
      public const string POSSIBLE_DISCOVERY = "possible discovery";

      public const string SHIP_DATA_KEY = "ship data";

      public string k; // Key
      public string v; // Value

      public float floatValue
      {
         get { return float.Parse(v, US_CULTURE); }
      }

      public bool tryGetFloatValue (out float value) {
         if (float.TryParse(v, NumberStyles.Float, US_CULTURE, out float val)) {
            value = val;
            return true;
         }
         value = 0;
         return false;
      }

      public int intValue
      {
         get { return int.Parse(v, US_CULTURE); }
      }

      public bool tryGetIntValue (out int value) {
         if (int.TryParse(v, NumberStyles.Integer, US_CULTURE, out int val)) {
            value = val;
            return true;
         }
         value = 0;
         return false;
      }

      public bool tryGetDirectionValue (out Direction value) {
         switch (v.Trim(' ')) {
            case "North":
               value = Direction.North;
               return true;
            case "NorthEast":
               value = Direction.NorthEast;
               return true;
            case "East":
               value = Direction.East;
               return true;
            case "SouthEast":
               value = Direction.SouthEast;
               return true;
            case "South":
               value = Direction.South;
               return true;
            case "SouthWest":
               value = Direction.SouthWest;
               return true;
            case "West":
               value = Direction.West;
               return true;
            case "NorthWest":
               value = Direction.NorthWest;
               return true;
         }

         value = Direction.North;
         return false;
      }

      public bool tryGetInteractionTypeValue (out GenericActionTrigger.InteractionType value) {
         switch (v.Trim(' ')) {
            case "Enter":
               value = GenericActionTrigger.InteractionType.Enter;
               return true;
            case "Exit":
               value = GenericActionTrigger.InteractionType.Exit;
               return true;
            case "Stay":
               value = GenericActionTrigger.InteractionType.Stay;
               return true;
         }
         value = GenericActionTrigger.InteractionType.Enter;
         return false;
      }

      public void fixField (bool forEditor) {
         switch (k) {
            case NPC_DATA_KEY:
            case LAND_ENEMY_DATA_KEY:
               string[] values = v.Split(':');
               if (values.Length == 2) {
                  if (int.TryParse(values[0], out int nv)) {
                     v = nv.ToString();
                  }
               }
               break;
            case WARP_TARGET_MAP_KEY:
               if (forEditor && !tryGetIntValue(out int mapId)) {
                  Map map = Overlord.remoteMaps.maps.Values.FirstOrDefault(m => m.name.CompareTo(v) == 0);
                  if (map != null) {
                     v = map.id.ToString();
                  }
               }
               break;
         }
      }
   }
}
