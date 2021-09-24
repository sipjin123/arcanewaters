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
using MapCreationTool.Serialization;

public class Minimap : ClientMonoBehaviour {
   #region Public Variables

   // The distance scale the minimap is using
   public static float SCALE = .10f;

   // The prefab we use for creating NPC icons
   public MM_Icon npcIconPrefab;

   // The prefab we use for creating Building icons
   public MM_Icon buildingIconPrefab;

   // The prefab we use for showing impassable areas
   public MM_Icon impassableIconPrefab;

   // The prefab we use for creating treasure chest icon
   public MM_Icon treasureChestIconPrefab;

   // The prefab we use for creating a discovery icon
   public MM_Icon discoveryIconPrefab;

   // The prefab we use for marking sea monster entity
   public MM_SeaMonsterIcon seaMonsterIconPrefab;

   // The prefab we use for marking sea monster entity
   public MM_LandMonsterIcon landMonsterIconPrefab;

   // The prefab we use for showing ship entity icon
   public MM_ShipEntityIcon shipIconPrefab;

   // The prefab we use for showing player icons in our voyage group (only in town)
   public MM_GroupPlayerIcon groupPlayerIconPrefab;

   // The sprites we use for identifying ship entity icons (enemy, friendly, neutral)
   public Sprite enemyShipSprite;
   public Sprite friendlyShipSprite;
   public Sprite neutralShipSprite;

   // The icon to use for NPCs
   public Sprite npcIcon;

   // The Container for the icons we create
   public GameObject iconContainer;

   // The Container for the player ship icons we create
   public GameObject playerShipIconContainer;

   // The Container for the bot ship icons we create
   public GameObject botShipIconContainer;

   // The Container for the discoveries icons we create
   public GameObject discoveryIconContainer;

   // The icons of treasure sites on minimap
   public Image[] treasureSiteImages;

   // Image we're using for the map background
   public Image backgroundImage;

   // Map config that should be consistent with minimap generators
   public MapCreationTool.EditorConfig mapEditorConfig;

   // Store real size based on number of tiles in the current area
   [HideInInspector]
   public Vector2Int realAreaSize;

   // Minimap generator presets (scriptable objects) for sea random maps
   [Header("Random sea prefabs")]
   public MinimapGeneratorPreset seaDesertPreset;
   public MinimapGeneratorPreset seaPinePreset;
   public MinimapGeneratorPreset seaSnowPreset;
   public MinimapGeneratorPreset seaLavaPreset;
   public MinimapGeneratorPreset seaForestPreset;
   public MinimapGeneratorPreset seaMushroomPreset;

   // Minimap generator presets (scriptable objects) for area maps
   [Header("Area map prefabs")]
   public MinimapGeneratorPreset baseDesertPreset;
   public MinimapGeneratorPreset basePinePreset;
   public MinimapGeneratorPreset baseSnowPreset;
   public MinimapGeneratorPreset baseLavaPreset;
   public MinimapGeneratorPreset baseForestPreset;
   public MinimapGeneratorPreset baseMushroomPreset;

   // Sprites used for marking towns in minimap
   [Header("Town icon sprites")]
   public Sprite townForestSprite;
   public Sprite townDesertSprite;
   public Sprite townMushroomSprite;
   public Sprite townSnowSprite;
   public Sprite townLavaSprite;
   public Sprite townPineSprite;

   // Minimap generator presets (scriptable objects) for interior maps
   [Header("Interior map prefabs")]
   public MinimapGeneratorPreset interiorPreset;

   // Minimap generator presets (scriptable objects) for special maps
   [Header("Special map prefabs")]
   public List<MinimapGeneratorPreset> specialPresets = new List<MinimapGeneratorPreset>();

   // Self
   public static Minimap self;

   // The object containing the tooltip text
   public GameObject toolTipContainer;

   // The tooltip text
   public Text tooltipText;

   // The map value to determine that an icon is out of bounds
   public const float MAP_CLAMP_VAL = 55.5f;

   // Reference to the player icon
   public MM_PlayerIcon playerIcon;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
      mapEditorConfig.testConfigCorrectness();
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

#if UNITY_EDITOR
   [MenuItem("Util/Save all minimaps")]
   public static void saveAllMinimaps () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         _mapsUsedToSaveMinimaps = DB_Main.getMaps();
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            _savingMinimapsWithScript = true;
            _currentMinimapIndex = 0;
            Minimap.self.continueMinimapSave();
         });
      });
   }

   public void continueMinimapSave () {
      if (_currentMinimapIndex >= _mapsUsedToSaveMinimaps.Count) {
         _savingMinimapsWithScript = false;
         _currentMinimapIndex = 0;
      } else {
         string areaKey = _mapsUsedToSaveMinimaps[_currentMinimapIndex].name;

         if (areaKey != "customfarm" && areaKey != "customhouse" && areaKey != "Treasure Sites" && areaKey != "Battlegrounds") {
            string rawMapInfo = DB_Main.getMapInfo(areaKey);
            if (rawMapInfo != "") {
               MapManager.self.createLiveMap(areaKey);
               StartCoroutine(waitForMap());
               return;
            }
         }

         _currentMinimapIndex++;
         continueMinimapSave();
      }
   }

   private IEnumerator waitForMap () {
      string areaKey = _mapsUsedToSaveMinimaps[_currentMinimapIndex].name;
      yield return new WaitUntil(() => !MapManager.self.isAreaUnderCreation(areaKey));

      Area.SpecialType specialType = AreaManager.self.getAreaSpecialType(areaKey);
      if (specialType == Area.SpecialType.Town || specialType == Area.SpecialType.League || specialType == Area.SpecialType.TreasureSite) {
         Minimap.self.updateMinimapForNewArea(AreaManager.self.getArea(areaKey), AreaManager.self.getDefaultBiome(areaKey));
      }
      _currentMinimapIndex++;
      continueMinimapSave();
   }
#endif

   void Update () {
      // Hide the minimap if there's no player
      _canvasGroup.alpha = (Global.player == null || Global.isInBattle()) ? 0f : 1f;

      // Don't do anything else if there's no player set right now
      if (Global.player == null) {
         return;
      }

      // If our area changes, update the markers we've created
      if (Global.player.areaKey != _previousAreaKey) {
         updateMinimapForNewArea();
      }
   }

   public void displayIconInfo (string description) {
      tooltipText.text = description;
      toolTipContainer.SetActive(true);
   }

   public void disableIconInfo () {
      toolTipContainer.SetActive(false);
   }

   public float getMaxDistance () {
      return _rect.sizeDelta.x / 2f * SCALE;
   }

   public void updateMinimapForNewArea (Area area = null, Biome.Type biomeType = Biome.Type.None) {
      if (Global.player == null) {
         return;
      }

      if (area == null) {
         area = AreaManager.self.getArea(Global.player.areaKey);
      }

      if (area == null) {
         return;
      }

      Instance instance = null;
      if (biomeType == Biome.Type.None) {
         instance = Global.player.getInstance();
         if (instance == null || !instance.isNetworkPrefabInstantiationFinished) {
            return;
         }

         biomeType = instance.biome;
      }

      // Change the background image - load static images
      backgroundImage.sprite = ImageManager.getSprite("Minimaps/" + area.areaKey, true);

      // Dynamically generate minimap for base map player entered - lack of static images
      if (backgroundImage.sprite == null || backgroundImage.sprite == ImageManager.self.blankSprite) {
         realAreaSize = Vector2Int.zero;

         if (biomeType != Biome.Type.None) {
            TilemapToTextureColorsStatic(area, biomeType, _savingMinimapsWithScript);
         } else if (AreaManager.self.isFarmOfUser(Global.player.areaKey, Global.player.userId)) {
            TilemapToTextureColorsStatic(area, Biome.Type.Forest, _savingMinimapsWithScript);
         } else if (AreaManager.self.isHouseOfUser(Global.player.areaKey, Global.player.userId)) {
            TilemapToTextureColorsStatic(area, Biome.Type.Forest, _savingMinimapsWithScript);
         }
      } else {
         // Static minimap was found - setup data (minimap size for static images might be different than real area size)
         realAreaSize = Vector2Int.zero;

         foreach (var tilemap in area.GetComponentsInChildren<Tilemap>(true)) {
            if (tilemap.size.x > realAreaSize.x) {
               realAreaSize.x = tilemap.size.x;
            }
            if (tilemap.size.y > realAreaSize.y) {
               realAreaSize.y = tilemap.size.y;
            }
         }

         Texture2D texture = backgroundImage.sprite.texture;
         Vector2 minimapMaskSize = backgroundImage.GetComponentInParent<Mask>().rectTransform.sizeDelta;

         if (texture.width != minimapMaskSize.x && texture.width * 2.0f + Mathf.Epsilon >= minimapMaskSize.x) {
            // Background image is twice the size of texture - update size after changing texture size
            backgroundImage.rectTransform.sizeDelta = new Vector2(texture.width * 2.0f, texture.height * 2.0f);
         } else {
            backgroundImage.rectTransform.sizeDelta = minimapMaskSize;
         }
      }

      // If we didn't find a background image, just use a black background
      if (backgroundImage.sprite == null || backgroundImage.sprite == ImageManager.self.blankSprite) {
         backgroundImage.sprite = ImageManager.getSprite("Minimaps/Black");
      }

      // Delete any old markers we created
      iconContainer.DestroyChildren();
      _treasureChestIcons.Clear();
      _seaMonsterIcons.Clear();

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
            if (spawn.spawnKey.Contains(building)) {
               MM_Icon icon = Instantiate(buildingIconPrefab, this.iconContainer.transform);
               icon.getImage().sprite = getBuildingSprite(building);
               icon.target = spawn.gameObject;
               icon.tooltip.text = building;
            }
         }
      }

      foreach (House houseRef in area.GetComponentsInChildren<House>()) {
         MM_Icon icon = Instantiate(buildingIconPrefab, this.iconContainer.transform);
         icon.target = houseRef.gameObject;
         icon.tooltip.text = houseRef.targetDisplayName;
         icon.getImage().sprite = null;
         icon.getImage().color = new Color(0, 0, 0, 0);
         icon.transform.localScale = new Vector2(15, 15);
      }

      // Create icons for all treasure chests
      foreach (TreasureChest chest in TreasureManager.self.GetComponentsInChildren<TreasureChest>()) {
         if (!chest.hasBeenOpened()) {
            addTreasureChestIcon(chest.gameObject);
         }
      }

      // Create icons for all sea monsters
      if (area.isSea) {
         foreach (SeaMonsterEntity seaMonsterEntity in area.GetComponentsInChildren<SeaMonsterEntity>()) {
            addSeaMonsterIcon(area, seaMonsterEntity);
         }
      } 
      // Create icons for all land monsters
      else if (!area.isInterior) {
         foreach (Enemy enemy in area.GetComponentsInChildren<Enemy>()) {
            addLandMonsterIcon(area, enemy);
         }
      }

      // Note the new area type
      _previousAreaKey = Global.player.areaKey;
   }

   public Vector2 getCorrectedPosition (Transform target, Area area) {
      if (area == null) {
         D.debug("No area referenced");
         return new Vector2(0.0f, 0.0f);
      }

      // Prepare data for position calculations
      Vector2 mapPos = backgroundImage.rectTransform.localPosition;
      Vector2 minimapSize = backgroundImage.rectTransform.sizeDelta;
      Vector2 minimapMaskSize = backgroundImage.GetComponentInParent<Mask>().rectTransform.sizeDelta;

      // Get object position relative to area
      Vector2 relativePosition = target.transform.position - area.transform.position;

      // Move it to bottom-left corner (because area position is centered)
      relativePosition += area.getAreaHalfSizeWorld();

      // Calculate relative position in [0, 1] range
      relativePosition /= area.getAreaSizeWorld();

      // Map [0, 1] to minimap
      relativePosition *= minimapSize;

      // Adjust based on minimap translation (map is focused on player icon)
      relativePosition += mapPos;
      relativePosition -= (minimapSize - minimapMaskSize) * 0.5f;
      relativePosition -= minimapMaskSize * 0.5f;

      return relativePosition;
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

   public void deleteTreasureChestIcon (GameObject chestObject) {
      if (_treasureChestIcons.Find(icon => icon.target == chestObject) != null) {
         _treasureChestIcons.Find(icon => icon.target == chestObject).gameObject.SetActive(false);
      } else {
         D.debug("ERROR! Treasure Chest Icon is NULL in minimap!");
      }
   }
   
   public void addDiscoveryIcon (Discovery discovery) {
      MM_Icon icon = Instantiate(discoveryIconPrefab, this.discoveryIconContainer.transform);
      icon.target = discovery.gameObject;
      _discoveryIcons.Add(icon);
   }

   public void deleteDiscoveryIcon (Discovery discovery) {
      MM_Icon icon = _discoveryIcons.Find(x => x.target == discovery.gameObject);

      if (icon != null) {
         Destroy(icon.gameObject);
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

   private void addSeaMonsterIcon (Area currentArea, SeaMonsterEntity seaMonsterEntity) {
      if (seaMonsterEntity != null && !seaMonsterEntity.isDead() && seaMonsterEntity.instanceId == Global.player.instanceId) {
         MM_SeaMonsterIcon icon = Instantiate(seaMonsterIconPrefab, this.iconContainer.transform);
         icon.seaMonster = seaMonsterEntity;
         icon.currentArea = currentArea;
         _seaMonsterIcons.Add(icon);
      }
   }

   private void addLandMonsterIcon (Area currentArea, Enemy enemy) {
      if (enemy != null && !enemy.isDead() && enemy.instanceId == Global.player.instanceId) {
         MM_LandMonsterIcon icon = Instantiate(landMonsterIconPrefab, this.iconContainer.transform);
         icon.enemy = enemy;
         icon.currentArea = currentArea;
         if (enemy && enemy.isBossType) {
            icon.setBossSprite();
         }
         _landMonsterIcons.Add(icon);
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

      Area area = AreaManager.self.getArea(Global.player.areaKey);
      if (area == null) {
         return;
      }

      MM_ShipEntityIcon[] shipIcons = this.playerShipIconContainer.transform.GetComponentsInChildren<MM_ShipEntityIcon>();
      PlayerShipEntity[] shipsArray = area.GetComponentsInChildren<PlayerShipEntity>();
      foreach (PlayerShipEntity ship in shipsArray) {
         bool isCreated = false;
         MM_ShipEntityIcon icon = null;

         if (ship == Global.player) {
            continue;
         }
         foreach (MM_ShipEntityIcon iconShip in shipIcons) {
            if (iconShip.shipEntity == ship) {
               isCreated = true;
               icon = iconShip;
               break;
            }
         }
         if (isCreated) {
            continue;
         }

         if (!isCreated) {
            if (ship.isDead()) {
               continue;
            }
            // Spawn ship prefab
            icon = Instantiate(shipIconPrefab, this.playerShipIconContainer.transform);
            icon.shipEntity = ship;
            icon.currentArea = area;
            icon.setCorrectPosition();
         }

         // Enemy ship
         if (ship.isEnemyOf(Global.player)) {
            icon.GetComponent<Image>().sprite = enemyShipSprite;
         }
         // Friendly ship
         else if (ship.isAllyOf(Global.player)) {
            icon.GetComponent<Image>().sprite = friendlyShipSprite;
         }
         // Neutral ship
         else {
            icon.GetComponent<Image>().sprite = neutralShipSprite;
         }
      }
   }

   private void initializeShipBotEntities () {
      if (Global.player == null) {
         return;
      }

      Area area = AreaManager.self.getArea(Global.player.areaKey);
      if (area == null) {
         return;
      }

      MM_ShipEntityIcon[] shipIcons = this.botShipIconContainer.transform.GetComponentsInChildren<MM_ShipEntityIcon>();
      BotShipEntity[] shipsArray = area.GetComponentsInChildren<BotShipEntity>();
      foreach (BotShipEntity ship in shipsArray) {
         bool isCreated = false;
         MM_ShipEntityIcon icon = null;

         foreach (MM_ShipEntityIcon iconShip in shipIcons) {
            if (iconShip.shipEntity == ship) {
               isCreated = true;
               icon = iconShip;
               break;
            }
         }

         if (!isCreated) {
            if (ship.isDead()) {
               continue;
            }
            // Spawn ship prefab
            icon = Instantiate(shipIconPrefab, this.botShipIconContainer.transform);
            icon.shipEntity = ship;
            icon.currentArea = area;
            icon.setCorrectPosition();
         }

         // Enemy ship
         if (ship.isEnemyOf(Global.player)) {
            icon.GetComponent<Image>().sprite = enemyShipSprite;
         }
         // Friendly ship
         else if (ship.isAllyOf(Global.player)) {
            icon.GetComponent<Image>().sprite = friendlyShipSprite;
         }
         // Neutral ship
         else {
            icon.GetComponent<Image>().sprite = neutralShipSprite;
         }
      }
   }

   private void createRugs (RugMarker[] rugs, int layerSizeX, int layerSizeY, List<Texture2D> textureList) {
      foreach (RugMarker rug in rugs) {
         Texture2D map = new Texture2D(layerSizeX, layerSizeY);
         MakeTextureTransparent(map);

         Color[] pixels = new Color[rug.getPixelCount()];
         for (int i = 0; i < pixels.Length; i++) {
            pixels[i] = rug.getRugColor();
         }

         int xSetPixel = rug.getMinBounds().x + layerSizeX / 2;
         int ySetPixel = rug.getMinBounds().y + layerSizeY / 2;

         map.SetPixels(xSetPixel, ySetPixel, rug.getWidth(), rug.getHeight(), pixels);

         map.Apply();
         textureList.Add(map);
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

   private MinimapGeneratorPreset chooseBaseMapPreset (Area area, Biome.Type biome) {
      if (!area) {
         D.debug("Couldn't get map instance!");
         return baseForestPreset;
      }

      int specialPresetIndex = specialPresets.FindIndex((MinimapGeneratorPreset preset) => preset.specialTypeAreaKey == area.areaKey);
      if (specialPresetIndex != -1) {
         return specialPresets[specialPresetIndex];
      }

      if (area.isSea) {
         return lookUpSeaPreset(biome);
      } else if (area.isInterior) {
         return interiorPreset;
      } else {
         switch (biome) {
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

      D.debug("Couldn't match biome type to given area!");
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
      D.debug("Couldn't match biome type to given area!");
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
      // Create sprite with exact size of passed texture
      _seaRandomSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 100, 1, SpriteMeshType.FullRect);
      _seaRandomSprite.texture.filterMode = FilterMode.Point;
      backgroundImage.sprite = _seaRandomSprite;

      Vector2 minimapMaskSize = backgroundImage.GetComponentInParent<Mask>().rectTransform.sizeDelta;

      if (texture.width * 2.0f + Mathf.Epsilon >= minimapMaskSize.x) {
         // Background image is twice the size of texture - update size after changing texture size
         backgroundImage.rectTransform.sizeDelta = new Vector2(texture.width * 2.0f, texture.height * 2.0f);
      } else {
         backgroundImage.rectTransform.sizeDelta = minimapMaskSize;
      }
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
         TilemapToTextureColorsStatic(area.GetComponent<Area>(), AreaManager.self.getDefaultBiome(area.GetComponent<Area>().name), true);
      }
      #endif
   }

   void TilemapToTextureColorsStatic (Area area, Biome.Type biome, bool saveMap) {
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

      float[,] depthValues = new float[layerSizeX, layerSizeY];
      for (int x = 0; x < layerSizeX; x++) {
         for (int y = 0; y < layerSizeY; y++) {
            depthValues[x, y] = float.MaxValue;
         }
      }

      int layerOriginX = -layerSizeX / 2;
      int layerOriginY = -layerSizeY / 2;

      if (area) {
         HashSet<string> createdPrefabIconsPerGrid = new HashSet<string>();
         Dictionary<string, List<Vector2Int>> createdPrefabIcons = new Dictionary<string, List<Vector2Int>>();
         Transform[] prefabs = area.gameObject.transform.Find("Prefabs") ? area.gameObject.transform.Find("Prefabs").GetComponentsInChildren<Transform>() : new Transform[0];
         RugMarker[] rugs = area.gameObject.GetComponentsInChildren<RugMarker>();
         AreaEffector2D[] areaEffectors2D = area.GetComponentsInChildren<AreaEffector2D>();
         Collider2D[] colliders2D = area.GetComponentsInChildren<Collider2D>();
         MinimapGeneratorPreset preset = chooseBaseMapPreset(area, biome);

         if (preset) {
            _tileLayer = preset._tileLayer;
            _tileIconLayers = preset._tileIconLayers;
            _textureSize = preset._textureSize;

            // Iterate over preset first to allow layers reordering
            foreach (var layer in _tileLayer) {
               // Handle special layer case
               if (layer.Name == MapCreationTool.Layer.RUG_KEY) {
                  createRugs(rugs, layerSizeX, layerSizeY, textureList);
                  continue;
               }
               // Locate the tilemaps within the area
               foreach (TilemapLayer tilemapLayer in area.getTilemapLayers()) {
                  Tilemap tilemap = tilemapLayer.tilemap;
                  if (tilemap.gameObject.activeSelf == false) {
                     continue;
                  }
                  // Create a variable texture we can write to
                  Texture2D map = null;
                  Texture2D borderOnlyMap = null;

                  bool flipflop = true;

                  if (string.Compare(layer.useFullName ? tilemapLayer.fullName : tilemapLayer.name, layer.Name, true) == 0) {                     
                     map = new Texture2D(layerSizeX, layerSizeY);
                     if (layer.isOnlyBorder) {
                        borderOnlyMap = new Texture2D(layerSizeX, layerSizeY);
                        MakeTextureTransparent(borderOnlyMap);
                     }
                     MakeTextureTransparent(map);

                     int layerOriginDiffX = tilemap.origin.x - layerOriginX;
                     int layerOriginDiffY = tilemap.origin.y - layerOriginY;

                     if (!layer.useHorizontalAlternatingColor && !layer.useVerticalAlternatingColor) {
                        // Cycle over all the Tile positions in this Tilemap layer
                        for (int y = 0; y <= layerSizeY; y++) {
                           for (int x = 0; x <= layerSizeX; x++) {
                              // Check which Tile is at the cell position
                              Vector3Int cellPos = new Vector3Int(x + tilemap.origin.x, layerSizeY - y + tilemap.origin.y, 0);
                              int mapPixelToSetX = x + layerOriginDiffX;
                              int mapPixelToSetY = layerSizeY - y + layerOriginDiffY;

                              var tileSprite = tilemap.GetSprite(cellPos);

                              if (tileSprite) {
                                 //checks if are using a sprite to compare
                                 if (!layer.isSubLayer) {
                                    //set base color
                                    map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);

                                    //set random color
                                    if (layer.useRandomColor) {
                                       if (Random.Range(0, 2) == 0) {
                                          map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                       } else {
                                          map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.randomColor);
                                       }
                                    }

                                    //set alternating color
                                    if (layer.useAlternatingColor) {
                                       if (flipflop) {
                                          map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                          flipflop = false;
                                       } else {
                                          map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.alternatingColor);
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
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);

                                                //set random color
                                                if (layer.useRandomColor) {
                                                   if (Random.Range(0, 2) == 0) {
                                                      map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                   } else {
                                                      map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.randomColor);
                                                   }
                                                }

                                                //set alternating color
                                                if (layer.useVerticalAlternatingColor) {
                                                   if (flipflop) {
                                                      map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                      flipflop = false;
                                                   } else {
                                                      map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.verticalAlternatingColor);
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
                                             map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);

                                             //set random color
                                             if (layer.useRandomColor) {
                                                if (Random.Range(0, 2) == 0) {
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                } else {
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.randomColor);
                                                }
                                             }

                                             //set alternating color
                                             if (layer.useVerticalAlternatingColor) {
                                                if (flipflop) {
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                   flipflop = false;
                                                } else {
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.verticalAlternatingColor);
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
                                          map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);

                                          //set random color
                                          if (layer.useRandomColor) {
                                             if (Random.Range(0, 2) == 0) {
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                             } else {
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.randomColor);
                                             }
                                          }

                                          //set alternating color
                                          if (layer.useVerticalAlternatingColor) {
                                             if (flipflop) {
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                flipflop = false;
                                             } else {
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.verticalAlternatingColor);
                                                flipflop = true;
                                             }
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
                              Vector3Int cellPos = new Vector3Int(x + tilemap.origin.x, layerSizeY - y + tilemap.origin.y, 0);
                              int mapPixelToSetX = x + layerOriginDiffX;
                              int mapPixelToSetY = layerSizeY - y + layerOriginDiffY;

                              var tileSprite = tilemap.GetSprite(cellPos);

                              if (tileSprite) {
                                 //checks if are using a sprite to compare
                                 if (!layer.isSubLayer) {
                                    if (flipflop) {
                                       map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                       flipflop = false;
                                    } else {
                                       map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.horizontalAlternatingColor);
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
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                   flipflop = false;
                                                } else {
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.horizontalAlternatingColor);
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
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                flipflop = false;
                                             } else {
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.horizontalAlternatingColor);
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
                                             map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                             flipflop = false;
                                          } else {
                                             map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.horizontalAlternatingColor);
                                             flipflop = true;
                                          }
                                       }
                                    }

                                 }
                              }
                           }
                        }
                     }

                     else if (layer.useVerticalAlternatingColor) {
                        // Cycle over all the Tile positions in this Tilemap layer
                        for (int y = 0; y <= layerSizeY; y++) {
                           flipflop = true;
                           for (int x = 0; x <= layerSizeX; x++) {
                              // Check which Tile is at the cell position
                              Vector3Int cellPos = new Vector3Int(x + tilemap.origin.x, layerSizeY - y + tilemap.origin.y, 0);
                              int mapPixelToSetX = x + layerOriginDiffX;
                              int mapPixelToSetY = layerSizeY - y + layerOriginDiffY;

                              var tileSprite = tilemap.GetSprite(cellPos);

                              if (tileSprite) {
                                 //checks if are using a sprite to compare
                                 if (!layer.isSubLayer) {
                                    if (flipflop) {
                                       map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                       flipflop = false;
                                    } else {
                                       map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.verticalAlternatingColor);
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
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                   flipflop = false;
                                                } else {
                                                   map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.verticalAlternatingColor);
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
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                                flipflop = false;
                                             } else {
                                                map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.verticalAlternatingColor);
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
                                             map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.color);
                                             flipflop = false;
                                          } else {
                                             map.SetPixel(mapPixelToSetX, mapPixelToSetY, layer.verticalAlternatingColor);
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
                                 // Set border color
                                 if (layer.useBorder) {
                                    if (map.GetPixel(x + 1, y).a <= 0 || map.GetPixel(x - 1, y).a <= 0 || map.GetPixel(x, y + 1).a <= 0 || map.GetPixel(x, y - 1).a <= 0) {
                                       if (!(map.GetPixel(x, y + 1).a <= 0 && map.GetPixel(x, y - 1).a <= 0)) { // XOR for 1px width
                                          if (!(map.GetPixel(x + 1, y).a <= 0 && map.GetPixel(x - 1, y).a <= 0)) { // XOR for 1px height
                                             // Write only to border
                                             if (layer.isOnlyBorder) {
                                                borderOnlyMap.SetPixel(x, y, layer.borderColor);
                                             } else {
                                                map.SetPixel(x, y, layer.borderColor);
                                             }
                                          }
                                       }
                                    }
                                 }

                                 // Set top down color
                                 if (layer.useTopDownBorder) {
                                    if (map.GetPixel(x, y + 1).a <= 0 || map.GetPixel(x, y - 1).a <= 0) {
                                       if (!(map.GetPixel(x, y + 1).a <= 0 && map.GetPixel(x, y - 1).a <= 0)) { // XOR for 1px width
                                          // Write only to border
                                          if (layer.isOnlyBorder) {
                                             borderOnlyMap.SetPixel(x, y, layer.topDownBorderColor);
                                          } else {
                                             map.SetPixel(x, y, layer.topDownBorderColor);
                                          }
                                       }
                                    }
                                 }

                                 // Set lateral border color
                                 if (layer.useLateralBorder) {
                                    if (map.GetPixel(x + 1, y).a <= 0 || map.GetPixel(x - 1, y).a <= 0) {
                                       if (!(map.GetPixel(x + 1, y).a <= 0 && map.GetPixel(x - 1, y).a <= 0)) { // XOR for 1px height
                                          // Write only to border
                                          if (layer.isOnlyBorder) {
                                             borderOnlyMap.SetPixel(x, y, layer.lateralColor);
                                          } else {
                                             map.SetPixel(x, y, layer.lateralColor);
                                          }
                                       }
                                    }
                                 }

                                 // Set top border color
                                 if (layer.useTopBorder) {
                                    if (map.GetPixel(x, y + 1).a <= 0) {
                                       if (!(map.GetPixel(x, y + 1).a <= 0 && map.GetPixel(x, y - 1).a <= 0)) { // XOR for 1px width
                                          for (int px = 0; px < ((layer.topPixelCount != 0) ? layer.topPixelCount : 1) && y + px < map.height; px++) {
                                             // Write only to border
                                             if (layer.isOnlyBorder) {
                                                borderOnlyMap.SetPixel(x, y + px, layer.topBorderColor);
                                             } else {
                                                map.SetPixel(x, y + px, layer.topBorderColor);
                                             }
                                          }
                                       }
                                    }
                                 }

                                 // Set down border color
                                 if (layer.useDownBorder) {
                                    if (map.GetPixel(x, y - 1).a <= 0 && y - 1 >= 0) {
                                       if (!(map.GetPixel(x, y + 1).a <= 0 && map.GetPixel(x, y - 1).a <= 0)) { // XOR for 1px width
                                          for (int px = 0; px < ((layer.downPixelCount != 0) ? layer.downPixelCount : 1) && y - px >= 0; px++) {
                                             // Write only to border
                                             if (layer.isOnlyBorder) {
                                                borderOnlyMap.SetPixel(x, y - px, layer.downBorderColor);
                                             } else {
                                                map.SetPixel(x, y - px, layer.downBorderColor);
                                             }
                                          }
                                       }
                                    }
                                 }

                                 // Set down border color - second layer
                                 if (layer.useAnotherDownBorder) {
                                    if (map.GetPixel(x, y - 1).a <= 0 && y - 1 >= 0) {
                                       if (!(map.GetPixel(x, y + 1).a <= 0 && map.GetPixel(x, y - 1).a <= 0)) { // XOR for 1px width
                                          for (int px = 0; px < ((layer.anotherDownPixelCount != 0) ? layer.anotherDownPixelCount : 1) && y - px >= 0; px++) {
                                             // Write only to border
                                             if (layer.isOnlyBorder) {
                                                borderOnlyMap.SetPixel(x, y - px, layer.anotherDownBorderColor);
                                             } else {
                                                map.SetPixel(x, y - px, layer.anotherDownBorderColor);
                                             }
                                          }
                                       }
                                    }
                                 }

                              }
                           }
                        }
                     }

                     if (map && borderOnlyMap) {
                        map = borderOnlyMap;
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
               if (area.isSea && icon.iconLayerName == _treasureSiteIconName) {
                  TreasureSite[] treasureSites = area.gameObject.transform.Find("Treasure Sites") ? area.gameObject.transform.Find("Treasure Sites").GetComponentsInChildren<TreasureSite>() : new TreasureSite[0];
                  foreach (var treasureSite in treasureSites) {
                     if (treasureSite.isActive() && treasureSite.GetComponent<SpriteRenderer>() && treasureSite.GetComponent<SpriteRenderer>().enabled) {
                        addIconToTexture(layerSizeY, layerSizeX, area, treasureSite.transform, icon, ref textureList);
                     }
                  }
               } else if (icon.useAreaEffector2D) {
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

                           for (int y = 0; y < (int) sprite.rect.height; y++) {
                              for (int x = 0; x < (int) sprite.rect.width; x++) {
                                 Color pixel = pixels[y * (int) sprite.rect.width + x];
                                 float z = areaEffector2D.transform.position.z;
                                 Vector2Int pos = new Vector2Int(areaEffector2DCellPosition.x + icon.offset.x + x, (layerSizeY - areaEffector2DCellPosition.y) + icon.offset.y + y);
                                 if (pixel.a != 0.0f && depthValues[pos.x, pos.y] > z) {
                                    map.SetPixel(pos.x, pos.y, pixel);
                                    depthValues[pos.x, pos.y] = z;
                                 }
                              }
                           }

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

                           int xSetPixel = Mathf.Clamp(collider2DCellPosition.x + icon.offset.x - (-layerSizeX / 2), 0, map.width - (int) sprite.textureRect.width);
                           int ySetPixel = Mathf.Clamp(layerSizeY + icon.offset.y - (collider2DCellPosition.y - (-layerSizeY / 2)), 0, map.height - (int) sprite.textureRect.height);

                           for (int y = 0; y < (int) sprite.rect.height; y++) {
                              for (int x = 0; x < (int) sprite.rect.width; x++) {
                                 Color pixel = pixels[y * (int) sprite.rect.width + x];
                                 float z = collider2D.transform.position.z;
                                 Vector2Int pos = new Vector2Int(xSetPixel + x, ySetPixel + y);
                                 if (pixel.a != 0.0f && depthValues[pos.x, pos.y] > z) {
                                    map.SetPixel(pos.x, pos.y, pixel);
                                    depthValues[pos.x, pos.y] = z;
                                 }
                              }
                           }

                           map.Apply();
                           textureList.Add(map);
                        }
                     }
                  }
               } else if (icon.usePrefab) {
                  foreach (var pref in prefabs) {
                     if (pref.name.StartsWith(icon.iconLayerName)) {
                        // Special case of prefab icon - warps
                        if (icon.iconLayerName == _warpIconName) {
                           if (area.isSea && pref.GetComponent<Warp>()?.targetInfo?.specialType == Area.SpecialType.Town) {
                              Sprite warpTownSprite = getTownWarpSprite(pref.GetComponent<Warp>().targetInfo.biome);
                              if (warpTownSprite != null) {
                                 addIconToTexture(layerSizeY, layerSizeX, area, pref.transform, warpTownSprite, new Vector2Int(-3, -3), ref textureList);
                              }
                           }
                           break;
                        }
                        Texture2D map = new Texture2D(layerSizeX, layerSizeY);
                        MakeTextureTransparent(map);

                        GridLayout gridLayout = area.GetComponentInChildren<GridLayout>();
                        Vector2Int collider2DCellPosition = new Vector2Int(gridLayout.WorldToCell(pref.transform.position).x, -gridLayout.WorldToCell(pref.transform.position).y);

                        var sprite = icon.spriteIcon;

                        if (sprite) {
                           var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
                                 (int) sprite.textureRect.y,
                                 (int) sprite.textureRect.width,
                                 (int) sprite.textureRect.height);

                           int xSetPixel = Mathf.Clamp(collider2DCellPosition.x + icon.offset.x - (-layerSizeX / 2), 0, map.width - (int) sprite.textureRect.width);
                           int ySetPixel = Mathf.Clamp(layerSizeY + icon.offset.y - (collider2DCellPosition.y - (-layerSizeY / 2)), 0, map.height - (int) sprite.textureRect.height);
                           bool saveResult = true;

                           if (icon.limitSpawnCount) {
                              // Check if icon already exist in grid
                              string gridKey = icon.iconLayerName + "grid_" + (xSetPixel / icon.spawnGridSize.x) + "_" + (ySetPixel / icon.spawnGridSize.y);
                              if (!createdPrefabIconsPerGrid.Contains(gridKey)) {
                                 // Check if distance between icons of given type is correct
                                 if (createdPrefabIcons.ContainsKey(icon.iconLayerName) && icon.minDistanceManhattan > 0) {
                                    foreach (var pos in createdPrefabIcons[icon.iconLayerName]) {
                                       if (Math.Abs(xSetPixel - pos.x) + Math.Abs(ySetPixel - pos.y) < icon.minDistanceManhattan) {
                                          saveResult = false;
                                          break;
                                       }
                                    }
                                 }
                              } else {
                                 saveResult = false;
                              }

                              // Save grid dictionary entry earlier to avoid recreating string
                              if (saveResult) {
                                 createdPrefabIconsPerGrid.Add(gridKey);
                              }
                           }

                           if (saveResult) {
                              if (icon.limitSpawnCount) {
                                 if (!createdPrefabIcons.ContainsKey(icon.iconLayerName)) {
                                    createdPrefabIcons.Add(icon.iconLayerName, new List<Vector2Int>());
                                 }
                                 createdPrefabIcons[icon.iconLayerName].Add(new Vector2Int(xSetPixel, ySetPixel));
                              }

                              for (int y = 0; y < (int) sprite.rect.height; y++) {
                                 for (int x = 0; x < (int) sprite.rect.width; x++) {
                                    Color pixel = pixels[y * (int) sprite.rect.width + x];
                                    float z = pref.transform.position.z;
                                    Vector2Int pos = new Vector2Int(xSetPixel + x, ySetPixel + y);
                                    if (pixel.a != 0.0f && depthValues[pos.x, pos.y] > z) {
                                       map.SetPixel(pos.x, pos.y, pixel);
                                       depthValues[pos.x, pos.y] = z;
                                    }
                                 }
                              }

                              map.Apply();
                              textureList.Add(map);
                           }
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
                  tex = TextureArrayToTexture(texArray);

                  if (saveMap) {
                     ExportTexture(TextureArrayToTexture(texArray), preset.imagePrefixName + area.GetComponent<Area>().areaKey + preset.imageSuffixName);
                  } else {
                     TextureScale.Point(tex, _textureSize.x, _textureSize.y);
                     PresentMap(tex);
                  }
               } else {
                  if (saveMap) {
                     ExportTexture(tex, preset.imagePrefixName + area.GetComponent<Area>().areaKey + preset.imageSuffixName);
                  } else {                    
                     // Use scale based on real texture size
                     TextureScale.Point(tex, tex.width, tex.height);
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

   private void addIconToTexture (int mapHeight, int mapWidth, Area area, Transform transform, TileIcon icon, ref List<Texture2D> textureList) {
      addIconToTexture(mapHeight, mapHeight, area, transform, icon.spriteIcon, icon.offset, ref textureList);
   }

   private void addIconToTexture (int mapHeight, int mapWidth, Area area, Transform transform, Sprite sprite, Vector2Int offset, ref List<Texture2D> textureList) {
      Texture2D map = new Texture2D(mapHeight, mapWidth);
      MakeTextureTransparent(map);

      GridLayout gridLayout = area.GetComponentInChildren<GridLayout>();
      Vector2Int collider2DCellPosition = new Vector2Int(gridLayout.WorldToCell(transform.position).x, -gridLayout.WorldToCell(transform.position).y);

      if (sprite) {
         var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
               (int) sprite.textureRect.y,
               (int) sprite.textureRect.width,
               (int) sprite.textureRect.height);

         int xSetPixel = Mathf.Clamp(collider2DCellPosition.x + offset.x - (-mapWidth / 2), 0, map.width - (int) sprite.textureRect.width);
         int ySetPixel = Mathf.Clamp(mapHeight + offset.y - (collider2DCellPosition.y - (-mapHeight / 2)), 0, map.height - (int) sprite.textureRect.height);

         map.SetPixels(xSetPixel, ySetPixel, (int) sprite.rect.width, (int) sprite.rect.height, pixels);

         map.Apply();
         textureList.Add(map);
      }
   }

   private Sprite getTownWarpSprite (Biome.Type biome) {
      switch (biome) {
         case Biome.Type.Desert:
            return townDesertSprite;
         case Biome.Type.Forest:
            return townForestSprite;
         case Biome.Type.Lava:
            return townLavaSprite;
         case Biome.Type.Mushroom:
            return townMushroomSprite;
         case Biome.Type.Pine:
            return townPineSprite;
         case Biome.Type.Snow:
            return townSnowSprite;
      }
      return null;
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
   protected string _previousAreaKey;

   // Current list of chest icons
   private List<MM_Icon> _treasureChestIcons = new List<MM_Icon>();

   // Current list of discovery icons
   private List<MM_Icon> _discoveryIcons = new List<MM_Icon>();

   // Current list of sea monster entity icons
   public List<MM_SeaMonsterIcon> _seaMonsterIcons = new List<MM_SeaMonsterIcon>();

   // Current list of land monster entity icons
   public List<MM_LandMonsterIcon> _landMonsterIcons = new List<MM_LandMonsterIcon>();

   [SerializeField] TileLayer[] _tileLayer = new TileLayer[0];
   [SerializeField] TileIcon[] _tileIconLayers = new TileIcon[0];
   [SerializeField] Vector2Int _textureSize = new Vector2Int(512, 512);

   [SerializeField] string _mapsPath = "Assets/Prefabs/Maps/";
   [SerializeField] string _minimapsPath = "Resources/Sprites/Minimaps/Editor";

   // The icons used for different shop types
   private string _shopShipyardIconPath = "Minimap/sign_shipyard";
   private string _shopTraderIconPath = "Minimap/sign_trader";
   private string _shopWeaponsIconPath = "Minimap/sign_weapons";

   // Name of special case icon layers
   private string _treasureSiteIconName = "TreasureSite";
   private string _warpIconName = "Warp";

   // Is user currently generating minimaps with script
   private static bool _savingMinimapsWithScript = false;

   // Stored map list available in database, used to generate minimaps with script
   private static List<Map> _mapsUsedToSaveMinimaps = new List<Map>();

   // Index of current map in the list - used to generate minimaps with script
   private static int _currentMinimapIndex = 0;

   #endregion
}
