using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using ProceduralMap;
using UnityEngine.Tilemaps;

public static class RandomMapCreator {
   #region Public Variables

   #endregion

   /// <summary>
   /// Generate and moves player to created map
   /// </summary>
   public static GameObject generateRandomMap (MapConfig mapConfig) {
      // Generate map and save it to variable
      GameObject generatedMap = GenerateMap(RandomMapManager.self.presets.ChooseRandom(), mapConfig);

      if (generatedMap == null) {
         D.error("Failed to generate map");
         return null;
      }

      // Scale down the generated map
      generatedMap.transform.localScale = new Vector3(0.16f, 0.16f, 1.0f);

      // Set the parent and local position
      Area area = AreaManager.self.getArea(mapConfig.areaType);
      generatedMap.transform.SetParent(area.transform, false);
      generatedMap.transform.localPosition = new Vector3(0f, -10.24f, 0f);

      return generatedMap;
   }

   /// <summary>
   /// Generate the map object
   /// </summary>
   private static GameObject GenerateMap (MapGeneratorPreset preset, MapConfig mapConfig) {
      if (preset == null) {
         D.error("Map contains empty preset!");
         return null;
      }

      GameObject prefab = new GameObject(preset.MapPrefixName + preset.MapName + preset.MapSuffixName);
      GameObject gridLayers = new GameObject("Grid Layers");
      gridLayers.transform.parent = prefab.transform;

      gridLayers.AddComponent(typeof(Grid));

      Rigidbody2D rigidbody2D = gridLayers.AddComponent(typeof(Rigidbody2D)) as Rigidbody2D;
      rigidbody2D.bodyType = RigidbodyType2D.Static;

      CompositeCollider2D compositeCollider = gridLayers.AddComponent(typeof(CompositeCollider2D)) as CompositeCollider2D;

      int seed = mapConfig.seed;
      float persistance = mapConfig.persistance;
      float lacunarity = mapConfig.lacunarity;
      Vector2 offset = mapConfig.offset;

      float[,] noiseMap = GenerateNoiseMap(preset.mapSize, seed, preset.noiseScale, preset.octaves, persistance, lacunarity, offset);

      for (int i = 0; i < preset.layers.Length; i++) {
         GameObject layerObject = new GameObject(preset.layers[i].name);
         layerObject.transform.position = new Vector3(layerObject.transform.position.x, layerObject.transform.position.y, (float) (preset.layers.Length - i) / 100);
         layerObject.transform.parent = gridLayers.transform;
         Tilemap tilemap = layerObject.AddComponent(typeof(Tilemap)) as Tilemap;
         TilemapRenderer tilemapRenderer = layerObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
         tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

         if (preset.layers[i].useCollider) {
            TilemapCollider2D tilemapCollider = layerObject.AddComponent(typeof(TilemapCollider2D)) as TilemapCollider2D;
            tilemapCollider.usedByComposite = true;
         }

         SetBaseLayers(preset, noiseMap, i, tilemap);
         if (preset.layers[i].useBorderOnDiferentLayer) {
            GameObject borderLayerObject = new GameObject(preset.layers[i].name + preset.layers[i].BorderLayerName);
            borderLayerObject.transform.position = new Vector3(borderLayerObject.transform.position.x, borderLayerObject.transform.position.y, (float) (preset.layers.Length - i) / 100);
            borderLayerObject.transform.parent = gridLayers.transform;
            Tilemap borderTilemap = borderLayerObject.AddComponent(typeof(Tilemap)) as Tilemap;
            TilemapRenderer borderTilemapRenderer = borderLayerObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
            borderTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

            //SetBorder(preset, i, tilemap, tilemap, true);
            SetBorder(preset, i, tilemap, borderTilemap, false);
         } else {
            SetBorder(preset, i, tilemap, tilemap, false);
         }

      }
      // TODO Should there be prefab generated?
      // GeneratePrefab(preset.mapPath, prefab); 

      return prefab;
   }
   /// <summary>
   /// set base tiles on the paramref name="tilemap"  using the paramref name="noiseMap"
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="noiseMap">noise</param>
   /// <param name="i">index</param>
   /// <param name="tilemap">tilemap used</param>
   private static void SetBaseLayers (MapGeneratorPreset preset, float[,] noiseMap, int i, Tilemap tilemap) {
      float[] layersHeight = null;

      if (!preset.usePresetLayerHeight) {
         layersHeight = CalculateLayerHeight(preset.layers.Length);
      }

      for (int y = 0; y < preset.mapSize.y; y++) {
         for (int x = 0; x < preset.mapSize.x; x++) {
            float currentHeight = noiseMap[x, y];

            if (preset.usePresetLayerHeight) {
               if (currentHeight <= preset.layers[i].height) {
                  tilemap.SetTile(new Vector3Int(x, y, 0), preset.layers[i].tile);
                  //tilemap.SetTile(new Vector3Int(-x + preset.mapSize.x / 2, -y + preset.mapSize.y / 2, 0), preset.layers[i].tile);
               }
            } else {
               if (currentHeight <= layersHeight[i]) {
                  tilemap.SetTile(new Vector3Int(x, y, 0), preset.layers[i].tile);
                  //tilemap.SetTile(new Vector3Int(-x + preset.mapSize.x / 2, -y + preset.mapSize.y / 2, 0), preset.layers[i].tile);
               }
            }
         }
      }
   }

   /// <summary>
   /// use the paramref name="checkedTilemap" to find the borders and set on paramref name="setTilemap" 
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="i">index</param>
   /// <param name="checkedTilemap">tilemap to check</param>
   /// <param name="setTilemap">tilemap to set</param>
   /// <param name="diferentLayerBorder">create new border for the layer</param>
   private static void SetBorder (MapGeneratorPreset preset, int i, Tilemap checkedTilemap, Tilemap setTilemap, bool diferentLayerBorder) {
      for (int x = 0; x < preset.mapSize.x; x++) {
         for (int y = 0; y < preset.mapSize.y; y++) {
            Vector3Int cellPos = new Vector3Int(x, y, 0);

            var tileSprite = checkedTilemap.GetSprite(cellPos);

            if (tileSprite) {
               foreach (var border in preset.layers[i].borders) {
                  switch (border.borderType) {
                     // Four directions
                     case BorderType.allDirections:
                        //                                                     right   left   up   down
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, false, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     // Three directions
                     case BorderType.topLateral:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, false, true)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.downLateral:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, true, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.leftTopDown:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, false, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.rightTopDown:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, false, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     // Two directions
                     case BorderType.topDown:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, false, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.Lateral:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, true, true)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.topLeft:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, false, true)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.downLeft:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, true, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.topRight:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, false, true)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.downRight:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, true, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     // One direction
                     case BorderType.top:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, false, true)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.down:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, true, false)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.right:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, true, true)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;

                     case BorderType.left:
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, true, true)) {
                           if (diferentLayerBorder) {
                              setTilemap.SetTile(cellPos, null);
                           } else {
                              setTilemap.SetTile(cellPos, border.BorderTile);
                           }
                        }
                        break;
                  }
               }
            }
         }
      }
   }
   /// <summary>
   /// check if exist tiles around the tilemap
   /// </summary>
   /// <param name="checkedTilemap">tilemap to check</param>
   /// <param name="x">x position</param>
   /// <param name="y">y position</param>
   /// <param name="right">tile on the right</param>
   /// <param name="left">tile on the left</param>
   /// <param name="up">tile above</param>
   /// <param name="down">tile below</param>
   /// <returns></returns>
   private static bool CheckExistingBorderDirections (Tilemap checkedTilemap, int x, int y, bool right, bool left, bool up, bool down) {
      if (CheckExistingTile(checkedTilemap, x + 1, y) == right && CheckExistingTile(checkedTilemap, x - 1, y) == left && CheckExistingTile(checkedTilemap, x, y + 1) == up && CheckExistingTile(checkedTilemap, x, y - 1) == down) {
         return true;
      } else {
         return false;
      }
   }
   /// <summary>
   /// check if paramref name="tilemap" exist
   /// </summary>
   /// <param name="tilemap">tilemap to check</param>
   /// <param name="x">x position</param>
   /// <param name="y">y position</param>
   /// <returns></returns>
   private static bool CheckExistingTile (Tilemap tilemap, int x, int y) {
      if (tilemap.GetSprite(new Vector3Int(x, y, 0))) {
         return true;
      } else {
         return false;
      }
   }

   /// <summary>
   /// automatically calculate the layer height
   /// </summary>
   /// <param name="layerLenght">number of layers</param>
   /// <returns></returns>
   private static float[] CalculateLayerHeight (int layerLenght) {
      List<float> layersHeight = null;

      layersHeight = new List<float>();

      float maxHeight = float.MinValue;
      float minHeight = float.MaxValue;

      for (int i = 0; i < layerLenght; i++) {
         layersHeight.Add(i + 1f);
         if (layersHeight[i] > maxHeight) {
            maxHeight = layersHeight[i];
         }
         if (layersHeight[i] < minHeight) {
            minHeight = layersHeight[i];
         }
      }

      for (int i = 0; i < layerLenght; i++) {
         layersHeight[i] = Mathf.InverseLerp(minHeight, maxHeight, layersHeight[i]);
      }

      layersHeight.Reverse();
      return layersHeight.ToArray();

   }

   /// <summary>
   /// generate the noise map
   /// </summary>
   /// <param name="mapSize">size</param>
   /// <param name="seed"></param>
   /// <param name="scale"></param>
   /// <param name="octaves"></param>
   /// <param name="persistance"></param>
   /// <param name="lacunarity"></param>
   /// <param name="offset"></param>
   /// <returns></returns>
   private static float[,] GenerateNoiseMap (Vector2Int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
      float[,] noiseMap = new float[mapSize.x, mapSize.y];

      System.Random pseudoRandom = new System.Random(seed);
      Vector2[] octaveOffsets = new Vector2[octaves];

      for (int i = 0; i < octaves; i++) {
         float offsetX = pseudoRandom.Next(-100000, 100000) + offset.x;
         float offsetY = pseudoRandom.Next(-100000, 100000) + offset.y;

         octaveOffsets[i] = new Vector2(offsetX, offsetY);
      }

      if (scale <= 0) {
         scale = .0001f;
      }

      float maxNoiseHeight = float.MinValue;
      float minNoiseHeight = float.MaxValue;

      float halfWidth = mapSize.x / 2;
      float halfHeight = mapSize.y / 2;

      for (int y = 0; y < mapSize.y; y++) {
         for (int x = 0; x < mapSize.x; x++) {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int i = 0; i < octaves; i++) {
               float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
               float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
               noiseHeight += perlinValue * amplitude;

               amplitude *= persistance;
               frequency *= lacunarity;
            }

            if (noiseHeight > maxNoiseHeight) {
               maxNoiseHeight = noiseHeight;
            } else if (noiseHeight < minNoiseHeight) {
               minNoiseHeight = noiseHeight;
            }

            noiseMap[x, y] = noiseHeight;
         }
      }

      // normalize
      for (int y = 0; y < mapSize.y; y++) {
         for (int x = 0; x < mapSize.x; x++) {
            noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
         }
      }

      return noiseMap;
   }

   #region Private Variables

   #endregion
}
