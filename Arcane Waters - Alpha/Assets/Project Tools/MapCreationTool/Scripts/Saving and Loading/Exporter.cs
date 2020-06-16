using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;
using System.Linq;

namespace MapCreationTool.Serialization
{
   public class Exporter
   {
      public Dictionary<string, Layer> layerDictionary { get; private set; }
      public List<PlacedPrefab> placedPrefabs { get; private set; }
      public Biome.Type biome { get; private set; }
      public EditorType editorType { get; private set; }
      public Vector2Int editorOrigin { get; private set; }
      public Vector2Int editorSize { get; private set; }

      private BoardCell[,] cellMatrix;
      private List<(TileBase, Vector2Int)> additionalTileColliders;


      public Exporter (Dictionary<string, Layer> layers, List<PlacedPrefab> prefabs, Biome.Type biome, EditorType eType, Vector2Int eOrigin, Vector2Int eSize) {
         layerDictionary = layers;
         placedPrefabs = prefabs;
         this.biome = biome;
         editorType = eType;
         editorOrigin = eOrigin;
         editorSize = eSize;
      }

      public ExportedProject001 toExportedProject001 (EditorConfig config) {
         if (cellMatrix == null) {
            throw new Exception("Cannot form export data - source data is not transformed.");
         }

         Func<GameObject, int> prefabToIndex = (go) => { return AssetSerializationMaps.getIndex(go, biome); };
         Func<TileBase, Vector2Int> tileToIndex = (tile) => { return AssetSerializationMaps.getIndex(tile, biome); };

         // Make prefab serialization object
         ExportedPrefab001[] prefabsSerialized
             = placedPrefabs.Select(p =>
                 new ExportedPrefab001 {
                    i = prefabToIndex(p.original),
                    x = p.placedInstance.transform.position.x,
                    y = p.placedInstance.transform.position.y,
                    d = p.data.Select(data => new DataField { k = data.Key, v = data.Value }).ToArray(),
                    iz = (p.placedInstance.GetComponent<ZSnap>()?.inheritedOffsetZ ?? 0) * 0.16f
                 }
             ).ToArray();

         // Gather tile columns to layers and turn them into exported layers
         ExportedLayer001[] exportedLayers = getAllTiles()
            .GroupBy(t => (t.layer, t.sublayer))
            .Select(g => new ExportedLayer001 {
               name = g.Key.layer,
               sublayer = g.Key.sublayer,
               type = config.getLayerConfig(editorType, g.Key.layer).layerType,
               z = config.getLayerConfig(editorType, g.Key.layer).zOffset + getZ(config.getIndex(g.Key.layer, editorType), g.Key.sublayer),
               tiles = g.Select(t => (tileToIndex(t.tileBase), t.position, t.shouldHaveCollider)).Select(t => new ExportedTile001 {
                  i = t.Item1.x,
                  j = t.Item1.y,
                  x = t.position.x,
                  y = t.position.y,
                  c = t.shouldHaveCollider ? 1 : 0
               }).ToArray()
            })
            .ToArray();

         // Translate tiles for setting additional tile colliders
         ExportedLayer001 exportedAddtionalColliders = new ExportedLayer001 {
            name = "SPECIAL_TILE_COLLIDERS",
            tiles = additionalTileColliders
               .Select(t => (tileToIndex(t.Item1), t.Item2 + editorOrigin))
               .Select(t => new ExportedTile001 { i = t.Item1.x, j = t.Item1.y, x = t.Item2.x, y = t.Item2.y, c = 1 })
               .ToArray()
         };

         return new ExportedProject001 {
            version = "0.0.1",
            biome = biome,
            layers = exportedLayers,
            specialTileChunks = formSpecialTileChunks(),
            editorType = editorType,
            prefabs = prefabsSerialized,
            additionalTileColliders = exportedAddtionalColliders
         };
      }

      public void transformData (Dictionary<TileBase, TileCollisionType> collisionDictionary) {
         // Gather all tiles in project, placing them in columns
         cellMatrix = gatherCellsToColumns(collisionDictionary);

         // Set special traits for tile columns
         setColumnTraits(cellMatrix);

         // Handle collisions
         setCollisions(cellMatrix);
      }

      private void setCollisions (BoardCell[,] cellMatrix) {
         additionalTileColliders = new List<(TileBase, Vector2Int)>();
         HashSet<Vector2Int> forceIgnoreCollisions = getCancelledTileCollisions(placedPrefabs);

         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               // If this cell is being forced to not have colliders, do nothing
               if (forceIgnoreCollisions.Contains(new Vector2Int(i - editorSize.x / 2, j - editorSize.x / 2))) {
                  continue;
               }

               // If this is a ceiling tile and it is not at the edge of a map, check for doorframes
               if (cellMatrix[i, j].hasCeiling && i > 0 && i < editorSize.x - 1 && j > 0 && j < editorSize.y - 1) {
                  // Check top and bot
                  if (cellMatrix[i, j - 1].hasDoorframe && cellMatrix[i, j + 1].hasDoorframe) {
                     TileInLayer bot = cellMatrix[i, j - 1].getTileFromTop(Layer.DOORFRAME_KEY);
                     TileInLayer top = cellMatrix[i, j + 1].getTileFromTop(Layer.DOORFRAME_KEY);
                     if (bot.collisionType == TileCollisionType.Enabled && top.collisionType == TileCollisionType.Enabled) {
                        // If we have a ceiling with doorframes with colliders on top and bottom, we want to extend that collider to the ceiling
                        additionalTileColliders.Add((bot.tileBase, new Vector2Int(i, j)));
                     }
                     continue;
                  }

                  // Check left and right
                  if (cellMatrix[i + 1, j].hasDoorframe && cellMatrix[i - 1, j].hasDoorframe &&
                     cellMatrix[i + 1, j].getTileFromTop(Layer.DOORFRAME_KEY).collisionType == TileCollisionType.CancelDisabled &&
                     cellMatrix[i - 1, j].getTileFromTop(Layer.DOORFRAME_KEY).collisionType == TileCollisionType.CancelDisabled) {

                     continue;
                  }
               }

               BoardCell column = cellMatrix[i, j];
               for (int z = column.tiles.Length - 1; z >= 0; z--) {
                  // If docks have no water under them, don't place colliders
                  if (Layer.isDock(column.tiles[z].layer) && !column.hasWater) {
                     if (column.tiles[z].collisionType == TileCollisionType.CancelEnabled || column.tiles[z].collisionType == TileCollisionType.CancelDisabled) {
                        break;
                     } else {
                        continue;
                     }
                  }

                  if (column.tiles[z].collisionType == TileCollisionType.Enabled) {
                     column.tiles[z].shouldHaveCollider = true;
                  } else if (column.tiles[z].collisionType == TileCollisionType.CancelEnabled) {
                     column.tiles[z].shouldHaveCollider = true;
                     break;
                  } else if (column.tiles[z].collisionType == TileCollisionType.CancelDisabled) {
                     break;
                  }
               }
            }
         }
      }

      private HashSet<Vector2Int> getCancelledTileCollisions (List<PlacedPrefab> prefabs) {
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

      private void setColumnTraits (BoardCell[,] columns) {
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               for (int z = 0; z < columns[i, j].tiles.Length; z++) {

                  if (Layer.isWater(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasWater = true;
                  }

                  if (Layer.isDock(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasDock = true;
                  }

                  if (Layer.isCeiling(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasCeiling = true;
                  }

                  if (Layer.isDoorframe(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasDoorframe = true;
                  }

                  if (Layer.isStair(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasStair = true;
                  }

                  if (Layer.isVine(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasVine = true;
                  }

                  if (Layer.isWater(columns[i, j].tiles[z].layer) && columns[i, j].tiles[z].sublayer == 4) {
                     columns[i, j].hasWater4 = true;
                  }

                  if (Layer.isWater(columns[i, j].tiles[z].layer) && columns[i, j].tiles[z].sublayer == 5) {
                     columns[i, j].hasWater5 = true;

                     foreach (Direction dir in Enum.GetValues(typeof(Direction))) {
                        if (columns[i, j].tiles[z].tileBase.name.EndsWith("_" + dir.ToString(), StringComparison.OrdinalIgnoreCase)) {
                           columns[i, j].currentDirection = dir;
                        }
                     }
                  }

                  if (Layer.isRug(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasRug = true;
                  }
               }
            }
         }
      }

      private BoardCell[,] gatherCellsToColumns (Dictionary<TileBase, TileCollisionType> collisionDictionary) {
         cellMatrix = new BoardCell[editorSize.x, editorSize.y];

         // Gather all tiles in project, placing them in columns
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               List<TileInLayer> tiles = new List<TileInLayer>();
               Vector3Int pos = new Vector3Int(i + editorOrigin.x, j + editorOrigin.y, 0);

               foreach (var layerkv in layerDictionary) {
                  if (layerkv.Value.hasTilemap) {
                     TileBase tileBase = layerkv.Value.getTile(pos);
                     if (tileBase != null) {
                        tiles.Add(new TileInLayer {
                           tileBase = tileBase,
                           position = (pos.x, pos.y),
                           layer = layerkv.Key,
                           sublayer = 0,
                           collisionType = collisionDictionary.ContainsKey(tileBase) ? collisionDictionary[tileBase] : TileCollisionType.Disabled
                        });
                     }
                  } else {
                     for (int z = 0; z < layerkv.Value.subLayers.Length; z++) {
                        TileBase tileBase = layerkv.Value.subLayers[z].getTile(pos);
                        if (tileBase != null) {
                           tiles.Add(new TileInLayer {
                              tileBase = tileBase,
                              position = (pos.x, pos.y),
                              layer = layerkv.Key,
                              sublayer = z,
                              collisionType = collisionDictionary.ContainsKey(tileBase) ? collisionDictionary[tileBase] : TileCollisionType.Disabled
                           });
                        }
                     }
                  }
               }

               cellMatrix[i, j] = new BoardCell { tiles = tiles.ToArray() };
            }
         }

         return cellMatrix;
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

      private SpecialTileChunk[] formSpecialTileChunks () {
         IEnumerable<SpecialTileChunk> stairs = formSquareChunks(SpecialTileChunk.Type.Stair, (c) => c.hasStair);
         IEnumerable<SpecialTileChunk> waterfalls = formSquareChunks(SpecialTileChunk.Type.Waterfall, (c) => c.hasWater4 && editorType == EditorType.Area);
         IEnumerable<SpecialTileChunk> vines = formSquareChunks(SpecialTileChunk.Type.Vine, (c) => c.hasVine);

         IEnumerable<SpecialTileChunk> currents = Enumerable.Empty<SpecialTileChunk>();

         if (editorType == EditorType.Area) {
            currents = currents.Union(formWaterCurrentChunk((cell) => cell.hasWater4 || cell.hasWater5, Direction.South));
         } else if (editorType == EditorType.Sea) {
            foreach (Direction dir in Enum.GetValues(typeof(Direction))) {
               currents = currents.Union(formWaterCurrentChunk((cell) => cell.hasWater5 && cell.currentDirection == dir, dir));
            }
         }

         Dictionary<TileBase, int> tileToRug = Palette.instance.paletteData.tileToRugType;

         IEnumerable<SpecialTileChunk> rugs = Enumerable.Range(1, 4).SelectMany(type => {
            return formSquareChunks(
               (SpecialTileChunk.Type) (type + 4),
               (c) => c.hasRug && tileToRug.ContainsKey(c.getTileFromTop(Layer.RUG_KEY).tileBase) && tileToRug[c.getTileFromTop(Layer.RUG_KEY).tileBase] == type);
         });

         return stairs.Union(waterfalls).Union(vines).Union(currents).Union(rugs).ToArray();
      }

      private IEnumerable<SpecialTileChunk> formWaterCurrentChunk (Func<BoardCell, bool> columnSelector, Direction dir) {
         bool[,] matrix = new bool[editorSize.x, editorSize.y];
         List<(int, int)> currentIndexes = new List<(int, int)>();

         // Find all current and waterfall tiles and set them in the matrix
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               if (columnSelector(cellMatrix[i, j])) {
                  currentIndexes.Add((i, j));
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

                  if (cellMatrix[i, j].hasWater) {
                     // Don't place effectors on waterfalls themselves, since it will be handled by waterfall ledges
                     if (editorType == EditorType.Area && cellMatrix[i, j].hasWater4) {
                        continue;
                     }

                     matrix[i, j] = true;
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
            paths = paths,
            effectorDirection = dir
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

      private IEnumerable<SpecialTileChunk> formSquareChunks (SpecialTileChunk.Type type, Func<BoardCell, bool> include) {
         // Find out which tiles have to be included
         bool[,] matrix = new bool[editorSize.x, editorSize.y];

         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               if (include(cellMatrix[i, j])) {
                  matrix[i, j] = true;
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

      private IEnumerable<TileInLayer> getAllTiles () {
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               for (int z = 0; z < cellMatrix[i, j].tiles.Length; z++) {
                  yield return cellMatrix[i, j].tiles[z];
               }
            }
         }
      }

      public static float getZ (int layer, int sublayer) {
         return
            AssetSerializationMaps.layerZFirst +
            layer * AssetSerializationMaps.layerZMultip +
            sublayer * AssetSerializationMaps.sublayerZMultip;
      }

      private struct BoardCell
      {
         public TileInLayer[] tiles { get; set; }

         public bool hasWater { get; set; }
         public bool hasDock { get; set; }
         public bool hasCeiling { get; set; }
         public bool hasDoorframe { get; set; }
         public bool hasStair { get; set; }
         public bool hasVine { get; set; }
         public bool hasRug { get; set; }
         public bool hasWater4 { get; set; }
         public bool hasWater5 { get; set; }
         public Direction currentDirection { get; set; }

         public TileInLayer getTileFromTop (string layer) {
            for (int i = tiles.Length - 1; i >= 0; i--) {
               if (tiles[i].layer.Equals(layer)) {
                  return tiles[i];
               }
            }

            throw new Exception("No such tile.");
         }
      }

      private struct TileInLayer
      {
         public TileBase tileBase { get; set; }
         public (int x, int y) position { get; set; }
         public string layer { get; set; }
         public int sublayer { get; set; }
         public bool shouldHaveCollider { get; set; }
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
      public ExportedLayer001 additionalTileColliders;

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
      public Direction effectorDirection = Direction.South;

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
}
