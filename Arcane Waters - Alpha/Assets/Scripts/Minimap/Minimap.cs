using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MinimapGeneration;
using UnityEngine.Tilemaps;
using System;
using Random = UnityEngine.Random;

public class Minimap : ClientMonoBehaviour {
   #region Public Variables

   // The distance scale the minimap is using
   public static float SCALE = .10f;

   // The prefab we use for creating NPC icons
   public MM_Icon npcIconPrefab;

   // The prefab we use for creating Building icons
   public MM_Icon buildingIconPrefab;

   // The prefab we use for showing Tutorial objectives
   public MM_TutorialIcon tutorialIconPrefab;

   // The prefab we use for showing impassable areas
   public MM_Icon impassableIconPrefab;

   // The icon to use for NPCs
   public Sprite npcIcon;

   // The Container for the icons we create
   public GameObject iconContainer;

   // Image we're using for the map background
   public Image backgroundImage;

   // Sprites used in sea random map generation
   public Sprite enemySprite;
   public Sprite treasureSiteSprite;

   // Minimap generator presets (scriptable objects)
   public MinimapGeneratorPreset desertPreset;
   public MinimapGeneratorPreset pinePreset;
   public MinimapGeneratorPreset snowPreset;
   public MinimapGeneratorPreset lavaPreset;
   public MinimapGeneratorPreset forestPreset;
   public MinimapGeneratorPreset mushroomPreset;

   // Self
   public static Minimap self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   protected void Start () {
      // Look up components
      _rect = GetComponent<RectTransform>();
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   void Update () {
      // Hide the minimap if there's no player
      _canvasGroup.alpha = (Global.player == null || Global.isInBattle()) ? 0f : 1f;

      // Don't do anything else if there's no player set right now
      if (Global.player == null) {
         return;
      }

      // If our area changes, update the markers we've created
      if (Global.player.areaType != _previousAreaType) {
         updateMinimapForNewArea();
      }
   }

   public float getMaxDistance () {
      return _rect.sizeDelta.x / 2f * SCALE;
   }

   protected void updateMinimapForNewArea () {
      Area area = AreaManager.self.getArea(Global.player.areaType);

      // For random sea map - create new minimap
      if (Area.isRandom(Global.player.areaType)) {
         CreateSeaRandomMinimap(Global.player.areaType);
      } else {
         // Change the background image
         backgroundImage.sprite = ImageManager.getSprite("Minimaps/" + area.areaType);

         // If we didn't find a background image, just use a black background
         if (backgroundImage.sprite == null) {
            backgroundImage.sprite = ImageManager.getSprite("Minimaps/Black");
         }
      }

      // Delete any old markers we created
      iconContainer.DestroyChildren();

      // Create icons for any impassable areas
      /*for (float y = area.cameraBounds.bounds.min.y; y < area.cameraBounds.bounds.max.y; y += (4f * SCALE)) {
         for (float x = area.cameraBounds.bounds.min.x; x < area.cameraBounds.bounds.max.x; x += (4f * SCALE)) {
            bool impassable = false;
            Vector2 pos = new Vector2(x, y);
            
            foreach (Collider2D hit in Physics2D.OverlapAreaAll(pos, pos + new Vector2(2f*SCALE, 2f*SCALE))) {
               if (!hit.isTrigger) {
                  impassable = true;
               }
            }
            
            if (impassable) {
               MM_Icon icon = Instantiate(impassableIconPrefab, this.iconContainer.transform);
               icon.transform.position = pos;
               icon.targetPosition = pos;
            }
         }
      }*/

      // Create new icons for all NPCs
      foreach (NPC npc in area.GetComponentsInChildren<NPC>()) {
         MM_Icon icon = Instantiate(npcIconPrefab, this.iconContainer.transform);
         icon.target = npc.gameObject;
      }

      // Create new icons for all buildings
      List<string> buildings = new List<string>() { "Shipyard", "Merchant", "Weapons" };
      foreach (Spawn spawn in area.GetComponentsInChildren<Spawn>()) {
         foreach (string building in buildings) {
            if (spawn.spawnType.ToString().Contains(building)) {
               MM_Icon icon = Instantiate(buildingIconPrefab, this.iconContainer.transform);
               icon.target = spawn.gameObject;
               icon.tooltip.text = building;
            }
         }
      }

      // Create icons for all tutorial objectives
      foreach (TutorialLocation loc in area.GetComponentsInChildren<TutorialLocation>()) {
         MM_TutorialIcon icon = Instantiate(tutorialIconPrefab, this.iconContainer.transform);
         icon.tutorialStepType = loc.tutorialStepType;
         icon.target = loc.gameObject;
      }

      // Note the new area type
      _previousAreaType = Global.player.areaType;
   }

   private void CreateSeaRandomMinimap (Area.Type areaType) {
      TilemapToTextureColors(areaType);
   }

   void TilemapToTextureColors (Area.Type areaType) {
      List<Texture2D> textureList = new List<Texture2D>();
      // Get the Area associated with the Map
      GameObject area = AreaManager.self.getArea(areaType).gameObject;

      //The layer will set the base image size
      int layerSizeX = 0;
      int layerSizeY = 0;

      foreach (var tilemap in area.GetComponentsInChildren<Tilemap>(true)) {
         if (tilemap.size.x > layerSizeX) {
            layerSizeX = tilemap.size.x;
         }
         if (tilemap.size.y > layerSizeY) {
            layerSizeY = tilemap.size.y;
         }
      }

      if (area) {
         MinimapGeneratorPreset preset = ChoosePreset(area.GetComponent<Area>());
         if (preset) {
            _tileLayer = preset._tileLayer;
            _tileIconLayers = preset._tileIconLayers;
            _textureSize = preset._textureSize;

            // Locate the tilemaps within the area
            foreach (Tilemap tilemap in area.GetComponentsInChildren<Tilemap>(true)) {
               foreach (var layer in _tileLayer) {
                  // Create a variable texture we can write to
                  Texture2D map = null;

                  bool flipflop = true;

                  if (tilemap.name.EndsWith(layer.Name)) {
                     map = new Texture2D(layerSizeX, layerSizeY);
                     MakeTextureTransparent(map);

                     // Cycle over all the Tile positions in this Tilemap layer
                     for (int y = 0; y <= layerSizeY; y++) {
                        for (int x = 0; x <= layerSizeX; x++) {
                           // Check which Tile is at the cell position
                           Vector3Int cellPos = new Vector3Int(x, y, 0);

                           var tileSprite = tilemap.GetSprite(cellPos);

                           if (tileSprite) {
                              //checks if are using a sprite to compare
                              if (!layer.isSubLayer) {
                                 //set base color
                                 map.SetPixel(x, y, layer.color);

                                 //set random color
                                 if (layer.useRandomColor) {
                                    if (Random.Range(0, 2) == 0) {
                                       map.SetPixel(x, layerSizeY - y, layer.color);
                                    } else {
                                       map.SetPixel(x, layerSizeY - y, layer.randomColor);
                                    }
                                 }

                                 //set alternating color
                                 if (layer.useVerticalAlternatingColor) {
                                    if (flipflop) {
                                       map.SetPixel(x, layerSizeY - y, layer.color);
                                       flipflop = false;
                                    } else {
                                       map.SetPixel(x, layerSizeY - y, layer.verticalAlternatingColor);
                                       flipflop = true;
                                    }
                                 }
                              } else {
                                 //sublayer with name and sprite to compare
                                 if (layer.subLayerSpriteSuffixNames.Length > 0 && layer.sprites.Length > 0) {
                                    foreach (var subLayerSpriteSuffixName in layer.subLayerSpriteSuffixNames) {
                                       foreach (var sprites in layer.sprites) {
                                          if (tileSprite == sprites) {
                                             if (subLayerSpriteSuffixName != "") {
                                                if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                                   continue;
                                                }
                                             }
                                             //set base color
                                             map.SetPixel(x, layerSizeY - y, layer.color);

                                             //set random color
                                             if (layer.useRandomColor) {
                                                if (Random.Range(0, 2) == 0) {
                                                   map.SetPixel(x, layerSizeY - y, layer.color);
                                                } else {
                                                   map.SetPixel(x, layerSizeY - y, layer.randomColor);
                                                }
                                             }

                                             //set alternating color
                                             if (layer.useVerticalAlternatingColor) {
                                                if (flipflop) {
                                                   map.SetPixel(x, layerSizeY - y, layer.color);
                                                   flipflop = false;
                                                } else {
                                                   map.SetPixel(x, layerSizeY - y, layer.verticalAlternatingColor);
                                                   flipflop = true;
                                                }
                                             }
                                          }
                                       }
                                    }
                                 }
                                 //sublayer with sprite to compare
                                 else if (layer.sprites.Length > 0) {
                                    foreach (var sprites in layer.sprites) {
                                       if (tileSprite == sprites) {
                                          //set base color
                                          map.SetPixel(x, layerSizeY - y, layer.color);

                                          //set random color
                                          if (layer.useRandomColor) {
                                             if (Random.Range(0, 2) == 0) {
                                                map.SetPixel(x, layerSizeY - y, layer.color);
                                             } else {
                                                map.SetPixel(x, layerSizeY - y, layer.randomColor);
                                             }
                                          }

                                          //set alternating color
                                          if (layer.useVerticalAlternatingColor) {
                                             if (flipflop) {
                                                map.SetPixel(x, layerSizeY - y, layer.color);
                                                flipflop = false;
                                             } else {
                                                map.SetPixel(x, layerSizeY - y, layer.verticalAlternatingColor);
                                                flipflop = true;
                                             }
                                          }
                                       }
                                    }
                                 }
                                 //sublayer with name to compare
                                 else if (layer.subLayerSpriteSuffixNames.Length > 0) {
                                    foreach (var subLayerSpriteSuffixName in layer.subLayerSpriteSuffixNames) {
                                       if (subLayerSpriteSuffixName != "") {
                                          if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                             continue;
                                          }
                                       }
                                       //set base color
                                       map.SetPixel(x, layerSizeY - y, layer.color);

                                       //set random color
                                       if (layer.useRandomColor) {
                                          if (Random.Range(0, 2) == 0) {
                                             map.SetPixel(x, layerSizeY - y, layer.color);
                                          } else {
                                             map.SetPixel(x, layerSizeY - y, layer.randomColor);
                                          }
                                       }

                                       //set alternating color
                                       if (layer.useVerticalAlternatingColor) {
                                          if (flipflop) {
                                             map.SetPixel(x, layerSizeY - y, layer.color);
                                             flipflop = false;
                                          } else {
                                             map.SetPixel(x, layerSizeY - y, layer.verticalAlternatingColor);
                                             flipflop = true;
                                          }
                                       }
                                    }
                                 }

                              }
                           }
                        }
                     }

                     if (layer.useHorizontalAlternatingColor) {
                        // Cycle over all the Tile positions in this Tilemap layer
                        for (int x = 0; x <= layerSizeX; x++) {
                           flipflop = true;
                           for (int y = 0; y <= layerSizeY; y++) {
                              // Check which Tile is at the cell position
                              Vector3Int cellPos = new Vector3Int(x, -y, 0);

                              var tileSprite = tilemap.GetSprite(cellPos);

                              if (tileSprite) {
                                 //checks if are using a sprite to compare
                                 if (!layer.isSubLayer) {
                                    if (flipflop) {
                                       map.SetPixel(x, layerSizeY - y, layer.color);
                                       flipflop = false;
                                    } else {
                                       map.SetPixel(x, layerSizeY - y, layer.horizontalAlternatingColor);
                                       flipflop = true;
                                    }

                                 } else {
                                    //sublayer with name and sprite to compare
                                    if (layer.subLayerSpriteSuffixNames.Length > 0 && layer.sprites.Length > 0) {
                                       foreach (var subLayerSpriteSuffixName in layer.subLayerSpriteSuffixNames) {
                                          foreach (var sprites in layer.sprites) {
                                             if (tileSprite == sprites) {
                                                if (subLayerSpriteSuffixName != "") {
                                                   if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                                      continue;
                                                   }
                                                }

                                                if (flipflop) {
                                                   map.SetPixel(x, layerSizeY - y, layer.color);
                                                   flipflop = false;
                                                } else {
                                                   map.SetPixel(x, layerSizeY - y, layer.horizontalAlternatingColor);
                                                   flipflop = true;
                                                }
                                             }
                                          }
                                       }
                                    }
                                    //sublayer with sprite to compare
                                    else if (layer.sprites.Length > 0) {
                                       foreach (var sprites in layer.sprites) {
                                          if (tileSprite == sprites) {

                                             if (flipflop) {
                                                map.SetPixel(x, layerSizeY - y, layer.color);
                                                flipflop = false;
                                             } else {
                                                map.SetPixel(x, layerSizeY - y, layer.horizontalAlternatingColor);
                                                flipflop = true;
                                             }
                                          }
                                       }
                                    }
                                    //sublayer with name to compare
                                    else if (layer.subLayerSpriteSuffixNames.Length > 0) {
                                       foreach (var subLayerSpriteSuffixName in layer.subLayerSpriteSuffixNames) {
                                          if (subLayerSpriteSuffixName != "") {
                                             if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                                continue;
                                             }
                                          }

                                          if (flipflop) {
                                             map.SetPixel(x, layerSizeY - y, layer.color);
                                             flipflop = false;
                                          } else {
                                             map.SetPixel(x, layerSizeY - y, layer.horizontalAlternatingColor);
                                             flipflop = true;
                                          }
                                       }
                                    }

                                 }
                              }
                           }
                        }
                     }

                     if (layer.useAlternatingColor) {
                        // Cycle over all the Tile positions in this Tilemap layer
                        for (int x = 0; x <= layerSizeX; x++) {
                           for (int y = 0; y <= layerSizeY; y++) {
                              // Check which Tile is at the cell position
                              Vector3Int cellPos = new Vector3Int(x, -y, 0);

                              var tileSprite = tilemap.GetSprite(cellPos);

                              if (tileSprite) {
                                 //checks if are using a sprite to compare
                                 if (!layer.isSubLayer) {

                                    if (flipflop) {
                                       map.SetPixel(x, layerSizeY - y, layer.color);
                                       flipflop = false;
                                    } else {
                                       map.SetPixel(x, layerSizeY - y, layer.alternatingColor);
                                       flipflop = true;
                                    }

                                 } else {
                                    //sublayer with name and sprite to compare
                                    if (layer.subLayerSpriteSuffixNames.Length > 0 && layer.sprites.Length > 0) {
                                       foreach (var subLayerSpriteSuffixName in layer.subLayerSpriteSuffixNames) {
                                          foreach (var sprites in layer.sprites) {
                                             if (tileSprite == sprites) {
                                                if (subLayerSpriteSuffixName != "") {
                                                   if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                                      continue;
                                                   }
                                                }

                                                if (flipflop) {
                                                   map.SetPixel(x, layerSizeY - y, layer.color);
                                                   flipflop = false;
                                                } else {
                                                   map.SetPixel(x, layerSizeY - y, layer.alternatingColor);
                                                   flipflop = true;
                                                }
                                             }

                                          }
                                       }
                                    }
                                    //sublayer with sprite to compare
                                    else if (layer.sprites.Length > 0) {
                                       foreach (var sprites in layer.sprites) {
                                          if (tileSprite == sprites) {
                                             if (flipflop) {
                                                map.SetPixel(x, layerSizeY - y, layer.color);
                                                flipflop = false;
                                             } else {
                                                map.SetPixel(x, layerSizeY - y, layer.alternatingColor);
                                                flipflop = true;
                                             }
                                          }

                                       }
                                    }
                                    //sublayer with name to compare
                                    else if (layer.subLayerSpriteSuffixNames.Length > 0) {
                                       foreach (var subLayerSpriteSuffixName in layer.subLayerSpriteSuffixNames) {
                                          if (subLayerSpriteSuffixName != "") {
                                             if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                                continue;
                                             }
                                          }

                                          if (flipflop) {
                                             map.SetPixel(x, layerSizeY - y, layer.color);
                                             flipflop = false;
                                          } else {
                                             map.SetPixel(x, layerSizeY - y, layer.alternatingColor);
                                             flipflop = true;
                                          }
                                       }
                                    }

                                 }
                              }
                           }
                        }
                     }

                     if (map) {
                        map.Apply();
                     }

                     if (layer.useTopBorder || layer.useBorder || layer.useTopDownBorder || layer.useLateralBorder || layer.useDownBorder) {
                        for (int y = 0; y < map.height; y++) {
                           for (int x = 0; x < map.width; x++) {
                              if (map.GetPixel(x, y).a > 0) {
                                 // TODO: verify if work in all maps
                                 //set border color
                                 if (layer.useBorder) {
                                    if (map.GetPixel(x + 1, y).a <= 0 || map.GetPixel(x - 1, y).a <= 0 || map.GetPixel(x, y + 1).a <= 0 || map.GetPixel(x, y - 1).a <= 0) {
                                       map.SetPixel(x, y, layer.borderColor);
                                    }
                                 }

                                 //set top down color
                                 if (layer.useTopDownBorder) {
                                    if (map.GetPixel(x, y + 1).a <= 0 || map.GetPixel(x, y - 1).a <= 0) {
                                       map.SetPixel(x, y, layer.topDownBorderColor);
                                    }
                                 }

                                 //set lateral border color
                                 if (layer.useLateralBorder) {
                                    if (map.GetPixel(x + 1, y).a <= 0 || map.GetPixel(x - 1, y).a <= 0) {
                                       map.SetPixel(x, y, layer.lateralColor);
                                    }
                                 }

                                 //set top border color
                                 if (layer.useTopBorder) {
                                    if (map.GetPixel(x, y + 1).a <= 0) {
                                       map.SetPixel(x, y, layer.topBorderColor);
                                    }
                                 }

                                 //set down border color
                                 if (layer.useDownBorder) {
                                    if (map.GetPixel(x, y - 1).a <= 0 && y - 1 >= 0) {
                                       map.SetPixel(x, y, layer.downBorderColor);
                                    }
                                 }

                              }
                           }
                        }
                     }

                     //create outline
                     if (layer.useOutline) {
                        Texture2D tempMap = new Texture2D(map.width, map.height);
                        MakeTextureTransparent(tempMap);
                        for (int y = 0; y < map.height; y++) {
                           for (int x = 0; x < map.width; x++) {
                              tempMap.SetPixel(x, y, map.GetPixel(x, y));
                           }
                        }
                        tempMap.Apply();

                        //set object size
                        for (int y = 0; y <= layerSizeY; y++) {
                           for (int x = 0; x <= layerSizeX; x++) {
                              Vector3Int cellPos = new Vector3Int(x, -y, 0);

                              var tileSprite = tilemap.GetSprite(cellPos);
                              if (tileSprite) {
                                 //set outline color
                                 if (map.GetPixel(x, (layerSizeY - y)).a > 0) {
                                    map.SetPixel(x, (layerSizeY - y) + 1, layer.outlineColor);
                                    map.SetPixel(x, (layerSizeY - y) - 1, layer.outlineColor);
                                    map.SetPixel(x + 1, (layerSizeY - y), layer.outlineColor);
                                    map.SetPixel(x - 1, (layerSizeY - y), layer.outlineColor);
                                 }
                              }
                           }
                        }
                        for (int y = 0; y < map.height; y++) {
                           for (int x = 0; x < map.width; x++) {
                              var pixel = tempMap.GetPixel(x, y);
                              if (tempMap.GetPixel(x, y).a > 0) {
                                 map.SetPixel(x, y, pixel);
                              }
                           }
                        }
                        map.Apply();
                     }

                  }

                  if (map != null) {
                     map.Apply();
                     textureList.Add(map);
                  }
               }
            }
         }

         //Grid gridLayer = area.GetComponentInChildren<Grid>();
         //Texture2D treasureSiteTexture = new Texture2D(layerSizeX, layerSizeY);
         //MakeTextureTransparent(treasureSiteTexture);
         //for (int i = 0; i < gridLayer.transform.childCount; i++) {
         //   if (gridLayer.transform.GetChild(i).name == "Treasure Site(Clone)") {
         //      //treasureSiteTexture.SetPixel(gridLayer.transform.GetChild(i).localPosition.x, gridLayer.transform.GetChild(i).localPosition.y);
         //      var sprite = treasureSiteSprite;

         //      if (sprite) {
         //         var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
         //             (int) sprite.textureRect.y,
         //             (int) sprite.textureRect.width,
         //             (int) sprite.textureRect.height);

         //         treasureSiteTexture.SetPixels((int)gridLayer.transform.GetChild(i).localPosition.x, (int)gridLayer.transform.GetChild(i).localPosition.y, (int) sprite.rect.width, (int) sprite.rect.height, pixels);
         //      }

         //   }
         //}
         //treasureSiteTexture.Apply();
         //textureList.Add(treasureSiteTexture);

         for (int x = 0; x < layerSizeX; x++) {
            for (int y = 0; y < layerSizeY; y++) {

            }
         }

         if (textureList.Count > 0 && textureList[0]) {
            Texture2D tex = MergeTexturesMinimap(textureList);

            if (preset.useOutline) {
               tex = OutlineTexture(tex, preset.outlineColor);
            }
            ExportTexture(tex);
         }
         textureList.Clear();
      }
   }

   Texture2D MergeTexturesMinimap (List<Texture2D> list) {
      Texture2D tex = new Texture2D(64, 64);
      for (int x = 0; x < 64; x++) {
         for (int y = 0; y < 64; y++) {
            for (int i = 0; i < list.Count; i++) {
               if (list[i].GetPixel(x, y).a == 1.0f) {
                  tex.SetPixel(x, y, list[i].GetPixel(x, y));
               }
            }
         }
      }
      tex.Apply();
      return tex;
   }

   private MinimapGeneratorPreset ChoosePreset (Area area) {
      Biome.Type biomeType = InstanceManager.self.getOpenInstance(area.areaType).biomeType;
      switch (biomeType) {
         case Biome.Type.Forest:
            return forestPreset;
         case Biome.Type.Desert:
            return desertPreset;
         case Biome.Type.Pine:
            return pinePreset;
         case Biome.Type.Snow:
            return snowPreset;
         case Biome.Type.Lava:
            return lavaPreset;
         case Biome.Type.Mushroom:
            return mushroomPreset;
      }
      D.error("Couldn't match biome type to given area!");
      return null;
   }

   Texture2D OutlineTexture (Texture2D texture, Color outlineColor) {
      Texture2D tempMap = new Texture2D(texture.width, texture.height);
      MakeTextureTransparent(tempMap);
      for (int y = 0; y < texture.height; y++) {
         for (int x = 0; x < texture.width; x++) {
            tempMap.SetPixel(x, y, texture.GetPixel(x, y));
         }
      }
      tempMap.Apply();

      bool[,] tileBools = new bool[texture.width, texture.height];

      //set object size
      for (int x = 0; x < texture.width; x++) {
         for (int y = 0; y < texture.height; y++) {
            if (texture.GetPixel(x, y).a > 0) {
               tileBools[x, y] = true;
            }
         }
      }
      for (int x = 0; x < texture.width; x++) {
         for (int y = 0; y < texture.height; y++) {
            if (tileBools[x, y]) {
               texture.SetPixel(x, y + 1, outlineColor);
               texture.SetPixel(x, y - 1, outlineColor);
               texture.SetPixel(x + 1, y, outlineColor);
               texture.SetPixel(x - 1, y, outlineColor);

               texture.SetPixel(x + 1, y + 1, outlineColor);
               texture.SetPixel(x - 1, y - 1, outlineColor);
               texture.SetPixel(x - 1, y + 1, outlineColor);
               texture.SetPixel(x + 1, y - 1, outlineColor);
            }
         }
      }

      for (int y = 0; y < texture.height; y++) {
         for (int x = 0; x < texture.width; x++) {
            var pixel = tempMap.GetPixel(x, y);
            if (tempMap.GetPixel(x, y).a > 0) {
               texture.SetPixel(x, y, pixel);
            }
         }
      }
      texture.Apply();

      return texture;
   }

   void MakeTextureTransparent (Texture2D texture) {
      for (int x = 0; x < texture.width; x++) {
         for (int y = 0; y < texture.height; y++) {
            texture.SetPixel(x, y, new Color(0, 0, 0, 0));
         }
      }
   }

   void ExportTexture (Texture2D texture, string fileName = "texture") {
      _seaRandomSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.0f, 0.0f));
      backgroundImage.sprite = _seaRandomSprite;
   }

   Color AlphaBlend (Color destination, Color source) {
      float sourceF = source.a;
      float destinationF = 1f - source.a;
      float alpha = sourceF + destinationF * destination.a;
      Color resultColor = (source * sourceF + destination * destination.a * destinationF) / alpha;
      resultColor.a = alpha;
      return resultColor;
   }

   #region Private Variables

   // Create random sea map sprite
   private Sprite _seaRandomSprite;

   // Our Rect Transform
   protected RectTransform _rect;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // The previous area we were in
   protected Area.Type _previousAreaType;

#pragma warning disable 0649
   [SerializeField] MinimapGeneratorPreset[] _presets;
#pragma warning restore 0649

   [SerializeField] TileLayer[] _tileLayer = new TileLayer[0];
   [SerializeField] TileIcon[] _tileIconLayers = new TileIcon[0];
   [SerializeField] Vector2Int _textureSize = new Vector2Int(512, 512);

   #endregion
}
