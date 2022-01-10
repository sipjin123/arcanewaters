using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using MinimapGeneration;
using UnityEngine.Tilemaps;
using MapCreationTool;

namespace MinimapGeneration
{
   public class SeaMinimapGenerator : MonoBehaviour
   {
      #region Public Variables

      // Maximum size in tiles a map can bew
      public const int MAX_MAP_SIZE_X = 256;
      public const int MAX_MAP_SIZE_Y = 256;

      #endregion

      public static Texture2D generateMinimap (Area area, MinimapGeneratorPreset preset) {
         // Get the size of the map in tiles
         Vector2Int mapSize = getSize(area.getTilemapLayers());

         // Create our resulting texture
         Texture2D result = new Texture2D(mapSize.x, mapSize.x);

         // Fill the texture with plain land color as a fallback
         fill(result, preset.baseLandColor);

         // Mark where land is in this map
         setMasks(_landMask, _cliffMask, _pathwayMask, area.getTilemapLayers(), mapSize);

         // Paint in all the regular water
         paintWater(result, _landMask, mapSize, preset);

         // Paint in mountains
         paintMountains(_landMask, _cliffMask, mapSize, result, preset);

         // Paint in rivers
         paintRivers(_landMask, _cliffMask, mapSize, area.getTilemapLayers(), result, preset);

         // Paint in pathways
         paintPathways(mapSize, _pathwayMask, result, preset);

         result.Apply();
         return result;
      }

      private static void paintPathways (Vector2Int mapSize, bool[,] pathwayMask, Texture2D target, MinimapGeneratorPreset preset) {
         for (int i = 0; i < mapSize.x; i++) {
            for (int j = 0; j < mapSize.y; j++) {
               if (pathwayMask[i, j]) {
                  // Place a road pixel if we have a tile here
                  target.SetPixel(i, j, preset.pathwayColor);
               } else if ((i > 0 && pathwayMask[i - 1, j]) || (j > 0 && pathwayMask[i, j - 1]) ||
                  (i < mapSize.x - 1 && pathwayMask[i + 1, j]) || (j < mapSize.y - 1 && pathwayMask[i, j + 1])) {
                  // Place a border tile if path is close
                  target.SetPixel(i, j, preset.pathwayBorderColor);
               }
            }
         }
      }

      private static void paintRivers (bool[,] landMask, CliffType[,] cliffMask, Vector2Int mapSize, List<TilemapLayer> layers, Texture2D target, MinimapGeneratorPreset preset) {
         Vector3Int offset = -(Vector3Int) mapSize / 2;

         foreach (TilemapLayer l in layers) {
            if (!l.name.Contains(MapCreationTool.Layer.RIVER_KEY)) {
               continue;
            }
            Tilemap tm = l.tilemap;
            for (int i = 0; i < mapSize.x; i++) {
               for (int j = 0; j < mapSize.y; j++) {
                  if (tm.HasTile(new Vector3Int(i + offset.x, j + offset.y, 0))) {
                     if (cliffMask[i, j] == CliffType.Outline || cliffMask[i, j] == CliffType.Lower) {
                        // If it overlaps cliff, paint in waterfall color
                        target.SetPixel(i, j, preset.waterfallColor);
                     } else if (cliffMask[i, j] == CliffType.Upper) {
                        // If it overlaps cliff's edge, paint in border color
                        target.SetPixel(i, j, preset.waterBorderColor);
                     } else if (j < mapSize.y - 1 && cliffMask[i, j + 1] == CliffType.Outline &&
                        tm.HasTile(new Vector3Int(i + offset.x, j + offset.y + 1, 0))) {
                        // If a tile is close below a waterfall, paint in border color
                        target.SetPixel(i, j, preset.waterBorderColor);
                     } else {
                        // Overwise, just paint water color
                        target.SetPixel(i, j, preset.waterColor);
                     }
                  }
                  if (!landMask[i, j] && j < mapSize.y - 2 && cliffMask[i, j + 2] == CliffType.Outline &&
                     tm.HasTile(new Vector3Int(i + offset.x, j + offset.y + 2, 0))) {
                     // If a tile is close below a waterfall in water, paint in border color
                     target.SetPixel(i, j, preset.waterBorderColor);
                  }
               }
            }
         }
      }

      private static void paintWater (Texture2D target, bool[,] landMask, Vector2Int mapSize, MinimapGeneratorPreset preset) {
         _raQueue.Clear();

         // Use random access buffer for marking pixels as 'used'
         // Initially, add all the land tiles to the queue
         // They will add water tiles next and they will be painted
         for (int i = 0; i < mapSize.x; i++) {
            for (int j = 0; j < mapSize.x; j++) {
               _raBuffer[i, j] = landMask[i, j];
               if (landMask[i, j]) {
                  _raQueue.Enqueue((i, j));
               }
            }
         }

         int dist = 0;
         int remaining = _raQueue.Count;

         while (_raQueue.Count > 0) {
            (int x, int y) index = _raQueue.Dequeue();
            remaining--;

            // Color based on distance
            if (dist == 1) {
               target.SetPixel(index.x, index.y, preset.waterBorderColor);
            } else if (dist > 0 && dist < 5) {
               target.SetPixel(index.x, index.y, preset.waterColor);
            } else if (dist > 4) {
               target.SetPixel(index.x, index.y, preset.deeper1WaterColor);
            }

            // Add neighbouring elements
            if (index.x > 0 && !_raBuffer[index.x - 1, index.y]) {
               _raQueue.Enqueue((index.x - 1, index.y));
               _raBuffer[index.x - 1, index.y] = true;
            }
            if (index.x < mapSize.x - 1 && !_raBuffer[index.x + 1, index.y]) {
               _raQueue.Enqueue((index.x + 1, index.y));
               _raBuffer[index.x + 1, index.y] = true;
            }
            if (index.y > 0 && !_raBuffer[index.x, index.y - 1]) {
               _raQueue.Enqueue((index.x, index.y - 1));
               _raBuffer[index.x, index.y - 1] = true;
            }
            if (index.y < mapSize.y - 1 && !_raBuffer[index.x, index.y + 1]) {
               _raQueue.Enqueue((index.x, index.y + 1));
               _raBuffer[index.x, index.y + 1] = true;
            }

            // If we used up all remaining elements, we move up to the next distance
            if (remaining <= 0) {
               remaining = _raQueue.Count;
               dist++;
            }
         }
      }

      private static void paintMountains (bool[,] landMask, CliffType[,] cliffMask, Vector2Int mapSize, Texture2D target, MinimapGeneratorPreset preset) {
         for (int i = 0; i < mapSize.x; i++) {
            for (int j = 0; j < mapSize.y; j++) {
               // Check if this is a mountain tile
               if (!landMask[i, j]) {
                  continue;
               }

               // Paint according to cliff data
               switch (cliffMask[i, j]) {
                  case CliffType.None:
                     target.SetPixel(i, j, preset.baseLandColor);
                     break;
                  case CliffType.Outline:
                     target.SetPixel(i, j, preset.landOutlineColor);
                     break;
                  case CliffType.Lower:
                     target.SetPixel(i, j, preset.landCliffLowerColor);
                     break;
                  case CliffType.Upper:
                     target.SetPixel(i, j, preset.landCliffUpperColor);
                     break;
                  case CliffType.Edge:
                     target.SetPixel(i, j, preset.landBorderColor);
                     break;
               }
            }
         }
      }

      private static void fill (Texture2D target, Color color) {
         for (int i = 0; i < target.width; i++) {
            for (int j = 0; j < target.height; j++) {
               target.SetPixel(i, j, color);
            }
         }
      }

      public static SidesInt surroundingCount (Tilemap tm, Vector2Int mapSize, Vector3Int from, SidesInt maxDepth, bool forceIncludeOffMap) {
         SidesInt maxCheck = new SidesInt {
            left = Mathf.Min(from.x, maxDepth.left),
            right = Mathf.Min(mapSize.x - from.x - 1, maxDepth.right),
            bot = Mathf.Min(from.y, maxDepth.bot),
            top = Mathf.Min(mapSize.y - from.y - 1, maxDepth.top)
         };

         Vector3Int offset = -(Vector3Int) mapSize / 2;
         SidesInt result = new SidesInt();

         for (int i = 1; i <= maxCheck.left; i++) {
            if (tm.HasTile(from + offset + Vector3Int.left * i))
               result.left++;
            else
               break;
         }

         for (int i = 1; i <= maxCheck.right; i++) {
            if (tm.HasTile(from + offset + Vector3Int.right * i))
               result.right++;
            else
               break;
         }

         for (int i = 1; i <= maxCheck.top; i++) {
            if (tm.HasTile(from + offset + Vector3Int.up * i))
               result.top++;
            else
               break;
         }

         for (int i = 1; i <= maxCheck.bot; i++) {
            if (tm.HasTile(from + offset + Vector3Int.down * i))
               result.bot++;
            else
               break;
         }

         if (forceIncludeOffMap) {
            // If anything hit max and we force include off map, make it max depth as if it extends into unknown
            if (result.left == maxCheck.left) {
               result.left = maxDepth.left;
            }
            if (result.right == maxCheck.right) {
               result.right = maxDepth.right;
            }
            if (result.bot == maxCheck.bot) {
               result.bot = maxDepth.bot;
            }
            if (result.top == maxCheck.top) {
               result.top = maxDepth.top;
            }
         }
         return result;
      }

      private static List<TilemapLayer> getMountainLayersAsceding (List<TilemapLayer> layers) {
         List<TilemapLayer> results = new List<TilemapLayer>();

         foreach (TilemapLayer l in layers) {
            if (l.name.Contains(MapCreationTool.Layer.MOUNTAIN_KEY)) {
               results.Add(l);
            }
         }

         return results.OrderBy(l => {
            if (int.TryParse(l.fullName.Replace(MapCreationTool.Layer.MOUNTAIN_KEY, ""), out int subLayer)) {
               return subLayer;
            }
            return 0;
         }).ToList();
      }

      private static Vector2Int getSize (List<TilemapLayer> layers) {
         Vector2Int size = new Vector2Int(0, 0);

         foreach (TilemapLayer l in layers) {
            size = new Vector2Int(Mathf.Max(size.x, l.tilemap.size.x), Mathf.Max(size.y, l.tilemap.size.y));
         }

         return size;
      }

      private static void setMasks (bool[,] landMask, CliffType[,] cliffMask, bool[,] pathwayMask, List<TilemapLayer> layers, Vector2Int mapSize) {
         setLandMask(landMask, layers, mapSize);
         setCliffMask(cliffMask, layers, mapSize);
         setPathwayMask(pathwayMask, layers, mapSize);
      }

      private static void setPathwayMask (bool[,] pathwayMask, List<TilemapLayer> layers, Vector2Int mapSize) {
         clear(pathwayMask);

         Vector3Int offset = -(Vector3Int) mapSize / 2;

         // First set the mask to be 1 to 1 with the tiles in the map
         foreach (TilemapLayer l in layers) {
            if (l.name.Contains(MapCreationTool.Layer.PATHWAY_KEY)) {
               for (int i = 0; i < pathwayMask.GetLength(0); i++) {
                  for (int j = 0; j < pathwayMask.GetLength(1); j++) {
                     pathwayMask[i, j] = l.tilemap.HasTile(new Vector3Int(i + offset.x, j + offset.y, 0));
                  }
               }
            }
         }

         // The tiles give a line that's too thick, try to remove some corner pixels to thin it out
         for (int i = 1; i < pathwayMask.GetLength(0) - 1; i++) {
            for (int j = 1; j < pathwayMask.GetLength(1) - 1; j++) {
               if (pathwayMask[i, j + 1] && pathwayMask[i + 1, j] && !pathwayMask[i + 1, j + 1]) {
                  pathwayMask[i, j] = false;
               } else if (pathwayMask[i, j - 1] && pathwayMask[i - 1, j] && !pathwayMask[i - 1, j - 1]) {
                  pathwayMask[i, j] = false;
               } else if (pathwayMask[i, j + 1] && pathwayMask[i - 1, j] && !pathwayMask[i - 1, j + 1]) {
                  pathwayMask[i, j] = false;
               } else if (pathwayMask[i, j - 1] && pathwayMask[i + 1, j] && !pathwayMask[i + 1, j - 1]) {
                  pathwayMask[i, j] = false;
               }
            }
         }
      }

      private static void setCliffMask (CliffType[,] cliffMask, List<TilemapLayer> layers, Vector2Int mapSize) {
         for (int i = 0; i < mapSize.x; i++) {
            for (int j = 0; j < mapSize.y; j++) {
               cliffMask[i, j] = CliffType.None;
            }
         }

         Vector3Int offset = -(Vector3Int) mapSize / 2;

         // Mark what part of mountain land is
         foreach (TilemapLayer l in getMountainLayersAsceding(layers)) {
            Tilemap tm = l.tilemap;
            for (int i = 0; i < mapSize.x; i++) {
               // Keep track of how many pixels from the bottom border we are at
               int pixelsFromEdge = 100;
               for (int j = 0; j < mapSize.y; j++) {
                  // Check if this is a mountain tile
                  if (!tm.HasTile(new Vector3Int(i + offset.x, j + offset.y, 0))) {
                     continue;
                  }
                  pixelsFromEdge++;

                  // Get surrounding count
                  SidesInt sur = surroundingCount(tm, mapSize, new Vector3Int(i, j, 0), SidesInt.uniform(2), true);

                  if (sur.min() == 0) {
                     // If this is an outline of a mountain
                     cliffMask[i, j] = CliffType.Outline;
                     pixelsFromEdge = 0;
                  } else if (pixelsFromEdge == 1) {
                     // If there is 1 pixel below
                     cliffMask[i, j] = CliffType.Lower;
                  } else if (pixelsFromEdge == 2) {
                     // If there are 2 pixels below
                     cliffMask[i, j] = CliffType.Upper;
                  } else {
                     // Otherwise, we are inside the mountain mass
                     if (sur.min() == 1 || pixelsFromEdge == 3) {
                        // If we are 1 pixel from the cliff
                        cliffMask[i, j] = CliffType.Edge;
                     } else {
                        // Otherwise, we are far inside mountain mass
                        cliffMask[i, j] = CliffType.None;
                     }
                  }
               }
            }
         }
      }

      private static void setLandMask (bool[,] landMask, List<TilemapLayer> layers, Vector2Int mapSize) {
         clear(_landMask);

         Vector3Int offset = -(Vector3Int) mapSize / 2;

         // Mark pixels which are land
         foreach (TilemapLayer layer in layers) {
            string ln = layer.name.ToLower();
            if (ln.Contains(MapCreationTool.Layer.MOUNTAIN_KEY) || ln.Contains(MapCreationTool.Layer.GRASS_KEY)) {
               for (int i = 0; i < mapSize.x; i++) {
                  for (int j = 0; j < mapSize.y; j++) {
                     landMask[i, j] |= layer.tilemap.HasTile(new Vector3Int(i + offset.x, j + offset.y, 0));
                  }
               }
            }
         }
      }

      private static void clear (bool[,] target) {
         for (int i = 0; i < target.GetLength(0); i++) {
            for (int j = 0; j < target.GetLength(1); j++) {
               target[i, j] = false;
            }
         }
      }

      #region Private Variables

      // What part of mountain a pixel is
      private enum CliffType
      {
         None = 0,
         Outline = 1,
         Lower = 2,
         Upper = 3,
         Edge = 4
      }

      // Caches all the positions where land is placed
      private static bool[,] _landMask = new bool[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Caches all the positions where land is placed
      private static CliffType[,] _cliffMask = new CliffType[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Caches which pixels are considered pathways
      private static bool[,] _pathwayMask = new bool[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Random access buffer of max map size
      private static bool[,] _raBuffer = new bool[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Random access queue for algorithms
      private static Queue<(int x, int y)> _raQueue = new Queue<(int x, int y)>();

      #endregion
   }
}