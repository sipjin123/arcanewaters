using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;
using System.Linq;

namespace MapCreationTool.Serialization
{
   public class Exporter
   {
      // Tiles that should not block a cell from being considered being a 'water' cell
      public static HashSet<string> nonWaterBlockingTiles = new HashSet<string> {
         "desert_tiles_161", "desert_tiles_258", "desert_tiles_261",
         "forest_tiles_155", "forest_tiles_252", "forest_tiles_255",
         "lava_tiles_151", "lava_tiles_248", "lava_tiles_251",
         "pine_tiles_153", "pine_tiles_250", "pine_tiles_253",
         "mushroom_tiles_151", "mushroom_tiles_252", "mushroom_tiles_255",
         "snow_tiles_153", "snow_tiles_250", "snow_tiles_253"
      };

      // The names of the full water tiles
      public static HashSet<string> fullWaterTiles = new HashSet<string>() {
         "water_15", "water_16", "water_17",
         "water_129", "water_132", "water_133", "water_134", "water_135", "water_136",
         "water_144", "water_146", "water_147", "water_148", "water_149",
         "water_156", "water_157", "water_158", "water_159", "water_160", "water_161",
         "water_168", "water_171", "water_174", "water_180", "water_186", "water_192",
         "water_195", "water_198",
      };

      // The names of the waterfall tiles
      public static HashSet<string> waterFallTiles = new HashSet<string>() {
         "water_60", "water_61", "water_62", "water_72", "water_73", "water_74",
         "water_84", "water_85", "water_86", "water_96", "water_108", "water_120"
      };

      // The names of the partial water tiles
      public static HashSet<string> partialWaterTiles = new HashSet<string>() {
         "water_3","water_12", "water_18", "water_27", "water_0", "water_6", "water_24", "water_30", "water_36", "water_48"
      };

      // The names of wall tiles that should be forces to be rendered on top of player
      public static HashSet<string> wallForceTopTiles = new HashSet<string>() {
         "desert_tiles_51", "desert_tiles_55", "desert_tiles_68", "desert_tiles_72", "desert_tiles_85", "desert_tiles_89",
         "forest_tiles_51", "forest_tiles_55", "forest_tiles_68", "forest_tiles_72", "forest_tiles_85", "forest_tiles_89",
         "lava_tiles_51"," lava_tiles_55", "lava_tiles_68", "lava_tiles_72", "lava_tiles_85", "lava_tiles_89",
         "mushroom_tiles_51", "mushroom_tiles_55", "mushroom_tiles_68", "mushroom_tiles_72", "mushroom_tiles_85", "mushroom_tiles_89",
         "pine_tiles_51", "pine_tiles_55", "pine_tiles_68", "pine_tiles_72", "pine_tiles_85", "pine_tiles_89",
         "snow_tiles_51", "snow_tiles_55", "snow_tiles_68", "snow_tiles_72", "snow_tiles_85", "snow_tiles_89"
      };

      // The names of bridge tiles
      public static HashSet<string> bridgeTiles = new HashSet<string>() {
         "desert_tiles_160", "desert_tiles_161", "desert_tiles_162", "desert_tiles_181", "desert_tiles_182", "desert_tiles_183", "desert_tiles_241", "desert_tiles_242", "desert_tiles_259", "desert_tiles_260", "desert_tiles_273", "desert_tiles_274",
         "forest_tiles_154", "forest_tiles_155", "forest_tiles_156", "forest_tiles_175", "forest_tiles_176", "forest_tiles_177", "forest_tiles_235", "forest_tiles_236", "forest_tiles_253", "forest_tiles_254", "forest_tiles_267", "forest_tiles_268",
         "lava_tiles_150", "lava_tiles_151", "lava_tiles_152", "lava_tiles_171", "lava_tiles_172", "lava_tiles_173", "lava_tiles_231", "lava_tiles_232", "lava_tiles_249", "lava_tiles_250",  "lava_tiles_263", "lava_tiles_264",
         "mushroom_tiles_150", "mushroom_tiles_151", "mushroom_tiles_152", "mushroom_tiles_171", "mushroom_tiles_172", "mushroom_tiles_173", "mushroom_tiles_231", "mushroom_tiles_232", "mushroom_tiles_253", "mushroom_tiles_254", "mushroom_tiles_271", "mushroom_tiles_272",
         "pine_tiles_152", "pine_tiles_153", "pine_tiles_154", "pine_tiles_173", "pine_tiles_174", "pine_tiles_175", "pine_tiles_233", "pine_tiles_234", "pine_tiles_251", "pine_tiles_252", "pine_tiles_265", "pine_tiles_266",
         "snow_tiles_152", "snow_tiles_153", "snow_tiles_154", "snow_tiles_173", "snow_tiles_174", "snow_tiles_175", "snow_tiles_233", "snow_tiles_234", "snow_tiles_251", "snow_tiles_252", "snow_tiles_265", "snow_tiles_266"
      };

      // The tile numbers for grass
      public static HashSet<int> grassTiles = new HashSet<int>() { 0, 1 };

      // The tile numbers for stone
      public static HashSet<int> stoneTiles = new HashSet<int>() { 11, 12, 13, 28, 29, 30, 45, 46, 47, 69, 143, 144, 145, 211, 212, 228, 229 };

      // The tile numbers for wood
      public static HashSet<int> woodTiles = new HashSet<int>() { 62, 63, 64, 79, 80, 81, 96, 97, 98, 113, 114, 115, 160, 161, 162, 177, 178, 179, 194, 195, 196,
      183, 184, 185, 200, 201, 202 };

      public Dictionary<string, Layer> layerDictionary { get; private set; }
      public List<PlacedPrefab> placedPrefabs { get; private set; }
      public Biome.Type biome { get; private set; }
      public EditorType editorType { get; private set; }
      public Vector2Int editorOrigin { get; private set; }
      public Vector2Int editorSize { get; private set; }

      private BoardCell[,] cellMatrix;
      private List<(TileBase, Vector2Int)> additionalTileColliders;
      private List<SpecialTileChunk> vineChunks;


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
                    d = p.data.Select(data => new DataField { k = data.Key, v = data.Value }).Union(p.getAdditionalDataForExport()).ToArray(),
                    iz = (p.placedInstance.GetComponent<ZSnap>()?.inheritedOffsetZ ?? 0) * 0.16f
                 }
             ).ToArray();

         // Switch layer for the tiles that are set to absolute top
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               for (int k = 0; k < cellMatrix[i, j].tiles.Length; k++) {
                  if (cellMatrix[i, j].tiles[k].forceAbsoluteTop) {
                     cellMatrix[i, j].tiles[k].layer = Layer.ABSOLUTE_TOP_KEY;
                     break;
                  }
               }
            }
         }

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

         // Map cell types
         List<CellTypesContainer.MapCellType> mapCellTypes = formMapCellTypes();

         return new ExportedProject001 {
            version = "0.0.1",
            biome = biome,
            layers = exportedLayers,
            specialTileChunks = formSpecialTileChunks().Union(vineChunks).ToArray(),
            editorType = editorType,
            size = editorSize,
            prefabs = prefabsSerialized,
            additionalTileColliders = exportedAddtionalColliders,
            mapCellTypes = mapCellTypes.ToArray()
         };
      }

      public void transformData (Dictionary<TileBase, TileCollisionType> collisionDictionary) {
         // Gather all tiles in project, placing them in columns
         cellMatrix = gatherCellsToColumns(collisionDictionary);

         // Set special traits for tile columns
         setColumnTraits(cellMatrix);

         // Handle collisions
         setCollisions(cellMatrix);

         // Handle vines differently
         vineChunks = handleVines().ToList();

         // Force some wall tiles to be rendered on top of player
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               // Dont force it if they have a doorframe on top though
               if (cellMatrix[i, j].hasDoorframe) continue;

               for (int k = 0; k < cellMatrix[i, j].tiles.Length; k++) {
                  if (Layer.isWall(cellMatrix[i, j].tiles[k].layer) && wallForceTopTiles.Contains(cellMatrix[i, j].tiles[k].tileBase.name)) {
                     cellMatrix[i, j].tiles[k].forceAbsoluteTop = true;
                     break;
                  }
               }
            }
         }
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
                  // Check top and bot (vertical path doorframe)
                  if (tryGetBotTopDoorframes(i, j, 4, cellMatrix, out TileInLayer bot, out TileInLayer top)) {
                     if (bot.collisionType == TileCollisionType.Enabled && top.collisionType == TileCollisionType.Enabled) {
                        // If we have a ceiling with doorframes with colliders on top and bottom, we want to extend that collider to the ceiling
                        for (int y = bot.position.y + 1 - editorOrigin.y; y < top.position.y - editorOrigin.y; y++) {
                           if (!additionalTileColliders.Any(pair => pair.Item2 == new Vector2Int(i, y))) {
                              additionalTileColliders.Add((bot.tileBase, new Vector2Int(i, y)));
                           }
                        }
                     }
                     continue;
                  }

                  // Check 1 above, left and right (horizontal path doorframe)
                  if (tryGetLeftRightDoorframes(i, j + 1, 4, cellMatrix, out TileInLayer left, out TileInLayer right)) {
                     if (left.collisionType == TileCollisionType.CancelEnabled && right.collisionType == TileCollisionType.CancelEnabled) {
                        continue;
                     }
                  }
               }

               // If this is a wall tile, check if it has a horizontal doorframe on top
               if (cellMatrix[i, j].hasWall && j < editorSize.y - 1) {
                  if (cellMatrix[i, j + 1].hasDoorframe && cellMatrix[i, j + 1].getTileFromTop(Layer.DOORFRAME_KEY).collisionType == TileCollisionType.CancelEnabled) {
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

      // Given a position, searches for doorframe tiles up and down from the position
      private bool tryGetBotTopDoorframes (int x, int y, int maxDist, BoardCell[,] matrix, out TileInLayer bot, out TileInLayer top) {
         bot = top = default;

         // Start of marking tiles as not found
         int topIndex = -1;
         int botIndex = -1;

         // iterate over possible distances
         for (int l = 1; l < maxDist; l++) {
            // Check top tile
            if (topIndex == -1 && y + l < editorSize.y) {
               // If we have a doorframe tile or reached the edge of the map, it is a valid doorframe
               if (matrix[x, y + l].hasDoorframe) {
                  topIndex = y + l;
               } else if (!matrix[x, y + l].hasCeiling) {
                  // If we are didnt find a doorframe and we are no longer on ceiling, stop searching
                  return false;
               }
            }

            // Check bot tile
            if (botIndex == -1 && y - l >= 0) {
               // If we have a doorframe tile or reached the edge of the map, it is a valid doorframe
               if (matrix[x, y - l].hasDoorframe) {
                  botIndex = y - l;
               } else if (!matrix[x, y - l].hasCeiling) {
                  // If we are didnt find a doorframe and we are no longer on ceiling, stop searching
                  return false;
               }
            }
         }

         // Check that we found both tiles and didn't exceed maximum distance
         if (botIndex == -1 || topIndex == -1 || topIndex - botIndex > maxDist) {
            return false;
         }

         bot = matrix[x, botIndex].getTileFromTop(Layer.DOORFRAME_KEY);
         top = matrix[x, topIndex].getTileFromTop(Layer.DOORFRAME_KEY);
         return true;
      }

      // Given a position, searches for doorframe tiles left and right from the position
      private bool tryGetLeftRightDoorframes (int x, int y, int maxDist, BoardCell[,] matrix, out TileInLayer left, out TileInLayer right) {
         left = right = default;

         // Start of marking tiles as not found
         int leftIndex = -1;
         int rightIndex = -1;

         // iterate over possible distances
         for (int l = 1; l < maxDist; l++) {
            // Check top tile
            if (rightIndex == -1 && x + l < editorSize.x) {
               // If we have a doorframe tile or reached the edge of the map, it is a valid doorframe
               if (matrix[x + l, y].hasDoorframe) {
                  rightIndex = x + l;
               } else if (!matrix[x + l, y].hasCeiling) {
                  // If we are didnt find a doorframe and we are no longer on ceiling, stop searching
                  return false;
               }
            }

            // Check bot tile
            if (leftIndex == -1 && x - l >= 0) {
               // If we have a doorframe tile or reached the edge of the map, it is a valid doorframe
               if (matrix[x - l, y].hasDoorframe) {
                  leftIndex = x - l;
               } else if (!matrix[x - l, y].hasCeiling) {
                  // If we are didnt find a doorframe and we are no longer on ceiling, stop searching
                  return false;
               }
            }
         }

         // Check that we found both tiles and didn't exceed maximum distance
         if (leftIndex == -1 || rightIndex == -1 || rightIndex - leftIndex > maxDist) {
            return false;
         }

         left = matrix[leftIndex, y].getTileFromTop(Layer.DOORFRAME_KEY);
         right = matrix[rightIndex, y].getTileFromTop(Layer.DOORFRAME_KEY);
         return true;
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

                  if (Layer.isWall(columns[i, j].tiles[z].layer)) {
                     columns[i, j].hasWall = true;
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

      private IEnumerable<SpecialTileChunk> handleVines () {
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 1; j < editorSize.y - 1; j++) {
               // If there is a tile underneath blocking, don't place vines behaviour
               if (cellMatrix[i, j - 1].shouldHaveAnyColliders()) continue;

               Vector2Int size = new Vector2Int(1, 0);
               // Try to find a line of vines
               while (cellMatrix[i, j].hasVine && j < editorSize.y - 1) {
                  size.y++;
                  j++;
               }

               // If we didn't find any vines, continue
               if (size.y == 0) continue;

               // We want to make vine behaviour if there is either 0 or 1 tiles with colliders on top of vines
               bool top1 = j < editorSize.y && cellMatrix[i, j].shouldHaveAnyColliders();
               bool top2 = j < editorSize.y - 1 && cellMatrix[i, j + 1].shouldHaveAnyColliders();

               if (top1 && top2) continue;

               // If there is only 1 tile on top, include it in the behaviour
               if (top1) {
                  j++;
                  size.y++;
               }

               // Force remove colliders underneath the vines
               for (int k = j - size.y; k < j; k++) {
                  cellMatrix[i, k].removeAllColliders();
               }

               yield return new SpecialTileChunk {
                  type = SpecialTileChunk.Type.Vine,
                  position = new Vector2(i, j - size.y) + (Vector2) size * 0.5f - (Vector2) editorSize * 0.5f,
                  size = size
               };
            }
         }
      }

      private List<CellTypesContainer.MapCellType> formMapCellTypes () {
         List<CellTypesContainer.MapCellType> result = new List<CellTypesContainer.MapCellType>();

         // Iterate over all the cells
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               result.Add(cellMatrix[i, j].getCellType());
            }
         }

         return result;
      }

      private SpecialTileChunk[] formSpecialTileChunks () {
         IEnumerable<SpecialTileChunk> stairs = formSquareChunks(SpecialTileChunk.Type.Stair, (c) => c.hasStair);
         IEnumerable<SpecialTileChunk> waterfalls = formSquareChunks(SpecialTileChunk.Type.Waterfall, (c) => c.hasWater4 && editorType == EditorType.Area);

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

         return stairs.Union(waterfalls).Union(currents).Union(rugs).ToArray();
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
                     } else {
                        // We only have tile horizontally or vertically, remove such a scenario
                        UnityEngine.Object.Destroy(poly);
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
         public bool hasWall { get; set; }
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

         public ref TileInLayer getTileFromTopRef (string layer) {
            for (int i = tiles.Length - 1; i >= 0; i--) {
               if (tiles[i].layer.Equals(layer)) {
                  return ref tiles[i];
               }
            }

            throw new Exception("No such tile.");
         }

         public bool shouldHaveAnyColliders () {
            for (int i = 0; i < tiles.Length; i++) {
               if (tiles[i].shouldHaveCollider) {
                  return true;
               }
            }
            return false;
         }

         public void removeAllColliders () {
            for (int i = 0; i < tiles.Length; i++) {
               tiles[i].shouldHaveCollider = false;
            }
         }

         public CellTypesContainer.MapCellType getCellType () {
            // Iterate over all the tiles
            for (int k = tiles.Length - 1; k >= 0; k--) {
               string tileName = tiles[k].tileBase.name.ToLower();

               // If we're in an interior, the floor is always wood
               if (tileName.StartsWith("interior")) {
                  return CellTypesContainer.MapCellType.Wood;
               }

               // Check if this is a water tile
               if (fullWaterTiles.Contains(tileName) || waterFallTiles.Contains(tileName)) {
                  return CellTypesContainer.MapCellType.FullWater;
               }

               if (partialWaterTiles.Contains(tileName)) {
                  return CellTypesContainer.MapCellType.PartialWater;
               }

               // If we're in one of the outdoor areas, we need to check specific tile numbers
               if (tileName.Contains("_tiles")) {
                  string[] split = tileName.Split('_');
                  if (int.TryParse(split[split.Length - 1], out int num)) {
                     if (stoneTiles.Contains(num)) {
                        return CellTypesContainer.MapCellType.Stone;
                     }
                     if (woodTiles.Contains(num)) {
                        return CellTypesContainer.MapCellType.Wood;
                     }
                     if (grassTiles.Contains(num)) {
                        return CellTypesContainer.MapCellType.Grass;
                     }
                  }
               }

               // If this tile is not among non blocking tiles, stop searching
               if (!tileName.Contains("water") && !nonWaterBlockingTiles.Contains(tileName)) {
                  break;
               }
            }

            return CellTypesContainer.MapCellType.None;
         }
      }

      private struct TileInLayer
      {
         public TileBase tileBase { get; set; }
         public (int x, int y) position { get; set; }
         public string layer { get; set; }
         public int sublayer { get; set; }
         public bool shouldHaveCollider { get; set; }

         // Force tile to be rendered above other tiles and player
         public bool forceAbsoluteTop { get; set; }

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
      public CellTypesContainer.MapCellType[] mapCellTypes;
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
