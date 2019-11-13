using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MinimapGeneration;
using UnityEngine.Tilemaps;
using System;
using System.IO;
using UnityEditor;
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

   // The prefab we use for creating treasure chest icon
   public MM_Icon treasureChestIconPrefab;

   // The prefab we use for showing player ship entity (enemy, friendly, neutral)
   public MM_ShipEntityIcon enemyShipIconPrefab;
   public MM_ShipEntityIcon friendlyShipIconPrefab;
   public MM_ShipEntityIcon neutralShipIconPrefab;

   // The prefab we use for showing bot ship entity (enemy, friendly, neutral)
   public MM_ShipEntityIcon enemyBotShipIconPrefab;
   public MM_ShipEntityIcon friendlyBotShipIconPrefab;
   public MM_ShipEntityIcon neutralBotShipIconPrefab;

   // The icon to use for NPCs
   public Sprite npcIcon;

   // The Container for the icons we create
   public GameObject iconContainer;

   // The Container for the player ship icons we create
   public GameObject playerShipIconContainer;

   // The Container for the bot ship icons we create
   public GameObject botShipIconContainer;

   // The icons of treasure sites on minimap
   public Image[] treasureSiteImages;

   // The icons of sea monster entities
   public MM_SeaMonsterIcon[] seaMonsterIcons;

   // Image we're using for the map background
   public Image backgroundImage;

   // Minimap generator presets (scriptable objects) for sea random maps
   [Header("Random sea prefabs")]
   public MinimapGeneratorPreset seaDesertPreset;
   public MinimapGeneratorPreset seaPinePreset;
   public MinimapGeneratorPreset seaSnowPreset;
   public MinimapGeneratorPreset seaLavaPreset;
   public MinimapGeneratorPreset seaForestPreset;
   public MinimapGeneratorPreset seaMushroomPreset;

   // Minimap generator presets (scriptable objects) for static maps
   [Header("Static map prefabs")]
   public MinimapGeneratorPreset baseDesertPreset;
   public MinimapGeneratorPreset basePinePreset;
   public MinimapGeneratorPreset baseSnowPreset;
   public MinimapGeneratorPreset baseLavaPreset;
   public MinimapGeneratorPreset baseForestPreset;
   public MinimapGeneratorPreset baseMushroomPreset;

   // Self
   public static Minimap self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   static void createStaticMinimaps () {
      GameObject.FindObjectOfType<Minimap>().generateAllStaticMinimaps();
   }

   protected void Start () {
      // Look up components
      _rect = GetComponent<RectTransform>();
      _canvasGroup = GetComponent<CanvasGroup>();

      // Refresh ship icons with intervals
      InvokeRepeating("refreshShipIcons", 0.0f, 2.0f);
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

   public void updateMinimapForNewArea () {
      if (Global.player == null) {
         return;
      }
      Area area = AreaManager.self.getArea(Global.player.areaType);

      // For random sea map - create new minimap
      if (Area.isRandom(Global.player.areaType)) {
         CreateSeaRandomMinimap(Global.player.areaType);
      } else {
         // Hide random sea map informations
         HideTreasureSites();
         HideSeaMonsters();

         // Dynamically generate minimap for base map player entered
         if (Area.getBiome(area.areaType) != Biome.Type.None) {
            TilemapToTextureColorsStatic(area, false);
         } else {
            // Change the background image
            backgroundImage.sprite = ImageManager.getSprite("Minimaps/" + area.areaType);
         }

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
               icon.getImage().sprite = getBuildingSprite(building);
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

   private Sprite getBuildingSprite(string buildingName) {
      switch (buildingName) {
         case "Shipyard":
            return ImageManager.getSprite(_shopShipyardIconPath);
         case "Merchant":
            return ImageManager.getSprite(_shopTraderIconPath);
         case "Weapons":
            return ImageManager.getSprite(_shopWeaponsIconPath);
         default:
            return null;
      }
   }

   private void CreateSeaRandomMinimap (Area.Type areaType) {
      TilemapToTextureColors(areaType);

      showTreasureSites(areaType);

      showSeaMonsters(areaType);
   }

   void showTreasureSites (Area.Type areaType) {
      GameObject area = AreaManager.self.getArea(areaType).gameObject;
      Grid gridLayer = area.GetComponentInChildren<Grid>();

      int treasureSiteCounter = 0;
      for (int i = 0; i < gridLayer.transform.childCount; i++) {
         if (gridLayer.transform.GetChild(i).name == "Treasure Site(Clone)") {
            if (treasureSiteImages.Length <= treasureSiteCounter) {
               D.error("Not enough treasure site images prepared for minimap presentation");
               return;
            }
            Vector3 relativePosition = gridLayer.transform.GetChild(i).position - area.transform.position;
            relativePosition *= 12f;
            relativePosition += new Vector3(-128f, 0f);
            Util.setLocalXY(treasureSiteImages[treasureSiteCounter].transform, relativePosition);
            treasureSiteCounter++;
         }
      }

      // Disable all treasure sites
      for (int i = 0; i < treasureSiteImages.Length; i++) {
         treasureSiteImages[i].gameObject.SetActive(false);
      }

      // Activate treasure sites that are being used in that map
      for (int i = 0; i < treasureSiteCounter; i++) {
         treasureSiteImages[i].gameObject.SetActive(true);
      }
   }

   void showSeaMonsters (Area.Type areaType) {
      HideSeaMonsters();
      Area area = AreaManager.self.getArea(areaType);

      // Find monsters in chosen area
      int counter = 0;
      SeaMonsterEntity[] seaMonsters = GameObject.FindObjectsOfType<SeaMonsterEntity>();
      foreach (SeaMonsterEntity monster in seaMonsters) {
         if (monster.areaType == areaType) {
            if (seaMonsterIcons.Length <= counter) {
               D.error("Not enough sea monster icons prepared for minimap presentation");
               return;
            }
            // Set up data for monster icon
            seaMonsterIcons[counter].currentArea = area;
            seaMonsterIcons[counter].seaMonster = monster;
            seaMonsterIcons[counter].gameObject.SetActive(true);
            counter++;
         }
      }
   }

   void HideTreasureSites () {
      // Disable all treasure sites
      foreach (Image image in treasureSiteImages) {
         image.gameObject.SetActive(false);
      }
   }

   void HideSeaMonsters () {
      // Disable all sea monsters
      foreach (MM_SeaMonsterIcon icon in seaMonsterIcons) {
         icon.gameObject.SetActive(false);
      }
   }

   public void deleteTreasureChestIcon (GameObject chestObject) {
      if (_treasureChestIcons.Find(icon => icon.target == chestObject) != null) {
         _treasureChestIcons.Find(icon => icon.target == chestObject).gameObject.SetActive(false);
      } else {
         Debug.LogError("Tresure Chest Icon is NULL!");
      }
   }

   public void addTreasureChestIcon (GameObject chestObject) {
      if (_treasureChestIcons.Find(iconItem => iconItem.target == chestObject) == null) {
         MM_Icon icon = Instantiate(treasureChestIconPrefab, this.iconContainer.transform);
         icon.target = chestObject;
         _treasureChestIcons.Add(icon);
      }
   }

   private void intializeTreasureChestIcons () {
      _treasureChestIcons.Clear();
      TreasureChest[] chestsArray = GameObject.FindObjectsOfType<TreasureChest>();
      foreach (TreasureChest chest in chestsArray) {
         if (!chest.hasBeenOpened()) {
            MM_Icon icon = Instantiate(treasureChestIconPrefab, this.iconContainer.transform);
            icon.target = chest.gameObject;
            _treasureChestIcons.Add(icon);
         }
      }
   }

   public void refreshShipIcons () {
      initializeShipEntities();
      initializeShipBotEntities();
   }

   private void initializeShipEntities () {
      if (Global.player == null) {
         return;
      }

      MM_ShipEntityIcon[] shipIcons = this.playerShipIconContainer.transform.GetComponentsInChildren<MM_ShipEntityIcon>();
      Area area = AreaManager.self.getArea(Global.player.areaType);
      PlayerShipEntity[] shipsArray = GameObject.FindObjectsOfType<PlayerShipEntity>();
      foreach (PlayerShipEntity ship in shipsArray) {
         bool stopLoop = false;
         if (ship == Global.player) {
            continue;
         }
         foreach (MM_ShipEntityIcon iconShip in shipIcons) {
            if (iconShip.shipEntity == ship) {
               stopLoop = true;
               break;
            }
         }
         if (stopLoop) {
            continue;
         }

         MM_ShipEntityIcon icon = null;
         // Enemy ship
         if (ship.isEnemyOf(Global.player)) {
            icon = Instantiate(enemyShipIconPrefab, this.playerShipIconContainer.transform);
         }
         // Friendly ship
         else if (ship.faction == Global.player.faction) {
            icon = Instantiate(friendlyShipIconPrefab, this.playerShipIconContainer.transform);
         }
         // Neutral ship
         else {
            icon = Instantiate(neutralShipIconPrefab, this.playerShipIconContainer.transform);
         }
         icon.shipEntity = ship;
         icon.currentArea = area;
      }
   }

   private void initializeShipBotEntities () {
      if (Global.player == null) {
         return;
      }

      MM_ShipEntityIcon[] shipIcons = this.botShipIconContainer.transform.GetComponentsInChildren<MM_ShipEntityIcon>();
      Area area = AreaManager.self.getArea(Global.player.areaType);
      BotShipEntity[] shipsArray = GameObject.FindObjectsOfType<BotShipEntity>();
      foreach (BotShipEntity ship in shipsArray) {
         bool stopLoop = false;
         foreach (MM_ShipEntityIcon iconShip in shipIcons) {
            if (iconShip.shipEntity == ship) {
               stopLoop = true;
               break;
            }
         }
         if (stopLoop) {
            continue;
         }

         MM_ShipEntityIcon icon = null;
         // Enemy ship
         if (ship.isEnemyOf(Global.player)) {
            icon = Instantiate(enemyShipIconPrefab, this.botShipIconContainer.transform);
         }
         // Friendly ship
         else if (ship.faction == Global.player.faction) {
            icon = Instantiate(friendlyShipIconPrefab, this.botShipIconContainer.transform);
         }
         // Neutral ship
         else {
            icon = Instantiate(neutralShipIconPrefab, this.botShipIconContainer.transform);
         }
         icon.shipEntity = ship;
         icon.currentArea = area;
      }
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
         MinimapGeneratorPreset preset = chooseSeaMapPreset(area.GetComponent<Area>());
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

         if (textureList.Count > 0 && textureList[0]) {
            Texture2D tex = MergeTexturesMinimap(textureList);

            if (preset.useOutline) {
               tex = OutlineTexture(tex, preset.outlineColor);
            }
            PresentMap(tex);
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

   private MinimapGeneratorPreset chooseSeaMapPreset (Area area) {
      if (!area || !RandomMapManager.self.mapConfigs.ContainsKey(area.areaType)) {
         D.error("Couldn't get map instance!");
         return seaForestPreset;
      }

      Biome.Type biomeType = RandomMapManager.self.mapConfigs[area.areaType].biomeType;
      return lookUpSeaPreset(biomeType);
   }

   private MinimapGeneratorPreset chooseBaseMapPreset (Area area) {
      if (!area) {
         D.error("Couldn't get map instance!");
         return baseForestPreset;
      }

      Biome.Type biomeType = Area.getBiome(area.areaType);
      
      if (Area.isSea(area.areaType)) {
         return lookUpSeaPreset(biomeType);
      } else {
         switch (biomeType) {
            case Biome.Type.Forest:
               return baseForestPreset;
            case Biome.Type.Desert:
               return baseDesertPreset;
            case Biome.Type.Pine:
               return basePinePreset;
            case Biome.Type.Snow:
               return baseSnowPreset;
            case Biome.Type.Lava:
               return baseLavaPreset;
            case Biome.Type.Mushroom:
               return baseMushroomPreset;
         }
      }

      D.error("Couldn't match biome type to given area!");
      return baseForestPreset;
   }

   private MinimapGeneratorPreset lookUpSeaPreset (Biome.Type biomeType) {
      switch (biomeType) {
         case Biome.Type.Forest:
            return seaForestPreset;
         case Biome.Type.Desert:
            return seaDesertPreset;
         case Biome.Type.Pine:
            return seaPinePreset;
         case Biome.Type.Snow:
            return seaSnowPreset;
         case Biome.Type.Lava:
            return seaLavaPreset;
         case Biome.Type.Mushroom:
            return seaMushroomPreset;
      }
      D.error("Couldn't match biome type to given area!");
      return seaForestPreset;
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

   void PresentMap (Texture2D texture) {
      _seaRandomSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.0f, 0.0f), 100, 1, SpriteMeshType.FullRect);
      _seaRandomSprite.texture.filterMode = FilterMode.Point;
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

   void generateAllStaticMinimaps () {
      #if UNITY_EDITOR
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about our map assets
         if (!assetPath.StartsWith(_mapsPath)) {
            continue;
         }

         // Get the Area associated with the Map
         GameObject area = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
         TilemapToTextureColorsStatic(area.GetComponent<Area>(), true);
      }
      #endif
   }

   void TilemapToTextureColorsStatic (Area area, bool saveMap) {
      List<Texture2D> textureList = new List<Texture2D>();

      //  The layer will set the base image size
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
         AreaEffector2D[] areaEffectors2D = area.GetComponentsInChildren<AreaEffector2D>();
         Collider2D[] colliders2D = area.GetComponentsInChildren<Collider2D>();
         MinimapGeneratorPreset preset = chooseBaseMapPreset(area);
         if (preset) {
            _tileLayer = preset._tileLayer;
            _tileIconLayers = preset._tileIconLayers;
            _textureSize = preset._textureSize;
            _minimapsPath = preset._minimapsPath;

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
                           Vector3Int cellPos = new Vector3Int(x, -y, 0);

                           var tileSprite = tilemap.GetSprite(cellPos);

                           if (tileSprite) {
                              //checks if are using a sprite to compare
                              if (!layer.isSubLayer) {
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

                  //set icon position
                  foreach (var icon in _tileIconLayers) {
                     if (!icon.useAreaEffector2D && !icon.useCollider2D) {

                        bool[,] tileBools = new bool[layerSizeX, layerSizeY];

                        if (tilemap.name.EndsWith(icon.iconLayerName)) {
                           map = new Texture2D(layerSizeX, layerSizeY);
                           MakeTextureTransparent(map);
                           //set chunk size
                           for (int y = 0; y <= layerSizeY; y++) {
                              for (int x = 0; x <= layerSizeX; x++) {
                                 Vector3Int cellPos = new Vector3Int(x, -y, 0);

                                 var tileSprite = tilemap.GetSprite(cellPos);

                                 if (tileSprite) {
                                    if (!icon.isSubLayer) {
                                       //set chunks
                                       tileBools[x, layerSizeY - y] = true;
                                    } else {
                                       //sublayer with name and sprite to compare
                                       if (icon.subLayerSpriteSuffixNames.Length > 0 && icon.subLayerSprites.Length > 0) {
                                          foreach (var subLayerSpriteSuffixName in icon.subLayerSpriteSuffixNames) {
                                             foreach (var subLayerSprite in icon.subLayerSprites) {
                                                if (tileSprite == subLayerSprite) {
                                                   if (!string.IsNullOrWhiteSpace(subLayerSpriteSuffixName)) {
                                                      if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                                         continue;
                                                      }
                                                   }
                                                   //set chunks
                                                   tileBools[x, layerSizeY - y] = true;
                                                }
                                             }
                                          }
                                       }
                                       //sublayer with sprite to compare
                                       else if (icon.subLayerSprites.Length > 0) {
                                          foreach (var subLayerSprite in icon.subLayerSprites) {
                                             if (tileSprite == subLayerSprite) {
                                                //set chunks
                                                tileBools[x, layerSizeY - y] = true;
                                             }
                                          }
                                       }
                                       //sublayer with name to compare
                                       else if (icon.subLayerSpriteSuffixNames.Length > 0) {
                                          foreach (var subLayerSpriteSuffixName in icon.subLayerSpriteSuffixNames) {

                                             if (!string.IsNullOrWhiteSpace(subLayerSpriteSuffixName)) {
                                                if (!tileSprite.name.EndsWith(subLayerSpriteSuffixName)) {
                                                   continue;
                                                }
                                             }
                                             //set chunks
                                             tileBools[x, layerSizeY - y] = true;
                                          }
                                       }

                                    }
                                 }
                              }
                           }

                           bool firstOfTheChunk = false;
                           for (int y = 0; y < map.height; y++) {
                              for (int x = 0; x < map.width; x++) {
                                 if (tileBools[x, y]) {
                                    if ((x + 1 < map.width && x - 1 >= 0 && y + 1 < map.height && y - 1 >= 0)) {
                                       //checks the end of the chunk
                                       if (tileBools[x + 1, y] && !tileBools[x - 1, y] && tileBools[x, y + 1] && !tileBools[x, y - 1]) {
                                          firstOfTheChunk = false;
                                       }
                                    }

                                    if (map.GetPixel(x + 1, y).a <= 0 || map.GetPixel(x - 1, y).a <= 0 || map.GetPixel(x, y + 1).a <= 0 || (map.GetPixel(x, y - 1).a <= 0 && y - 1 >= 0)) {
                                       //set icon position
                                       if (!firstOfTheChunk) {
                                          var sprite = icon.spriteIcon;

                                          if (sprite) {
                                             var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
                                                   (int) sprite.textureRect.y,
                                                   (int) sprite.textureRect.width,
                                                   (int) sprite.textureRect.height);

                                             map.SetPixels(x + icon.offset.x, y + icon.offset.y, (int) sprite.rect.width, (int) sprite.rect.height, pixels);
                                          }

                                          firstOfTheChunk = true;
                                       }
                                    }
                                 }
                              }
                           }
                        }
                     }

                  }

                  if (map != null) {
                     map.Apply();
                     textureList.Add(map);
                  }
               }
            }

            foreach (var icon in _tileIconLayers) {
               if (icon.useAreaEffector2D) {
                  foreach (var areaEffector2D in areaEffectors2D) {
                     if (areaEffector2D.forceAngle == 90) {
                        Texture2D map = new Texture2D(layerSizeX, layerSizeY);
                        MakeTextureTransparent(map);

                        GridLayout gridLayout = area.GetComponentInChildren<GridLayout>();
                        Vector2Int areaEffector2DCellPosition = new Vector2Int(gridLayout.WorldToCell(areaEffector2D.transform.position).x, -gridLayout.WorldToCell(areaEffector2D.transform.position).y);

                        var sprite = icon.spriteIcon;

                        if (sprite) {

                           var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
                                 (int) sprite.textureRect.y,
                                 (int) sprite.textureRect.width,
                                 (int) sprite.textureRect.height);

                           map.SetPixels(areaEffector2DCellPosition.x + icon.offset.x, (layerSizeY - areaEffector2DCellPosition.y) + icon.offset.y, (int) sprite.rect.width, (int) sprite.rect.height, pixels);

                           map.Apply();
                           textureList.Add(map);

                        }
                     }
                  }
               } else if (icon.useCollider2D) {
                  foreach (var collider2D in colliders2D) {
                     if (collider2D.name.StartsWith(icon.iconLayerName)) {
                        Texture2D map = new Texture2D(layerSizeX, layerSizeY);
                        MakeTextureTransparent(map);

                        GridLayout gridLayout = area.GetComponentInChildren<GridLayout>();
                        Vector2Int collider2DCellPosition = new Vector2Int(gridLayout.WorldToCell(collider2D.transform.position).x, -gridLayout.WorldToCell(collider2D.transform.position).y);

                        var sprite = icon.spriteIcon;

                        if (sprite) {

                           var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
                                 (int) sprite.textureRect.y,
                                 (int) sprite.textureRect.width,
                                 (int) sprite.textureRect.height);

                           map.SetPixels(collider2DCellPosition.x + icon.offset.x, (layerSizeY - collider2DCellPosition.y) + icon.offset.y, (int) sprite.rect.width, (int) sprite.rect.height, pixels);

                           map.Apply();
                           textureList.Add(map);

                        }
                     }
                  }
               }


            }

            if (textureList.Count > 0 && textureList[0]) {
               Texture2D tex = TextureArrayToTexture(textureList.ToArray());

               if (preset.useOutline) {
                  tex = OutlineTexture(tex, preset.outlineColor);
               }

               if (preset.useBackground) {
                  Texture2D[] texArray = new Texture2D[2];
                  //background layer
                  texArray[0] = new Texture2D(layerSizeX, layerSizeY);
                  texArray[1] = tex;

                  for (int y = 0; y <= layerSizeY; y++) {
                     for (int x = 0; x <= layerSizeX; x++) {
                        texArray[0].SetPixel(x, layerSizeY - y, preset.backgroundColor);
                     }
                  }

                  if (saveMap) {
                     ExportTexture(TextureArrayToTexture(texArray), preset.imagePrefixName + area.GetComponent<Area>().areaType + preset.imageSuffixName);
                  } else {
                     TextureScale.Point(tex, _textureSize.x, _textureSize.y);
                     PresentMap(tex);
                  }
               } else {
                  if (saveMap) {
                     ExportTexture(tex, preset.imagePrefixName + area.GetComponent<Area>().areaType + preset.imageSuffixName);
                  } else {
                     TextureScale.Point(tex, _textureSize.x, _textureSize.y);
                     PresentMap(tex);
                  }
               }

               //ExportTextureArray(textureList.ToArray(), preset.imagePrefixName + area.name + preset.imageSuffixName);
            }
            textureList.Clear();
         }
      }
      if (saveMap) {
         #if UNITY_EDITOR
         AssetDatabase.Refresh();
         #endif
      }

      _tileLayer = new TileLayer[0];
      _tileIconLayers = new TileIcon[0];
   }

   /// <summary>
   /// export all texture from a array to the project
   /// </summary>
   /// <param name="texturesArray">textures array</param>
   /// <param name="fileName">file name</param>
   void ExportTextureArray (Texture2D[] texturesArray, string fileName = "texture") {
      #if UNITY_EDITOR
      int i = 0;
      foreach (var tex in texturesArray) {
         byte[] atlasPng = tex.EncodeToPNG();
         string path2 = Application.dataPath + _minimapsPath + fileName + "_" + i + ".png";
         File.WriteAllBytes(path2, atlasPng);
         AssetDatabase.Refresh();
         i++;
      }
      #endif
   }

   /// <summary>
   /// export one texture to the project
   /// </summary>
   /// <param name="texture">texture</param>
   /// <param name="fileName">file name</param>
   void ExportTexture (Texture2D texture, string fileName = "texture") {
      #if UNITY_EDITOR
      TextureScale.Point(texture, _textureSize.x, _textureSize.y);

      byte[] atlasPng = texture.EncodeToPNG();
      string path2 = Application.dataPath + _minimapsPath + fileName + ".png";
      File.WriteAllBytes(path2, atlasPng);
      AssetDatabase.Refresh();
      #endif
   }

   /// <summary>
   /// blend texture array in one texture
   /// </summary>
   /// <param name="texturesArray">textures array</param>
   /// <returns></returns>
   Texture2D TextureArrayToTexture (Texture2D[] texturesArray) {
      //create texture
      Texture2D tex = new Texture2D(texturesArray[0].width, texturesArray[0].height, TextureFormat.RGBA32, false);
      MakeTextureTransparent(tex);

      //arrat to store the destination texture's pixels
      Color[] colorArray = new Color[tex.width * tex.height];

      //array of colors derived from the source texture
      Color[][] srcArray = new Color[texturesArray.Length][];

      //populate source array with layer arrays
      for (int i = 0; i < texturesArray.Length; i++) {
         srcArray[i] = texturesArray[i].GetPixels();
      }

      for (int x = 0; x < tex.width; x++) {
         for (int y = 0; y < tex.height; y++) {
            int pixelIndex = x + (y * tex.width);

            for (int i = 0; i < texturesArray.Length; i++) {
               Color srcPixel = srcArray[i][pixelIndex];
               if (srcPixel.a == 1) {
                  colorArray[pixelIndex] = srcPixel;
               } else if (srcPixel.a > 0) {
                  //blend alpha
                  colorArray[pixelIndex] = AlphaBlend(colorArray[pixelIndex], srcPixel);
               }
            }
         }
      }
      tex.SetPixels(colorArray);

      return tex;

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

   // Current list of chest icons
   private List<MM_Icon> _treasureChestIcons = new List<MM_Icon>();

   [SerializeField] TileLayer[] _tileLayer = new TileLayer[0];
   [SerializeField] TileIcon[] _tileIconLayers = new TileIcon[0];
   [SerializeField] Vector2Int _textureSize = new Vector2Int(512, 512);

   #pragma warning disable
   [SerializeField] string _mapsPath = "Assets/Prefabs/Maps/";
   [SerializeField] string _minimapsPath = "/Sprites/Minimaps/";

   // The icons used for different shop types
   private string _shopShipyardIconPath = "Minimap/sign_shipyard";
   private string _shopTraderIconPath = "Minimap/sign_trader";
   private string _shopWeaponsIconPath = "Minimap/sign_weapons";
   #pragma warning restore

   #endregion
}
