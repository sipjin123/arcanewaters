using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;
using MapCreationTool;
using System;

namespace MinimapGeneration
{
   public class SeaMinimapGenerator : MonoBehaviour
   {
      #region Public Variables

      // Maximum size in tiles a map can bew
      public const int MAX_MAP_SIZE_X = 256;
      public const int MAX_MAP_SIZE_Y = 256;

      // Special icon names
      public const string treasureSiteIconName = "TreasureSite";
      public const string warpIconName = "Warp";
      public const string buildingIconPrefix = "building";

      #endregion

      private static void ensureInitialized () {
         if (initialized) {
            return;
         }

         AssetSerializationMaps.ensureLoaded();

         loadDockTiles(_dockTilesIndexSouth, _dockTilesSouth);
         loadDockTiles(_dockTilesIndexNorth, _dockTilesNorth);
         loadDockTiles(_dockTilesIndexEast, _dockTilesEast);
         loadDockTiles(_dockTilesIndexWest, _dockTilesWest);

         initialized = true;
      }

      private static void loadDockTiles (BoundsInt index, HashSet<TileBase> target) {
         foreach (Biome.Type biome in Biome.gameBiomes) {
            for (int i = 0; i < index.size.x; i++) {
               for (int j = 0; j < index.size.y; j++) {
                  TileBase tile = AssetSerializationMaps.tryGetTile(new Vector2Int(i + index.xMin, j + index.yMin), biome);
                  if (tile != null && !target.Contains(tile)) {
                     target.Add(tile);
                  }
               }
            }
         }
      }

      public static Texture2D generateMinimap (Area area, MinimapGeneratorPreset preset) {
         ensureInitialized();

         // Get the size of the map in tiles
         Vector2Int mapSize = getSize(area.getTilemapLayers());

         // Create our resulting texture
         Texture2D result = new Texture2D(mapSize.x, mapSize.x, TextureFormat.RGBA32, false);

         // Fill the texture with plain land color as a fallback
         fill(result, preset.baseLandColor);

         // Mark where land is in this map
         setMasks(_landMask, _cliffMask, _pathwayMask, _dockOrientationMask, area.getTilemapLayers(), mapSize);

         // Mark where buildings should be
         setTilemapObjectPositions(area.getTilemapLayers(), mapSize, _buildingPosition,
            l => l.name.StartsWith(MapCreationTool.Layer.BUILDING_KEY) && l.fullName.EndsWith("0") || l.fullName.EndsWith("1"));

         // Mark where docks should be
         setTilemapObjectPositions(area.getTilemapLayers(), mapSize, _dockPositions,
            l => l.name.StartsWith(MapCreationTool.Layer.DOCK_KEY));

         // Paint in all the regular water
         paintWater(result, _landMask, area.getTilemapLayers(), mapSize, preset);

         // Paint in mountains
         paintMountains(_landMask, _cliffMask, mapSize, result, preset);

         // Paint in rivers
         paintRivers(_landMask, _cliffMask, mapSize, area.getTilemapLayers(), result, preset);

         // Paint in pathways
         paintPathways(mapSize, _pathwayMask, result, preset);

         // Paint icons
         paintIcons(area, _buildingPosition, _dockPositions, _dockOrientationMask, mapSize, result, preset);

         result.Apply();

#if UNITY_EDITOR
         //System.IO.File.WriteAllBytes(Application.persistentDataPath + "/minimap.png", result.EncodeToPNG());
#endif // UNITY_EDITOR

         return result;
      }

      private static void paintIcons (Area area, List<Vector2Int> buildingPositions, List<Vector2Int> dockPositions, Direction[,] dockOrientationMask, Vector2Int mapSize, Texture2D target, MinimapGeneratorPreset preset) {
         Transform[] prefabs = area.gameObject.transform.Find("Prefabs") ? area.gameObject.transform.Find("Prefabs").GetComponentsInChildren<Transform>() : new Transform[0];
         GridLayout gridLayout = area.GetComponentInChildren<GridLayout>();
         _createdPrefabIcons.Clear();
         _createdPrefabIconsPerGrid.Clear();
         List<Vector3> positions = new List<Vector3>();

         for (int x = 0; x < mapSize.x; x++) {
            for (int y = 0; y < mapSize.y; y++) {
               _iconDepths[x, y] = float.MaxValue;
            }
         }

         // Paint mountains
         foreach (var pref in prefabs) {
            if (pref.name.StartsWith(preset.mountainIcon.iconLayerName)) {
               positions.Add(getPixelPosition(pref.transform, gridLayout, mapSize, preset.mountainIcon));
            }
         }
         paintIcons(target, mapSize, preset.mountainIcon, positions);

         // Paint docks, figure out which direction they have to face
         List<Vector3> docksNorth = new List<Vector3>();
         List<Vector3> docksSouth = new List<Vector3>();
         List<Vector3> docksWest = new List<Vector3>();
         List<Vector3> docksEast = new List<Vector3>();
         foreach (Vector2Int pos in dockPositions) {
            // Add z to force docks to be drawn below other things
            if (dockOrientationMask[pos.x, pos.y] == Direction.North) {
               Vector3 p = pixelToHybridPosition(pos, gridLayout, mapSize, preset.dockNorthIcon);
               p.z += 10f;
               docksNorth.Add(p);
            } else if (dockOrientationMask[pos.x, pos.y] == Direction.South) {
               Vector3 p = pixelToHybridPosition(pos, gridLayout, mapSize, preset.dockSouthIcon);
               p.z += 10f;
               docksSouth.Add(p);
            } else if (dockOrientationMask[pos.x, pos.y] == Direction.West) {
               Vector3 p = pixelToHybridPosition(pos, gridLayout, mapSize, preset.dockWestIcon);
               p.z += 10f;
               docksWest.Add(p);
            } else if (dockOrientationMask[pos.x, pos.y] == Direction.East) {
               Vector3 p = pixelToHybridPosition(pos, gridLayout, mapSize, preset.dockEastIcon);
               p.z += 10f;
               docksEast.Add(p);
            }
         }

         paintIcons(target, mapSize, preset.dockNorthIcon, docksNorth);
         paintIcons(target, mapSize, preset.dockSouthIcon, docksSouth);
         paintIcons(target, mapSize, preset.dockWestIcon, docksWest);
         paintIcons(target, mapSize, preset.dockEastIcon, docksEast);

         // Paint buildings
         positions.Clear();
         foreach (Vector2Int pos in buildingPositions) {
            positions.Add(pixelToHybridPosition(pos, gridLayout, mapSize, preset.houseIcon));
         }
         paintIcons(target, mapSize, preset.houseIcon, positions);

         // Paint tree 1
         positions.Clear();
         foreach (var pref in prefabs) {
            if (pref.name.StartsWith(preset.tree1Icon.iconLayerName)) {
               positions.Add(getPixelPosition(pref.transform, gridLayout, mapSize, preset.tree1Icon));
            }
         }
         paintIcons(target, mapSize, preset.tree1Icon, positions);

         // Paint tree 2
         positions.Clear();
         foreach (var pref in prefabs) {
            if (pref.name.StartsWith(preset.tree2Icon.iconLayerName)) {
               positions.Add(getPixelPosition(pref.transform, gridLayout, mapSize, preset.tree2Icon));
            }
         }
         paintIcons(target, mapSize, preset.tree2Icon, positions);

         // Paint other legacy icons that might be added
         foreach (TileIcon icon in preset._tileIconLayers) {
            if (icon.iconLayerName == treasureSiteIconName) {
               TreasureSite[] treasureSites = area.gameObject.transform.Find("Treasure Sites") ? area.gameObject.transform.Find("Treasure Sites").GetComponentsInChildren<TreasureSite>() : new TreasureSite[0];
               foreach (var treasureSite in treasureSites) {
                  if (treasureSite.isActive() && treasureSite.GetComponent<SpriteRenderer>() && treasureSite.GetComponent<SpriteRenderer>().enabled) {
                     paintIcon(area, target, mapSize, treasureSite.transform, icon);
                  }
               }
            } else if (icon.usePrefab) {
               positions.Clear();

               foreach (var pref in prefabs) {
                  if (pref.name.StartsWith(icon.iconLayerName)) {
                     // Special case of prefab icon - warps
                     if (icon.iconLayerName == warpIconName) {
                        if (area.isSea && pref.GetComponent<Warp>()?.targetInfo?.specialType == Area.SpecialType.Town) {
                           Sprite warpTownSprite = Minimap.self.getTownWarpSprite(pref.GetComponent<Warp>().targetInfo.biome);
                           if (warpTownSprite != null) {
                              paintIcon(area, target, mapSize, pref.transform, warpTownSprite, new Vector2Int(-3, -3));
                           }
                        }
                        break;
                     }

                     positions.Add(getPixelPosition(pref.transform, gridLayout, mapSize, icon));
                  }
               }
               paintIcons(target, mapSize, icon, positions);
            }
         }
      }

      private static Vector3 getPixelPosition (Transform target, GridLayout gridLayout, Vector2Int mapSize, TileIcon icon) {
         Vector2Int collider2DCellPosition = new Vector2Int(
            gridLayout.WorldToCell(target.position).x,
            -gridLayout.WorldToCell(target.position).y);

         var sprite = icon.spriteIcon;

         if (sprite) {
            int xSetPixel = Mathf.Clamp(collider2DCellPosition.x + icon.offset.x - (-mapSize.x / 2), 0, mapSize.x - (int) sprite.textureRect.width);
            int ySetPixel = Mathf.Clamp(mapSize.x + icon.offset.y - (collider2DCellPosition.y - (-mapSize.y / 2)), 0, mapSize.y - (int) sprite.textureRect.height);

            return new Vector3(xSetPixel, ySetPixel, target.position.z);
         }

         D.warning("Missing icon for: " + icon.iconLayerName);
         return Vector3.zero;
      }

      private static Vector3 pixelToHybridPosition (Vector2Int pixel, GridLayout gridLayout, Vector2Int mapSize, TileIcon icon) {
         var sprite = icon.spriteIcon;

         if (sprite) {
            int xSetPixel = Mathf.Clamp(pixel.x + icon.offset.x, 0, mapSize.x - (int) sprite.textureRect.width);
            int ySetPixel = Mathf.Clamp(icon.offset.y + pixel.y, 0, mapSize.y - (int) sprite.textureRect.height);
            float z = Util.TruncateTo100ths(gridLayout.CellToWorld(new Vector3Int(pixel.x - mapSize.x / 2, pixel.y - mapSize.y / 2, 0)).y) * 0.01f;
            return new Vector3(xSetPixel, ySetPixel, z);
         }

         D.warning("Missing icon for: " + icon.iconLayerName);
         return Vector3.zero;
      }

      private static void paintIcons (Texture2D target, Vector2Int mapSize, TileIcon icon, List<Vector3> positions) {
         int placed = 0;
         for (int k = 0; k < positions.Count; k++) {
            int xSetPixel = (int) positions[k].x;
            int ySetPixel = (int) positions[k].y;
            float z = positions[k].z;
            bool saveResult = true;

            if (icon.limitSpawnCount) {
               // Check if icon already exist in grid
               string gridKey = icon.iconLayerName + "grid_" + (xSetPixel / icon.spawnGridSize.x) + "_" + (ySetPixel / icon.spawnGridSize.y);
               if (!_createdPrefabIconsPerGrid.Contains(gridKey)) {
                  // Check if distance between icons of given type is correct
                  if (_createdPrefabIcons.ContainsKey(icon.iconLayerName) && icon.minDistanceManhattan > 0) {
                     foreach (var pos in _createdPrefabIcons[icon.iconLayerName]) {
                        if (Math.Abs(xSetPixel - pos.x) + Math.Abs(ySetPixel - pos.y) < icon.minDistanceManhattan) {
                           saveResult = false;
                           break;
                        }
                     }
                  }
                  // Check if distance between all icons is corect
                  if (icon.minGlobalDistanceManhattan > 0) {
                     foreach (List<Vector2Int> posList in _createdPrefabIcons.Values) {
                        foreach (Vector2Int pos in posList) {
                           if (Math.Abs(xSetPixel - pos.x) + Math.Abs(ySetPixel - pos.y) < icon.minGlobalDistanceManhattan) {
                              saveResult = false;
                              break;
                           }
                        }
                     }
                  }
               } else {
                  saveResult = false;
               }

               // Save grid dictionary entry earlier to avoid recreating string
               if (saveResult) {
                  _createdPrefabIconsPerGrid.Add(gridKey);
               }
            }

            var sprite = icon.spriteIcon;
            if (icon.altIcons != null && icon.altIcons.Length > 0) {
               if (placed % (icon.altIcons.Length + 1) > 0) {
                  sprite = icon.altIcons[(placed % (icon.altIcons.Length + 1)) - 1];
               }
            }

            if (sprite) {
               if (saveResult) {
                  placed++;

                  var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
                     (int) sprite.textureRect.y,
                     (int) sprite.textureRect.width,
                     (int) sprite.textureRect.height);

                  if (icon.limitSpawnCount) {
                     if (!_createdPrefabIcons.ContainsKey(icon.iconLayerName)) {
                        _createdPrefabIcons.Add(icon.iconLayerName, new List<Vector2Int>());
                     }
                     _createdPrefabIcons[icon.iconLayerName].Add(new Vector2Int(xSetPixel, ySetPixel));
                  }

                  for (int y = 0; y < (int) sprite.rect.height; y++) {
                     for (int x = 0; x < (int) sprite.rect.width; x++) {
                        Color pixel = pixels[y * (int) sprite.rect.width + x];
                        //Color pixel = Minimap.AlphaBlend(target.GetPixel(pos.x, pos.y), pixels[y * (int) sprite.rect.width + x]);
                        Vector2Int pos = new Vector2Int(xSetPixel + x, ySetPixel + y);
                        if (pixel.a > 0.05f && _iconDepths[pos.x, pos.y] > z) {
                           target.SetPixel(pos.x, pos.y, pixel);
                           _iconDepths[pos.x, pos.y] = z;
                        }
                     }
                  }
               }
            }
         }
      }

      private static void paintIcon (Area area, Texture2D target, Vector2Int mapSize, Transform transform, Sprite sprite, Vector2Int offset) {
         GridLayout gridLayout = area.GetComponentInChildren<GridLayout>();
         Vector2Int collider2DCellPosition = new Vector2Int(gridLayout.WorldToCell(transform.position).x, -gridLayout.WorldToCell(transform.position).y);

         if (sprite) {
            var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
                  (int) sprite.textureRect.y,
                  (int) sprite.textureRect.width,
                  (int) sprite.textureRect.height);

            int xSetPixel = Mathf.Clamp(collider2DCellPosition.x + offset.x - (-mapSize.x / 2), 0, mapSize.x - (int) sprite.textureRect.width);
            int ySetPixel = Mathf.Clamp(mapSize.y + offset.y - (collider2DCellPosition.y - (-mapSize.y / 2)), 0, mapSize.y - (int) sprite.textureRect.height);

            for (int i = 0; i < sprite.rect.width; i++) {
               for (int j = 0; j < sprite.rect.height; j++) {
                  pixels[j * (int) sprite.rect.width + i] =
                     Minimap.AlphaBlend(target.GetPixel(xSetPixel + i, ySetPixel + j), pixels[j * (int) sprite.rect.width + i]);
               }
            }

            target.SetPixels(xSetPixel, ySetPixel, (int) sprite.rect.width, (int) sprite.rect.height, pixels);
         }
      }

      private static void paintIcon (Area area, Texture2D target, Vector2Int mapSize, Transform transform, TileIcon icon) {
         paintIcon(area, target, mapSize, transform, icon.spriteIcon, icon.offset);
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

      private static void paintWater (Texture2D target, bool[,] landMask, List<TilemapLayer> layers, Vector2Int mapSize, MinimapGeneratorPreset preset) {
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

         // Color deep water
         Vector3Int offset = -(Vector3Int) mapSize / 2;
         foreach (TilemapLayer layer in layers) {
            string ln = layer.name.ToLower();
            if (ln.Contains(MapCreationTool.Layer.WATER_KEY) && (layer.fullName.EndsWith("4") || layer.fullName.EndsWith("5"))) {
               for (int i = 0; i < mapSize.x; i++) {
                  for (int j = 0; j < mapSize.y; j++) {
                     if (layer.tilemap.HasTile(new Vector3Int(i + offset.x, j + offset.y, 0))) {
                        target.SetPixel(i, j, preset.deeper2WaterColor);
                     }
                  }
               }
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

      private static void setMasks (bool[,] landMask, CliffType[,] cliffMask, bool[,] pathwayMask, Direction[,] dockOrientationMask, List<TilemapLayer> layers, Vector2Int mapSize) {
         setLandMask(landMask, layers, mapSize);
         setCliffMask(cliffMask, layers, mapSize);
         setPathwayMask(pathwayMask, cliffMask, layers, mapSize);
         setDockOrientationMask(dockOrientationMask, layers, mapSize);
      }

      private static void setPathwayMask (bool[,] pathwayMask, CliffType[,] cliffMask, List<TilemapLayer> layers, Vector2Int mapSize) {
         bool[,] pm = pathwayMask;
         clear(pm);

         Vector3Int offset = -(Vector3Int) mapSize / 2;

         // First set the mask to be 1 to 1 with the tiles in the map
         foreach (TilemapLayer l in layers) {
            if (l.name.Contains(MapCreationTool.Layer.PATHWAY_KEY)) {
               for (int i = 0; i < pm.GetLength(0); i++) {
                  for (int j = 0; j < pm.GetLength(1); j++) {
                     pm[i, j] = l.tilemap.HasTile(new Vector3Int(i + offset.x, j + offset.y, 0));
                  }
               }
            }
         }

         // The tiles give a line that's too thick, try to remove some corner pixels to thin it out
         for (int i = 1; i < pm.GetLength(0) - 1; i++) {
            for (int j = 1; j < pm.GetLength(1) - 1; j++) {
               if (pm[i, j + 1] && pm[i + 1, j] && !pm[i + 1, j + 1] && !pm[i - 1, j] && !pm[i, j - 1]) {
                  pm[i, j] = false;
               } else if (pm[i, j - 1] && pm[i - 1, j] && !pm[i - 1, j - 1] && !pm[i, j + 1] && !pm[i + 1, j]) {
                  pm[i, j] = false;
               } else if (pm[i, j + 1] && pm[i - 1, j] && !pm[i - 1, j + 1] && !pm[i, j - 1] && !pm[i + 1, j]) {
                  pm[i, j] = false;
               } else if (pm[i, j - 1] && pm[i + 1, j] && !pm[i + 1, j - 1] && !pm[i, j + 1] && !pm[i - 1, j]) {
                  pm[i, j] = false;
               }
            }
         }

         // Try to 'pull back' the pathway tile that is the last in a path and overhangs over a mountain
         for (int i = 1; i < pm.GetLength(0) - 1; i++) {
            for (int j = 1; j < pm.GetLength(1) - 1; j++) {
               if (cliffMask[i, j] != CliffType.None && pm[i, j]) {
                  int c = 0;

                  if (pm[i, j + 1]) c++;
                  if (pm[i + 1, j + 1]) c++;
                  if (pm[i + 1, j]) c++;
                  if (pm[i + 1, j - 1]) c++;
                  if (pm[i, j - 1]) c++;
                  if (pm[i - 1, j - 1]) c++;
                  if (pm[i - 1, j]) c++;
                  if (pm[i - 1, j + 1]) c++;

                  if (c == 1) {
                     pm[i, j] = false;
                  }
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
         clear(landMask);

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

      private static void setDockOrientationMask (Direction[,] mask, List<TilemapLayer> layers, Vector2Int mapSize) {
         for (int i = 0; i < mapSize.x; i++) {
            for (int j = 0; j < mapSize.y; j++) {
               mask[i, j] = (Direction) 0;
            }
         }

         Vector3Int offset = -(Vector3Int) mapSize / 2;

         // Find dock tiles and mark which direction they are facing
         foreach (TilemapLayer layer in layers) {
            string ln = layer.name.ToLower();
            if (ln.Contains(MapCreationTool.Layer.DOCK_KEY)) {
               for (int i = 0; i < mapSize.x; i++) {
                  for (int j = 0; j < mapSize.y; j++) {
                     TileBase tile = layer.tilemap.GetTile(new Vector3Int(i + offset.x, j + offset.y, 0));
                     if (_dockTilesEast.Contains(tile)) {
                        mask[i, j] = Direction.East;
                     } else if (_dockTilesWest.Contains(tile)) {
                        mask[i, j] = Direction.West;
                     } else if (_dockTilesNorth.Contains(tile)) {
                        mask[i, j] = Direction.North;
                     } else if (_dockTilesSouth.Contains(tile)) {
                        mask[i, j] = Direction.South;
                     }
                  }
               }
            }
         }
      }

      private static void setTilemapObjectPositions (List<TilemapLayer> layers, Vector2Int mapSize, List<Vector2Int> positions, Predicate<TilemapLayer> fromLayers) {
         clear(_raBuffer, true);
         positions.Clear();
         Vector3Int offset = -(Vector3Int) mapSize / 2;

         // Fill out the mask and set which pixels have a building in it
         foreach (TilemapLayer layer in layers) {
            if (fromLayers(layer)) {
               for (int i = 0; i < mapSize.x; i++) {
                  for (int j = 0; j < mapSize.y; j++) {
                     if (layer.tilemap.HasTile(new Vector3Int(i + offset.x, j + offset.y, 0))) {
                        _raBuffer[i, j] = false;
                     }
                  }
               }
            }
         }

         // Clump pixels into groups
         for (int i = 0; i < mapSize.x; i++) {
            for (int j = 0; j < mapSize.y; j++) {
               if (!_raBuffer[i, j]) {
                  _raQueue.Clear();
                  _raQueue.Enqueue((i, j));
                  (int minX, int minY, int maxX, int maxY) bounds = (i, j, i, j);

                  while (_raQueue.Count > 0) {
                     (int x, int y) index = _raQueue.Dequeue();

                     // Track down the minimum and maximum indexes
                     if (index.x < bounds.minX) bounds.minX = index.x;
                     if (index.x > bounds.maxX) bounds.maxX = index.x;
                     if (index.y < bounds.minY) bounds.minY = index.y;
                     if (index.y > bounds.maxY) bounds.maxY = index.y;

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
                  }

                  // Based on the bounds, add center as the position of the building
                  positions.Add(new Vector2Int((bounds.maxX + bounds.minX) / 2, (bounds.maxY + bounds.minY) / 2));
               }
            }
         }
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

      private static void clear (bool[,] target, bool clearWithTrue = false) {
         for (int i = 0; i < target.GetLength(0); i++) {
            for (int j = 0; j < target.GetLength(1); j++) {
               target[i, j] = clearWithTrue;
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

      // Have we initialized yet
      private static bool initialized = false;

      // Which tiles belong to which dock (index in asset serialization maps)
      private static BoundsInt _dockTilesIndexSouth = new BoundsInt(63, 20, 0, 3, 3, 0);
      private static BoundsInt _dockTilesIndexNorth = new BoundsInt(63, 23, 0, 3, 2, 0);
      private static BoundsInt _dockTilesIndexEast = new BoundsInt(63, 25, 0, 2, 4, 0);
      private static BoundsInt _dockTilesIndexWest = new BoundsInt(63, 29, 0, 2, 4, 0);
      private static HashSet<TileBase> _dockTilesSouth = new HashSet<TileBase>();
      private static HashSet<TileBase> _dockTilesNorth = new HashSet<TileBase>();
      private static HashSet<TileBase> _dockTilesEast = new HashSet<TileBase>();
      private static HashSet<TileBase> _dockTilesWest = new HashSet<TileBase>();

      // Caches all the positions where land is placed
      private static bool[,] _landMask = new bool[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Caches all the positions where land is placed
      private static CliffType[,] _cliffMask = new CliffType[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Caches which pixels are considered pathways
      private static bool[,] _pathwayMask = new bool[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Caches which direction should docks be facing in every pixel
      private static Direction[,] _dockOrientationMask = new Direction[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Positions of buildings in the map
      private static List<Vector2Int> _buildingPosition = new List<Vector2Int>();

      // Positions of docks in the map
      private static List<Vector2Int> _dockPositions = new List<Vector2Int>();

      // Random access buffer of max map size
      private static bool[,] _raBuffer = new bool[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      // Random access queue for algorithms
      private static Queue<(int x, int y)> _raQueue = new Queue<(int x, int y)>();

      // Buffers for checking which icons were placed already
      private static HashSet<string> _createdPrefabIconsPerGrid = new HashSet<string>();
      private static Dictionary<string, List<Vector2Int>> _createdPrefabIcons = new Dictionary<string, List<Vector2Int>>();
      private static float[,] _iconDepths = new float[MAX_MAP_SIZE_X, MAX_MAP_SIZE_Y];

      #endregion
   }
}