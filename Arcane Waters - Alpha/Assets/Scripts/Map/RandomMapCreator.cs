using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using ProceduralMap;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine.Tilemaps;

public static class RandomMapCreator {
   #region Public Variables

   #endregion

   private static MapGeneratorPreset GetBiomePreset (MapConfig mapConfig) {
      MapGeneratorPreset preset = null;
      switch (mapConfig.biomeType) {
         case Biome.Type.Forest:
            preset = RandomMapManager.self.tropicalPreset;
            break;
         case Biome.Type.Desert:
            preset = RandomMapManager.self.desertPreset;
            break;
         case Biome.Type.Pine:
            preset = RandomMapManager.self.pinePreset;
            break;
         case Biome.Type.Snow:
            preset = RandomMapManager.self.snowPreset;
            break;
         case Biome.Type.Lava:
            preset = RandomMapManager.self.lavaPreset;
            break;
         case Biome.Type.Mushroom:
            preset = RandomMapManager.self.mushroomPreset;
            break;
      }

      if (preset) {
         return preset;
      }
      return RandomMapManager.self.presets.ChooseRandom();
   }

   /// <summary>
   /// Generate and moves player to created map
   /// </summary>
   public static GameObject generateRandomMap (MapConfig mapConfig) {
      // If tiles are already generated for this client - do not spawn again
      if (_generatedAreas.Contains(mapConfig.areaType)) {
         return null;
      }

      // Generate map and save it to variable
      MapGeneratorPreset preset = GetBiomePreset(mapConfig);
      if (preset == null) {
         D.error("No valid preset exists for RandomMapGenerator");
         return null;
      }

      GameObject generatedMap = GenerateMap(preset, mapConfig);

      if (generatedMap == null) {
         D.error("Failed to generate map");
         return null;
      }

      const float mapScale = 0.16f;
      _generatedAreas.Add(mapConfig.areaType);

      // Scale down the generated map
      generatedMap.transform.localScale = new Vector3(mapScale, mapScale, mapScale);

      // Get area root object
      Area area = AreaManager.self.getArea(mapConfig.areaType);

      // Set the parent and local position
      generatedMap.transform.SetParent(area.transform, false);
      generatedMap.transform.localPosition = new Vector3(0f, -10.24f, 0f);

      // Set spawn position in spawn area range
      Vector2 spawnPosition = Vector2.zero;
      spawnPosition.y = Random.Range(2, _areaSize.Length - 1) * 0.5f * mapScale - 10.0f;
      spawnPosition.x = (_spawnStartsBottom[_areaSize.Length / 2] + (_spawnEndsBottom[_areaSize.Length / 2] - _spawnStartsBottom[_areaSize.Length / 2]) * 0.5f) * mapScale;
      area.transform.GetComponentInChildren<Spawn>().transform.localPosition = spawnPosition;

      // Set warp zone at spawn areas
      CreateWarpBack(true, preset, area);
      CreateWarpBack(false, preset, area);

      return generatedMap;
   }

   private static void CreateWarpBack(bool bottom, MapGeneratorPreset preset, Area area) {
      // Create warp object to docks
      Warp backWarp = new GameObject(bottom ? "Warp Back Bottom" : "Warp Back Top").AddComponent<Warp>();
      backWarp.gameObject.AddComponent<SpriteRenderer>();
      backWarp.spawnTarget = Spawn.Type.ForestTownDock;
      backWarp.transform.SetParent(area.transform);
      float localScale = (1 + (bottom ? _spawnEndsBottom[0] - _spawnStartsBottom[0] : _spawnEndsTop[0] - _spawnStartsTop[0])) / 64.0f * 10.25f;
      backWarp.transform.localScale = new Vector3(localScale, 1f, 1f);
      float startPos = bottom ? _spawnStartsBottom[0] : _spawnStartsTop[0];
      float localPosition = (startPos / (float) preset.mapSize.x * 10.25f) + localScale * 0.5f;
      backWarp.transform.localPosition = new Vector2(localPosition,  bottom ? -10.5f : 0.25f); //TODO Magic number

      // Trigger collider for warp
      BoxCollider2D warpCol = backWarp.gameObject.AddComponent<BoxCollider2D>();
      warpCol.size = new Vector2(1.0f, 1.0f);
      warpCol.isTrigger = true;
   }

   /// <summary>
   /// Generate the map object
   /// </summary>
   private static GameObject GenerateMap (MapGeneratorPreset preset, MapConfig mapConfig) {
      if (preset == null) {
         D.error("Map contains empty preset!");
         return null;
      }

      if (preset.updateTilesFromProject) {
         SetTiles(preset);
      }
      Node[,] grid;

      GameObject prefab = new GameObject(preset.MapPrefixName + preset.MapName + preset.MapSuffixName);
      GameObject gridLayers = new GameObject("Grid Layers");
      gridLayers.transform.parent = prefab.transform;

      gridLayers.AddComponent(typeof(Grid));

      Rigidbody2D rigidbody2D = gridLayers.AddComponent(typeof(Rigidbody2D)) as Rigidbody2D;
      rigidbody2D.bodyType = RigidbodyType2D.Static;

      CompositeCollider2D compositeCollider = gridLayers.AddComponent(typeof(CompositeCollider2D)) as CompositeCollider2D;

      Tilemap[] tilemaps = new Tilemap[preset.layers.Length];

      float[,] noiseMap = null;

      noiseMap = GenerateNoiseMap(preset.mapSize, mapConfig.seed, preset.noiseScale, preset.octaves, mapConfig.persistance, mapConfig.lacunarity, mapConfig.offset);

      // Prepare spawning area on bottom and top of the map
      PrepareWaterTiles(preset, noiseMap);
      SpawnStartArea(preset, noiseMap, true);
      SpawnStartArea(preset, noiseMap, false);

      // If there isn't path available - generate one
      if (!IsAnyPathBetweenSpawns(preset)) {
         CreatePathBetweenSpawns(preset, gridLayers, noiseMap, mapConfig.seedPath);
      }

      // Find inaccessible areas
      FindClosedArea(noiseMap, preset, gridLayers, mapConfig.seedPath);

      // Set land tiles for border
      GenerateLandBorder(noiseMap, preset);

      for (int i = 0; i < preset.layers.Length; i++) {
         grid = new Node[preset.mapSize.x, preset.mapSize.y];
         if (preset.layers[i].name == preset.river.layerToPlaceRiver) {
            for (int y = 0; y < preset.mapSize.y; y++) {
               for (int x = 0; x < preset.mapSize.x; x++) {
                  grid[x, y] = new Node(x, y, new Vector3(x, y, 0), NodeType.Wall);
               }
            }
         }

         GameObject baseLayerGameObject = new GameObject(preset.layers[i].name);
         baseLayerGameObject.transform.position = new Vector3(baseLayerGameObject.transform.position.x, baseLayerGameObject.transform.position.y, (float) (preset.layers.Length - i) / 10);
         baseLayerGameObject.transform.parent = gridLayers.transform;
         Tilemap baseTilemap = baseLayerGameObject.AddComponent(typeof(Tilemap)) as Tilemap;
         TilemapRenderer baseTilemapRenderer = baseLayerGameObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
         baseTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

         tilemaps[i] = baseTilemap;
         bool[,] availableTiles = new bool[preset.mapSize.x, preset.mapSize.y];

         if (preset.layers[i].useCollider) {
            TilemapCollider2D tilemapCollider = baseLayerGameObject.AddComponent(typeof(TilemapCollider2D)) as TilemapCollider2D;
            tilemapCollider.usedByComposite = true;
         }

         SetBaseLayers(preset, noiseMap, i, baseTilemap, availableTiles, grid);

         if (preset.layers[i].useBorderOnDifferentLayer) {
            GameObject borderLayerObject = new GameObject(preset.layers[i].name + preset.layers[i].BorderLayerName);
            borderLayerObject.transform.position = new Vector3(borderLayerObject.transform.position.x, borderLayerObject.transform.position.y, (float) (preset.layers.Length - i) / 10);
            borderLayerObject.transform.parent = gridLayers.transform;
            Tilemap borderTilemap = borderLayerObject.AddComponent(typeof(Tilemap)) as Tilemap;
            TilemapRenderer borderTilemapRenderer = borderLayerObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
            borderTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

            SetBorder(preset, i, baseTilemap, borderTilemap, availableTiles, grid);

            SetCorner(preset, i, gridLayers, baseTilemap, borderTilemap, availableTiles, grid);

         } else {
            SetBorder(preset, i, baseTilemap, baseTilemap, availableTiles, grid);

            SetCorner(preset, i, gridLayers, baseTilemap, baseTilemap, availableTiles, grid);
         }

         SetRiver(preset, grid, gridLayers, i, availableTiles);

         SetObjects(preset, gridLayers, availableTiles, i);
      }

#if UNITY_EDITOR
      if (_enableDrawDebug) {
         DrawStartAreaTiles(false, gridLayers, preset.mapSize.y);
         DrawStartAreaTiles(true, gridLayers, preset.mapSize.y);
      }
#endif

      return prefab;
   }

   static void PrepareWaterTiles(MapGeneratorPreset preset, float[,] noiseMap) {
      _waterTiles = new bool[preset.mapSize.x, preset.mapSize.y];

      for (int y = 0; y < preset.mapSize.y; y++) {
         for (int x = 0; x < preset.mapSize.x; x++) {
            _waterTiles[x, y] = true;
         }
      }

      bool anyLand = false;
      for (int i = 0; i < preset.layers.Length; i++) {
         if (!preset.layers[i].isLand) {
            continue;
         }
         anyLand = true;
         float layerHeight = preset.layers[i].height;
         for (int y = 0; y < preset.mapSize.y; y++) {
            for (int x = 0; x < preset.mapSize.x; x++) {
               if (noiseMap[x, y] <= layerHeight) {
                  _waterTiles[x, y] = false;
               }
            }
         }
      }

      if (!anyLand) {
         D.error("No land found. Check your map preset and choose land flag for correct layers");
      }
   }

   static void SpawnStartArea (MapGeneratorPreset preset, float[,] noiseMap, bool bottomSpawn) {

      int lastRow = preset.mapSize.y - 1;

      List<int> newStart = new List<int>();
      List<int> newEnd = new List<int>();
      bool foundIdealSpot = false;

      // Looking for ideal spot
      if (SearchAllStartAreas(preset.mapSize.x, bottomSpawn ? 0 : lastRow, _areaSize[0], ref newStart, ref newEnd)) {
         for (int i = 0; i < newStart.Count; i++) {
            int start = newStart[i];
            int end = newEnd[i];
            if (newStart[i] == newEnd[i]) {
               continue;
            }

            if (bottomSpawn) {
               _spawnStartsBottom[0] = start;
               _spawnEndsBottom[0] = end;
            } else {
               _spawnStartsTop[0] = start;
               _spawnEndsTop[0] = end;
            }

            // Iterate rows and check if they meet criteria
            for (int index = 1; index < _areaSize.Length; index++) {
               if (SearchStartArea(start, end, bottomSpawn ? index : lastRow - index, _areaSize[index], ref start, ref end)) {
                  if (bottomSpawn) {
                     _spawnStartsBottom[index] = start;
                     _spawnEndsBottom[index] = end;
                  } else {
                     _spawnStartsTop[index] = start;
                     _spawnEndsTop[index] = end;
                  }
               } else {
                  goto skip;
               }
            }
            // If all rows are ok, this will be spawn spot
            foundIdealSpot = true;
            break;

            skip:
            { }
         }
      }

      if (!foundIdealSpot) {
         int firstRowStart = 0;
         int firstRowEnd = 0;
         // No ideal spot exist - expand the biggest one
         if (newStart.Count > 0 && newEnd.Count > 0) {
            firstRowStart = newStart[0];
            firstRowEnd = newEnd[0];
            if (!SearchStartArea(0, preset.mapSize.x - 1, bottomSpawn ? 0 : lastRow, _areaSize[0], ref firstRowStart, ref firstRowEnd)) {
               ExpandStartArea(ref firstRowStart, ref firstRowEnd, bottomSpawn ? 0 : lastRow, _areaSize[0], noiseMap, preset);
            }
         }
         // If there isn't the big enough base, create it
         else {
            if (!SearchStartArea(0, preset.mapSize.x - 1, bottomSpawn ? 0 : lastRow, _areaSize[0], ref firstRowStart, ref firstRowEnd)) {
               ExpandStartArea(ref firstRowStart, ref firstRowEnd, bottomSpawn ? 0 : lastRow, _areaSize[0], noiseMap, preset);
            }
         }

         if (bottomSpawn) {
            _spawnStartsBottom[0] = firstRowStart;
            _spawnEndsBottom[0] = firstRowEnd;
         } else {
            _spawnStartsTop[0] = firstRowStart;
            _spawnEndsTop[0] = firstRowEnd;
         }

         // Avoid infinite loop - call error if detected
         int failCounter = 0;

      startPoint:
         int start = firstRowStart;
         int end = firstRowEnd;

         failCounter++;
         if (failCounter > 100) {
            D.error("Failed to generate map!");
            return;
         }

         // Check each row width and expand to meet criteria
         for (int index = 1; index < _areaSize.Length; index++) {
            if (SearchStartArea(start, end, bottomSpawn ? index : lastRow - index, _areaSize[index], ref start, ref end)) {
               if (bottomSpawn) {
                  _spawnStartsBottom[index] = start;
                  _spawnEndsBottom[index] = end;
               } else {
                  _spawnStartsTop[index] = start;
                  _spawnEndsTop[index] = end;
               }
            } else {
               ExpandStartArea(ref start, ref end, bottomSpawn ? index : lastRow - index, _areaSize[index], noiseMap, preset);
               goto startPoint;
            }
         }
      }

      // Set correct height for areas that are force to be water
      if (bottomSpawn) {
         for (int y = 0; y < _spawnStartsBottom.Length; y++) {
            for (int x = _spawnStartsBottom[y]; x <= _spawnEndsBottom[y]; x++) {
               if (noiseMap[x, y] < preset.replaceWaterHeight) {
                  noiseMap[x, y] = preset.replaceWaterHeight;
               }
            }
         }
      } else {
         for (int y = 0; y < _spawnStartsTop.Length; y++) {
            for (int x = _spawnStartsTop[y]; x <= _spawnEndsTop[y]; x++) {
               if (noiseMap[x, lastRow - y] < preset.replaceWaterHeight) {
                  noiseMap[x, lastRow - y] = preset.replaceWaterHeight;
               }
            }
         }
      }
   }

   public struct Point
   {
      public int x;
      public int y;

      public Point(int x_, int y_) {
         x = x_;
         y = y_;
      }
   }

   static bool IsAnyPathBetweenSpawns (MapGeneratorPreset preset) {

      int sizeX = preset.mapSize.x;
      int sizeY = preset.mapSize.y;

      bool[,] freeTiles = new bool[sizeX, sizeY];
      {
         for (int x_ = 0; x_ < sizeX; x_++) {
            for (int y_ = 0; y_ < sizeY; y_++) {
               freeTiles[x_, y_] = _waterTiles[x_, y_];
            }
         }
      }

      Stack<Point> tilesStack = new Stack<Point>();
      int start = _spawnStartsBottom[0];
      int end = _spawnEndsTop[0];

      int maxY = sizeY - 1;
      int y = 0;
      int x = start;

      // Starting point
      tilesStack.Push(new Point(start, y));

      while (true) {
         if (x == end && y == maxY) {
            return true;
         }
         // Move up
         if (y + 1 < sizeY && freeTiles[x, y + 1]) {
            freeTiles[x, y + 1] = false;
            y = y + 1;
            tilesStack.Push(new Point(x, y));
         }
         // Move right
         else if (x + 1 < sizeX && freeTiles[x + 1, y]) {
            freeTiles[x + 1, y] = false;
            x = x + 1;
            tilesStack.Push(new Point(x, y));
            continue;
         }
         // Move left
         else if (x - 1 >= 0 && freeTiles[x - 1, y]) {
            freeTiles[x - 1, y] = false;
            x = x - 1;
            tilesStack.Push(new Point(x, y));
            continue;
         }
         // Move down
         else if (y - 1 >= 0 && freeTiles[x, y - 1]) {
            freeTiles[x, y - 1] = false;
            y = y - 1;
            tilesStack.Push(new Point(x, y));
         }
         // Go back
         else {
            if (x == end && y == maxY) {
               return true;
            }

            if (tilesStack.Count == 0) {
               // Path not found
               break;
            }
            Point point = tilesStack.Pop();
            x = point.x;
            y = point.y;
         }
      }
      // Couldn't find any path between spawns
      return false;
   }

   static void CreatePathBetweenSpawns (MapGeneratorPreset preset, GameObject parentObject, float[,] noiseMap, int seed) {
      System.Random pseudoRandom = new System.Random(seed);

      float startTime = Time.realtimeSinceStartup;

      int sizeX = preset.mapSize.x;
      int sizeY = preset.mapSize.y;

      bool[,] freeTiles = new bool[sizeX, sizeY];
      {
         for (int x_ = 0; x_ < sizeX; x_++) {
            for (int y_ = 0; y_ < sizeY; y_++) {
               freeTiles[x_, y_] = _waterTiles[x_, y_];
            }
         }
      }

      int start = pseudoRandom.Next(_spawnStartsBottom[0], _spawnEndsBottom[0] + 1);
      int trueEnd = pseudoRandom.Next(_spawnStartsTop[0], _spawnEndsTop[0] + 1);
      // Leave one tile for land border
      start = Mathf.Clamp(start, 1, sizeX - 2);
      trueEnd = Mathf.Clamp(trueEnd, 1, sizeX - 2);
      int end = -1;
      while (end < _spawnStartsTop[0] || end > _spawnEndsTop[0]) {
         // Leave one tile for land border
         end = pseudoRandom.Next(1, sizeX - 2);
      }
      int swapEndHeight = pseudoRandom.Next((int) (sizeY * 0.5f), (int) (sizeY * 0.75f));

      // Make sure that top tile is land border, set max to one tile before top
      int maxY = sizeY - 2;

      int y = 0;
      int x = start;

      GameObject baseLayerGameObject = new GameObject("Spawn area black");
      baseLayerGameObject.transform.position = new Vector3(baseLayerGameObject.transform.position.x, baseLayerGameObject.transform.position.y, -1.0f);
      baseLayerGameObject.transform.parent = parentObject.transform;
      Tilemap baseTilemap = baseLayerGameObject.AddComponent(typeof(Tilemap)) as Tilemap;
      TilemapRenderer baseTilemapRenderer = baseLayerGameObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
      baseTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

      while (true) {
#if UNITY_EDITOR
         if (_enableDrawDebug) {
            baseTilemap.SetTile(new Vector3Int(x, y, 0), RandomMapManager.self.debugTileBlack);
         }
#endif
         if (noiseMap[x, y] < preset.replaceWaterHeight) {
            noiseMap[x, y] = preset.replaceWaterHeight;
         }

         if (end != trueEnd && y > swapEndHeight) {
            end = trueEnd;
         }

         if (x == end && y == maxY) {
            break;
         }

         // Move up
         if (y + 1 < maxY && freeTiles[x, y + 1]) {
            freeTiles[x, y + 1] = false;
            y = y + 1;
         }
         else if (x <= end && ((x + 1 < sizeX && freeTiles[x + 1, y]) || (x - 1 >= 1 && freeTiles[x - 1, y]))) {
            // Move right
            if (x + 1 < sizeX && freeTiles[x + 1, y]) {
               freeTiles[x + 1, y] = false;
               x = x + 1;
               continue;
            }
            // Move left
            else if (x - 1 >= 1 && freeTiles[x - 1, y]) {
               freeTiles[x - 1, y] = false;
               x = x - 1;
               continue;
            }
         } else if (x > end && ((x - 1 >= 1 && freeTiles[x - 1, y]) || (x + 1 < sizeX && freeTiles[x + 1, y]))) {
            // Move left
            if (x - 1 >= 1 && freeTiles[x - 1, y]) {
               freeTiles[x - 1, y] = false;
               x = x - 1;
               continue;
            }
            // Move right
            else if (x + 1 < sizeX && freeTiles[x + 1, y]) {
               freeTiles[x + 1, y] = false;
               x = x + 1;
               continue;
            }
         } 
         // Go back
         else {
            if (x == end && y == maxY) {
               break;
            }

            // Top, right, left
            SetFreeTile(x, y + 1, freeTiles, noiseMap, baseTilemap, preset);
            SetFreeTile(x + 1, y, freeTiles, noiseMap, baseTilemap, preset);
            SetFreeTile(x - 1, y, freeTiles, noiseMap, baseTilemap, preset);

            // Always can add bottom because we are never going down
            SetFreeTile(x, y - 1, freeTiles, noiseMap, baseTilemap, preset);

            // Move in random direction (up or right/left)
            if (pseudoRandom.Next(0, 2) == 0 && y + 1 <= maxY) {
               y++;
               freeTiles[x, y] = false;
            } else {
               if (x < end) {
                  x++;
                  freeTiles[x, y] = false;
               } else if (x - 1 >= 1) {
                  x--;
                  freeTiles[x, y] = false;
               }
            }

            // Top-left, top-right, bottom-left, bottom-right (cannot move to those)
            SetFreeTile(x + 1, y + 1, freeTiles, noiseMap, baseTilemap, preset, false);
            SetFreeTile(x + 1, y - 1, freeTiles, noiseMap, baseTilemap, preset, false);
            SetFreeTile(x - 1, y - 1, freeTiles, noiseMap, baseTilemap, preset, false);
            SetFreeTile(x - 1, y + 1, freeTiles, noiseMap, baseTilemap, preset, false);

            // Randomly placed water tiles nearby to improve path look
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x + 2, y + 1, freeTiles, noiseMap, baseTilemap, preset, false);
               SetFreeTile(x + 2, y - 1, freeTiles, noiseMap, baseTilemap, preset, false);
               SetFreeTile(x + 2, y, freeTiles, noiseMap, baseTilemap, preset, false);
            }
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x - 2, y + 1, freeTiles, noiseMap, baseTilemap, preset, false);
               SetFreeTile(x - 2, y - 1, freeTiles, noiseMap, baseTilemap, preset, false);
               SetFreeTile(x - 2, y, freeTiles, noiseMap, baseTilemap, preset, false);
            }
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x - 2, y, freeTiles, noiseMap, baseTilemap, preset, false);
            }
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x + 2, y, freeTiles, noiseMap, baseTilemap, preset, false);
            }

            if (Time.realtimeSinceStartup - startTime > 1.0f) {
               D.error("Creating random map: Time out");
               return;
            }
         }
      }
   }

   static void CreatePathBetweenPoints (Point startPoint, Point endPoint, MapGeneratorPreset preset, GameObject parentObject, float[,] noiseMap, bool[,] freeTiles_, int seed, bool bottom) {
      System.Random pseudoRandom = new System.Random(seed);

      float startTime = Time.realtimeSinceStartup;

      bool[,] freeTiles = new bool[preset.mapSize.x, preset.mapSize.y];
      {
         for (int x_ = 0; x_ < preset.mapSize.x; x_++) {
            for (int y_ = 0; y_ < preset.mapSize.y; y_++) {
               freeTiles[x_, y_] = freeTiles_[x_, y_];
            }
         }
      }

      int y = startPoint.y;
      int x = startPoint.x;

      int end = endPoint.x;

      int sizeX = preset.mapSize.x - 2;
      int sizeY = preset.mapSize.y;
      int maxY = Mathf.Clamp(endPoint.y, 1, preset.mapSize.y - 3);

      while (true) {
         if (noiseMap[x, y] < preset.replaceWaterHeight) {
            noiseMap[x, y] = preset.replaceWaterHeight;
         }

         if (x == end && y == maxY) {
            break;
         }

         // Move up or down
         if (bottom == false && y + 1 < maxY && freeTiles[x, y + 1]) {
            freeTiles[x, y + 1] = false;
            y = y + 1;
         }
         else if (bottom == true && y - 1 >= maxY && freeTiles[x, y - 1]) {
            freeTiles[x, y - 1] = false;
            y = y - 1;
         }
         else if (x <= end && ((x + 1 < sizeX && freeTiles[x + 1, y]) || (x - 1 >= 1 && freeTiles[x - 1, y]))) {
            // Move right
            if (x + 1 < sizeX && freeTiles[x + 1, y]) {
               freeTiles[x + 1, y] = false;
               x = x + 1;
               continue;
            }
            // Move left
            else if (x - 1 >= 1 && freeTiles[x - 1, y]) {
               freeTiles[x - 1, y] = false;
               x = x - 1;
               continue;
            }
         } else if (x > end && ((x - 1 >= 1 && freeTiles[x - 1, y]) || (x + 1 < sizeX && freeTiles[x + 1, y]))) {
            // Move left
            if (x - 1 >= 1 && freeTiles[x - 1, y]) {
               freeTiles[x - 1, y] = false;
               x = x - 1;
               continue;
            }
            // Move right
            else if (x + 1 < sizeX && freeTiles[x + 1, y]) {
               freeTiles[x + 1, y] = false;
               x = x + 1;
               continue;
            }
         }
         // Go back
         else {
            if (x == end && y == maxY) {
               break;
            }

            // Top, right, left
            SetFreeTile(x, y + 1, freeTiles, noiseMap, null, preset, false);
            SetFreeTile(x + 1, y, freeTiles, noiseMap, null, preset, false);
            SetFreeTile(x - 1, y, freeTiles, noiseMap, null, preset, false);

            // Always can add bottom because we are never going down
            SetFreeTile(x, y - 1, freeTiles, noiseMap, null, preset, false);

            // Move in random direction (up or right/left)
            if (pseudoRandom.Next(0, 2) == 0 && (bottom ? (y - 1 >= maxY) : (y + 1 <= maxY))) {
               if (bottom)
                  y--;
               else
                  y++;
               freeTiles[x, y] = false;
            } else {
               if (x < end) {
                  x++;
                  freeTiles[x, y] = false;
               } else if (x - 1 >= 1) {
                  x--;
                  freeTiles[x, y] = false;
               }
            }

            // Top-left, top-right, bottom-left, bottom-right (cannot move to those)
            SetFreeTile(x + 1, y + 1, freeTiles, noiseMap, null, preset, false);
            SetFreeTile(x + 1, y - 1, freeTiles, noiseMap, null, preset, false);
            SetFreeTile(x - 1, y - 1, freeTiles, noiseMap, null, preset, false);
            SetFreeTile(x - 1, y + 1, freeTiles, noiseMap, null, preset, false);

            // Randomly placed water tiles nearby to improve path look
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x + 2, y + 1, freeTiles, noiseMap, null, preset, false);
               SetFreeTile(x + 2, y - 1, freeTiles, noiseMap, null, preset, false);
               SetFreeTile(x + 2, y, freeTiles, noiseMap, null, preset, false);
            }
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x - 2, y + 1, freeTiles, noiseMap, null, preset, false);
               SetFreeTile(x - 2, y - 1, freeTiles, noiseMap, null, preset, false);
               SetFreeTile(x - 2, y, freeTiles, noiseMap, null, preset, false);
            }
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x - 2, y, freeTiles, noiseMap, null, preset, false);
            }
            if (pseudoRandom.Next(0, 10) >= 8) {
               SetFreeTile(x + 2, y, freeTiles, noiseMap, null, preset, false);
            }

            if (Time.realtimeSinceStartup - startTime > 1f) {
               D.error("CreatePathBetweenPoints: Time out");
               return;
            }
         }
      }

   }

   static void SetFreeTile(int x, int y, bool[,] freeTiles, float[,] noiseMap, Tilemap tilemap, MapGeneratorPreset preset, bool freeTile = true) {
      if (preset == null) {
         D.error("Empty preset passed");
      }

      if (x < preset.mapSize.x - 1 && x >= 1 && y < preset.mapSize.y && y >= 1) {
         if (freeTile) {
            freeTiles[x, y] = true;
         }
         _waterTiles[x, y] = true;
         noiseMap[x, y] = preset.replaceWaterHeight;
#if UNITY_EDITOR
         if (_enableDrawDebug && tilemap) {
            tilemap.SetTile(new Vector3Int(x, y, 0), RandomMapManager.self.debugTileBlack);
         }
#endif
      }
   }

   static void DrawStartAreaTiles (bool bottomSpawn, GameObject parentObject, int mapHeight) {
      GameObject baseLayerGameObject = new GameObject("Spawn area" + (bottomSpawn ? " red" : " blue"));
      baseLayerGameObject.transform.position = new Vector3(baseLayerGameObject.transform.position.x, baseLayerGameObject.transform.position.y, -1.0f);
      baseLayerGameObject.transform.parent = parentObject.transform;
      Tilemap baseTilemap = baseLayerGameObject.AddComponent(typeof(Tilemap)) as Tilemap;
      TilemapRenderer baseTilemapRenderer = baseLayerGameObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
      baseTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

      if (bottomSpawn) {
         for (int y = 0; y < _spawnStartsBottom.Length; y++) {
            for (int x = _spawnStartsBottom[y]; x <= _spawnEndsBottom[y]; x++) {
               baseTilemap.SetTile(new Vector3Int(x, y, 0), RandomMapManager.self.debugTileBlue);
            }
         }
      } else {
         for (int y = 0; y < _spawnStartsTop.Length; y++) {
            for (int x = _spawnStartsTop[y]; x <= _spawnEndsTop[y]; x++) {
               baseTilemap.SetTile(new Vector3Int(x, mapHeight - y - 1, 0), RandomMapManager.self.debugTileRed);
            }
         }
      }
   }

   // Search 0 to map size
   static bool SearchAllStartAreas (int mapSizeX, int y, int minWidth, ref List<int> newStart, ref List<int> newEnd) {
      int startTmp = 0;
      int endTmp = 0;

      for (int x = 0; x < mapSizeX; x++) {
         if (_waterTiles[x, y]) {
            endTmp = x;
         } else {
            if (endTmp - startTmp > minWidth - 1) {
               newStart.Add(startTmp);
               newEnd.Add(endTmp);
            }
            startTmp = x + 1;
            endTmp = x + 1;
         }
      }

      if (endTmp - startTmp > minWidth - 1) {
         newStart.Add(startTmp);
         newEnd.Add(endTmp);
      }

      List<int> size = new List<int>();
      for (int i = 0; i < newStart.Count; i++) {
         size.Add(newEnd[i] - newStart[i]);
      }

      for (int i = 0; i < size.Count; i++) {
         int maxIndex = size.IndexOf(size.Max());
         size[maxIndex] = 0;

         {
            int tmp = newStart[i];
            newStart[i] = newStart[maxIndex];
            newStart[maxIndex] = tmp;
         }
         {
            int tmp = newEnd[i];
            newEnd[i] = newEnd[maxIndex];
            newEnd[maxIndex] = tmp;
         }
      }

      return true;
   }

   // Inclusive-in, inclusive-out
   static bool SearchStartArea(int start, int end, int y, int minWidth, ref int newStart, ref int newEnd) {
      int max = 0;
      int current = 0;
      int startTmp = start;

      for (int x = start; x <= end; x++) {
         if (_waterTiles[x, y]) {
            current++;
            if (current > max) {
               newStart = startTmp;
               newEnd = x;
               max = current;
            }
         } else {
            startTmp = x + 1;
            current = 0;
         }
      }

      return (newEnd - newStart >= minWidth - 1);
   }

   // Inclusive-in, inclusive-out
   static void ExpandStartArea (ref int start, ref int end, int y, int minWidth, float[,] noiseMap, MapGeneratorPreset preset) {
      int width = end - start + 1;

      if (width > minWidth) {
         int maxShift = width - minWidth;
         int shift = Random.Range(-maxShift, maxShift + 1);
         if (shift > 0) {
            start += shift;
         } else {
            end += shift;
         }
      } else {
         // Shift start
         if (Random.Range(0, 2) == 0) {
            start -= Mathf.Abs(width - minWidth);
            if (start < 0) {
               end += Mathf.Abs(start);
               start = 0;
            }
         } 
         // Shift end
         else {
            end += Mathf.Abs(width - minWidth);
            if (end >= preset.mapSize.x) {
               start -= (Mathf.Abs(preset.mapSize.x - end) + 1);
               end = preset.mapSize.x - 1;
            }
         }
      }

      start = Mathf.Clamp(start, 0, preset.mapSize.x - 1);
      end = Mathf.Clamp(end, 0, preset.mapSize.x - 1);

      for (int x = start; x <= end; x++) {
         _waterTiles[x, y] = true;
         noiseMap[x, y] = preset.replaceWaterHeight;
      }
   }

   static void GenerateLandBorder (float[,] noiseMap, MapGeneratorPreset preset) {
      // Set bottom tiles
      int bottomStart = _spawnStartsBottom[0];
      int bottomEnd = _spawnEndsBottom[0];
      for (int x = 0; x < preset.mapSize.x; x++) {
         if (x < bottomStart || x > bottomEnd) {
            noiseMap[x, 0] = preset.landBorderHeight;
         }
      }
      // Set top tiles
      int topStart = _spawnStartsTop[0];
      int topEnd = _spawnEndsTop[0];
      for (int x = 0; x < preset.mapSize.x; x++) {
         if (x < topStart || x > topEnd) {
            noiseMap[x, preset.mapSize.y - 1] = preset.landBorderHeight;
         }
      }
      // Set left tiles
      {
         // Prepare list to check if tiles of start area are taken
         List<int> spawnLefts = new List<int>(_areaSize.Length * 2);

         // Iterate over start area tiles and find taken ones
         for (int i = 0; i < _spawnStartsBottom.Length; i++) {
            if (_spawnStartsBottom[i] == 0) {
               spawnLefts.Add(i);
            }
         }
         for (int i = 0; i < _spawnStartsTop.Length; i++) {
            if (_spawnStartsTop[i] == 0) {
               spawnLefts.Add(preset.mapSize.y - 1 - i);
            }
         }

         // Set left tiles with checking taken tiles in spawn area
         for (int y = 0; y < preset.mapSize.y; y++) {
            if (spawnLefts.Contains(y) == false) {
               noiseMap[0, y] = preset.landBorderHeight;
            }
         }
      }
      // Set right tiles
      {
         // Prepare list to check if tiles of start area are taken
         List<int> spawnRights = new List<int>(_areaSize.Length * 2);

         // Iterate over start area tiles and find taken ones
         for (int i = 0; i < _spawnEndsBottom.Length; i++) {
            if (_spawnEndsBottom[i] == preset.mapSize.y - 1) {
               spawnRights.Add(i);
            }
         }
         for (int i = 0; i < _spawnEndsTop.Length; i++) {
            if (_spawnEndsTop[i] == preset.mapSize.y - 1) {
               spawnRights.Add(preset.mapSize.y - 1 - i);
            }
         }

         // Set right tiles with checking taken tiles in spawn area
         for (int y = 0; y < preset.mapSize.y; y++) {
            if (spawnRights.Contains(y) == false) {
               noiseMap[preset.mapSize.x - 1, y] = preset.landBorderHeight;
            }
         }
      }
   }

   static void FindClosedArea (float[,] noiseMap, MapGeneratorPreset preset, GameObject parentObject, int seed) {
      // Recreate water tiles array after changing map
      PrepareWaterTiles(preset, noiseMap);

      // Prepare array with tiles to mark inaccessible areas
      bool[,] freeTiles = new bool[preset.mapSize.x, preset.mapSize.y];
      for (int x = 0; x < preset.mapSize.x; x++) {
         for (int y = 0; y < preset.mapSize.y; y++) {
            freeTiles[x, y] = _waterTiles[x, y];
         }
      }

      // Mark closed areas
      MarkClosedAreas(freeTiles, preset);

      // Pseudo-random number generator based on seed
      System.Random pseudoRandom = new System.Random(seed);

      // Calculate areas size
      List<Point> allPointsLast = new List<Point>();
      for (int x = 0; x < preset.mapSize.x; x++) {
         for (int y = 0; y < preset.mapSize.y; y++) {
            if (freeTiles[x, y]) {
               List<Point> allPoints = new List<Point>();
               // Create path to only big areas
               if (CalculateClosedAreaSize(freeTiles, preset, new Point(x, y), ref allPoints) > _minClosedPathSize) {

                  // Path from random area point to top/bottom spawns
                  CreatePathBetweenPoints(allPoints.ChooseRandom(pseudoRandom.Next()), new Point(_spawnEndsTop[0], preset.mapSize.y - 2), preset, parentObject, noiseMap, freeTiles, seed, false);
                  CreatePathBetweenPoints(allPoints.ChooseRandom(pseudoRandom.Next()), new Point(_spawnEndsBottom[0], 1), preset, parentObject, noiseMap, freeTiles, seed, true);

                  // Create path between two closed areas
                  if (allPointsLast.Count > 0) {
                     Point first = allPoints.ChooseRandom();
                     Point second = allPointsLast.ChooseRandom();
                     CreatePathBetweenPoints(first, second, preset, parentObject, noiseMap, freeTiles, seed, first.y > second.y);
                     allPointsLast.Clear();
                  } else {
                     allPointsLast = allPoints;
                  }
               }
            }
         }
      }
   }
   
   static void MarkClosedAreas (bool[,] freeTiles, MapGeneratorPreset preset) {
      List<Point> allPoints = new List<Point>();
      CalculateClosedAreaSize(freeTiles, preset, new Point(_spawnStartsBottom[0], 0), ref allPoints);
   }
   
   static int CalculateClosedAreaSize (bool[,] freeTiles, MapGeneratorPreset preset, Point startPoint, ref List<Point> allPoints) {
      Stack<Point> points = new Stack<Point>();
      points.Push(startPoint);
      allPoints.Add(startPoint);
      int size = 0;

      while (points.Count > 0) {
         Point point = points.Pop();
         allPoints.Add(point);

         freeTiles[point.x, point.y] = false;
         // Check bottom
         if (point.y - 1 >= 0 && freeTiles[point.x, point.y - 1]) {
            points.Push(new Point(point.x, point.y - 1));
            freeTiles[point.x, point.y - 1] = false;
            size++;
         }
         // Check left
         if (point.x - 1 >= 0 && freeTiles[point.x - 1, point.y]) {
            points.Push(new Point(point.x - 1, point.y));
            freeTiles[point.x - 1, point.y] = false;
            size++;
         }
         // Check right
         if (point.x + 1 < preset.mapSize.x && freeTiles[point.x + 1, point.y]) {
            points.Push(new Point(point.x + 1, point.y));
            freeTiles[point.x + 1, point.y] = false;
            size++;
         }
         // Check top
         if (point.y + 1 < preset.mapSize.y && freeTiles[point.x, point.y + 1]) {
            points.Push(new Point(point.x, point.y + 1));
            freeTiles[point.x, point.y + 1] = false;
            size++;
         }
      }

      return size;
   }

   /// <summary>
   /// set tiles on preset
   /// </summary>
   /// <param name="preset">preset</param>
   static void SetTiles (MapGeneratorPreset preset) {
      // Updates tiles only when playing in editor
#if UNITY_EDITOR

      // Time improved due to data locality and not searching every time through all paths
      if (_assetsPath == null) {
         _assetsPath = new List<string>();
         foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
            // We only care about our map assets
            if (assetPath.StartsWith(_tilesPath)) {
               _assetsPath.Add(assetPath);
            }
         }
      }

      foreach (var layer in preset.layers) {

         SetTileFromProject(ref layer.tile, layer.biome, layer.tilePrefix, layer.tileSuffix, _assetsPath);

         foreach (var border in layer.borders) {
            SetTileFromProject(ref border.borderTile, layer.biome, border.tilePrefix, border.tileSuffix, _assetsPath);
         }

         foreach (var corner in layer.corners) {
            SetTileFromProject(ref corner.cornerTile, layer.biome, corner.tilePrefix, corner.tileSuffix, _assetsPath);
         }

         foreach (var objectLayer in layer.objectLayers) {
            SetTileFromProject(ref objectLayer.tile, layer.biome, objectLayer.tilePrefix, objectLayer.tileSuffix, _assetsPath);
         }
      }

      SetTileFromProject(ref preset.river.riverTile, preset.river.biome, preset.river.tilePrefix, preset.river.tileSuffix, _assetsPath);

      foreach (var border in preset.river.riverBorders) {
         SetTileFromProject(ref border.borderTile, preset.river.biome, border.tilePrefix, border.tileSuffix, _assetsPath);
      }

#endif
   }

   /// <summary>
   /// Search the tiles on the project 
   /// </summary>
   /// <param name="tileToSet">tile</param>
   /// <param name="biome">tile biome</param>
   /// <param name="tilePrefix">prefix</param>
   /// <param name="tileSuffix">suffix</param>

   static void SetTileFromProject (ref Tile tileToSet, Biome.Type biome, string tilePrefix, string tileSuffix, List<string> assetsPath) {
#if UNITY_EDITOR
      // Check preconditions only once
      string biomeName = System.Enum.GetName(typeof(Biome.Type), biome).ToLower();
      bool biomeIsEmpty = biomeName.Contains("none");
      bool tilePrefixIsEmpty = string.IsNullOrEmpty(tilePrefix);
      bool tileSuffixIsEmpty = string.IsNullOrEmpty(tileSuffix);

      // If all conditions are met - first found tiled will be return, that's an error
      if (biomeIsEmpty && tilePrefixIsEmpty && tileSuffixIsEmpty) {
         D.error("No enough data to get tile");
      }

      string currentName = tilePrefix + biomeName + tileSuffix;
      // Already correct tile is set
      if (tileToSet.name.ToLower() == currentName.ToLower()) {
         return;
      }

      D.log("Tile: " + tileToSet.name + "; generated: " + tilePrefix + biomeName + tileSuffix);

      // Iterate over paths that we are only interested in (map related)
      foreach (string assetPath in assetsPath) {
         // Get the tile
         Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
         if (tile) {
            // check tile name
            if (!biomeIsEmpty) {
               if (!tile.name.Contains(biomeName)) {
                  continue;
               }
            }
            if (!tilePrefixIsEmpty) {
               if (!tile.name.StartsWith(tilePrefix)) {
                  continue;
               }
            }
            if (!tileSuffixIsEmpty) {
               if (!tile.name.EndsWith(tileSuffix)) {
                  continue;
               }
            }

            tileToSet = tile;
            return;
         }
      }
#endif
   }

   /// <summary>
   /// set base tiles on the paramref name="tilemap"  using the paramref name="noiseMap"
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="noiseMap">noise</param>
   /// <param name="i">index</param>
   /// <param name="tilemap">tilemap used</param>
   static void SetBaseLayers (MapGeneratorPreset preset, float[,] noiseMap, int i, Tilemap tilemap, bool[,] availableTiles, Node[,] grid) {
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
                  if (i + 1 < preset.layers.Length) {
                     if (currentHeight > preset.layers[i + 1].height) {
                        availableTiles[x, y] = true;

                        // river
                        if (preset.layers[i].name == preset.river.layerToPlaceRiver) {
                           grid[x, y].nodeType = NodeType.Land;
                        }
                     }
                  } else {
                     availableTiles[x, y] = true;
                  }
               }

               // river
               if (preset.layers[i].name == preset.river.layerToPlaceRiver) {
                  if (currentHeight > preset.layers[i].height) {
                     grid[x, y].nodeType = NodeType.Water;
                  }
               }
            } else {
               if (currentHeight <= layersHeight[i]) {
                  tilemap.SetTile(new Vector3Int(x, y, 0), preset.layers[i].tile);
                  //tilemap.SetTile(new Vector3Int(-x + preset.mapSize.x / 2, -y + preset.mapSize.y / 2, 0), preset.layers[i].tile);

                  if (i + 1 < layersHeight.Length) {
                     if (currentHeight > layersHeight[i + 1]) {
                        availableTiles[x, y] = true;

                        // river
                        if (preset.layers[i].name == preset.river.layerToPlaceRiver) {
                           grid[x, y].nodeType = NodeType.Land;
                        }
                     }
                  } else {
                     availableTiles[x, y] = true;
                  }
               }

               // river
               if (preset.layers[i].name == preset.river.layerToPlaceRiver) {
                  if (currentHeight > preset.layers[i].height) {
                     grid[x, y].nodeType = NodeType.Water;
                  }
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
   static void SetBorder (MapGeneratorPreset preset, int i, Tilemap checkedTilemap, Tilemap setTilemap, bool[,] availableTiles, Node[,] grid) {
      for (int x = 0; x < preset.mapSize.x; x++) {
         for (int y = 0; y < preset.mapSize.y; y++) {
            Vector3Int cellPos = new Vector3Int(x, y, 0);

            var tileSprite = checkedTilemap.GetSprite(cellPos);

            if (tileSprite) {
               foreach (var border in preset.layers[i].borders) {
                  switch (border.borderDirection) {
                     // Four directions
                     case BorderDirection.N_E_S_W: // all directions
                                                   //                                                      right   left    up    down
                                                   //                                                        E      W      N        S
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, false, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;

                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }

                        }
                        break;

                     // Three directions
                     case BorderDirection.N_E_W: // Top Lateral
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, false, true)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.E_S_W: // Down Lateral
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, true, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.N_S_W: // Left Top Down
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, false, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.N_E_S: // Right Top Down
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, false, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     // Two directions
                     case BorderDirection.N_S: // Top Down
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, false, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.E_W: // Lateral
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, true, true)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.N_W: // Top Left
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, false, true)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.S_W: // Down Left
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, true, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.N_E: // Top Right
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, false, true)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case BorderDirection.S_E: // Down Right
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, true, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     // One direction
                     case BorderDirection.N: // Top
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, false, true)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.LandBorder_N;
                           }
                        }
                        break;

                     case BorderDirection.S: // Down
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, true, false)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.LandBorder_S;
                           }
                        }
                        break;

                     case BorderDirection.E: // Right
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, true, true)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.LandBorder_E;
                           }
                        }
                        break;

                     case BorderDirection.W: // Left
                        if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, true, true)) {
                           setTilemap.SetTile(cellPos, border.borderTile);
                           availableTiles[x, y] = false;
                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.LandBorder_W;
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
   /// create rivers on the map
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="grid">a* grid</param>
   /// <param name="gridLayers">grid object</param>
   /// <param name="i">i</param>
   /// <param name="availableTiles">tiles to place objects</param>
   static void SetRiver (MapGeneratorPreset preset, Node[,] grid, GameObject gridLayers, int i, bool[,] availableTiles) {
      if (preset.river.numberOfAttempts > 0 && preset.layers[i].name == preset.river.layerToPlaceRiver) {
         GameObject riverLayerObject = new GameObject(preset.layers[i].name + " River");
         riverLayerObject.transform.position = new Vector3(riverLayerObject.transform.position.x, riverLayerObject.transform.position.y, .0001f);
         riverLayerObject.transform.parent = gridLayers.transform;
         Tilemap riverTilemap = riverLayerObject.AddComponent(typeof(Tilemap)) as Tilemap;
         TilemapRenderer riverTilemapRenderer = riverLayerObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
         riverTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

         System.Random pseudoRandom = new System.Random(preset.seed);

         for (int j = 0; j < preset.river.numberOfAttempts;) {
            List<Vector3Int> finalPath = new List<Vector3Int>();
            List<Node> path = new List<Node>();

            path = FindPath(preset, grid, pseudoRandom);

            if (path != null) {

               foreach (var node in path) {
                  if (node.nodeType != NodeType.Water) {
                     finalPath.Add(new Vector3Int(node.gridX, node.gridY, 0));

                     if (grid[node.gridX, node.gridY].nodeType == NodeType.Land) {
                        grid[node.gridX, node.gridY].nodeType = NodeType.Wall;
                     }
                     availableTiles[node.gridX, node.gridY] = false;
                  }
               }
               foreach (var position in finalPath) {
                  riverTilemap.SetTile(position, preset.river.riverTile);
               }
               j++;


            }
         }
         SetRiverDirection(preset, i, riverTilemap, grid);
      }
   }

   /// <summary>
   /// change the tile river to the correct direction
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="i">i</param>
   /// <param name="setTilemap">tilemap</param>
   /// <param name="grid">a* grid</param>
   static void SetRiverDirection (MapGeneratorPreset preset, int i, Tilemap setTilemap, Node[,] grid) {
      for (int x = 0; x < preset.mapSize.x; x++) {
         for (int y = 0; y < preset.mapSize.y; y++) {
            Vector3Int cellPos = new Vector3Int(x, y, 0);

            var tileSprite = setTilemap.GetSprite(cellPos);

            if (tileSprite) {
               foreach (var riverBorder in preset.river.riverBorders) {
                  switch (riverBorder.riverDirection) {
                     case RiverDirection.E_S:
                        //                                                  right   left  up   down
                        //                                                  E      W     N     S
                        if (CheckExistingBorderDirections(setTilemap, x, y, true, false, false, true) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.EBorder_W:
                        if (grid[x, y].nodeType == NodeType.LandBorder_E) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.E_WBorder:
                        if (grid[x, y].nodeType == NodeType.LandBorder_W) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.E_W:
                        if (CheckExistingBorderDirections(setTilemap, x, y, true, true, false, false) && grid[x, y].nodeType == NodeType.Wall) //
                        {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.S_W:
                        if (CheckExistingBorderDirections(setTilemap, x, y, false, true, false, true) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.NBorder_S:
                        if (grid[x, y].nodeType == NodeType.LandBorder_N) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);

                        }
                        break;

                     case RiverDirection.N_SBorder:
                        if (grid[x, y].nodeType == NodeType.LandBorder_S) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.N_S:
                        if (CheckExistingBorderDirections(setTilemap, x, y, false, false, true, true) && grid[x, y].nodeType == NodeType.Wall) // && grid[x, y].nodeType == NodeType.Land
                        {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);

                        }
                        break;

                     case RiverDirection.N_W:
                        if (CheckExistingBorderDirections(setTilemap, x, y, false, true, true, false) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.N_E:
                        if (CheckExistingBorderDirections(setTilemap, x, y, true, false, true, false) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.N_E_S:

                        if (CheckExistingBorderDirections(setTilemap, x, y, true, false, true, true) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.N_S_W:
                        if (CheckExistingBorderDirections(setTilemap, x, y, false, true, true, true) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.E_S_W:
                        if (CheckExistingBorderDirections(setTilemap, x, y, true, true, false, true) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.N_E_W:
                        if (CheckExistingBorderDirections(setTilemap, x, y, true, true, true, false) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
                        }
                        break;

                     case RiverDirection.N_E_S_W:

                        if (CheckExistingBorderDirections(setTilemap, x, y, true, true, true, true) && grid[x, y].nodeType == NodeType.Wall) {
                           setTilemap.SetTile(cellPos, riverBorder.borderTile);
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
   /// <param name="E">tile on the right</param>
   /// <param name="W">tile on the left</param>
   /// <param name="N">tile above</param>
   /// <param name="S">tile below</param>
   /// <returns></returns>
   static bool CheckExistingBorderDirections (Tilemap checkedTilemap, int x, int y, bool E, bool W, bool N, bool S) {
      if (CheckExistingTile(checkedTilemap, x + 1, y) == E && CheckExistingTile(checkedTilemap, x - 1, y) == W && CheckExistingTile(checkedTilemap, x, y + 1) == N && CheckExistingTile(checkedTilemap, x, y - 1) == S) {
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
   static bool CheckExistingTile (Tilemap tilemap, int x, int y) {
      if (tilemap.GetSprite(new Vector3Int(x, y, 0))) {
         return true;
      } else {
         return false;
      }
   }

   /// <summary>
   /// set the corners of the layer
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="i">i</param>
   /// <param name="parent">grid object</param>
   /// <param name="baseTilemap">base tilmap</param>
   /// <param name="borderTilemap">border tilemap</param>
   /// <param name="availableTiles">tiles to place objects</param>
   /// <param name="grid">a* grid</param>
   static void SetCorner (MapGeneratorPreset preset, int i, GameObject parent, Tilemap baseTilemap, Tilemap borderTilemap, bool[,] availableTiles, Node[,] grid) {
      BorderDirection[] bordersN = {
                BorderDirection.N,
                BorderDirection.N_E_W,
                BorderDirection.N_S_W,
                BorderDirection.N_E_S,
                BorderDirection.N_S,
                BorderDirection.N_W,
                BorderDirection.N_E
            };
      BorderDirection[] bordersE = {
                BorderDirection.E,
                BorderDirection.N_E_W,
                BorderDirection.E_S_W,
                BorderDirection.N_E_S,
                BorderDirection.E_W,
                BorderDirection.N_E,
                BorderDirection.S_E
            };
      BorderDirection[] bordersS = {
                BorderDirection.S,
                BorderDirection.E_S_W,
                BorderDirection.N_S_W,
                BorderDirection.N_E_S,
                BorderDirection.N_S,
                BorderDirection.S_W,
                BorderDirection.S_E
            };
      BorderDirection[] bordersW = {
                BorderDirection.W,
                BorderDirection.N_E_W,
                BorderDirection.E_S_W,
                BorderDirection.N_S_W,
                BorderDirection.E_W,
                BorderDirection.N_W,
                BorderDirection.S_W
            };

      float j = 1;
      foreach (var corner in preset.layers[i].corners) {
         GameObject cornerLayerObject = new GameObject(preset.layers[i].name + " " + System.Enum.GetName(typeof(CornerDirection), corner.cornerDirection));
         cornerLayerObject.transform.position = new Vector3(cornerLayerObject.transform.position.x, cornerLayerObject.transform.position.y, (float) (preset.layers.Length - i) / 10 - (float) i / 100);
         cornerLayerObject.transform.parent = parent.transform;
         Tilemap cornerTilemap = cornerLayerObject.AddComponent(typeof(Tilemap)) as Tilemap;
         TilemapRenderer cornerTilemapRenderer = cornerLayerObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
         cornerTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

         for (int x = 0; x < preset.mapSize.x; x++) {
            for (int y = 0; y < preset.mapSize.y; y++) {
               Vector3Int cellPos = new Vector3Int(x, y, 0);
               var tileSprite = baseTilemap.GetSprite(cellPos);

               if (tileSprite) {
                  switch (corner.cornerDirection) {
                     case CornerDirection.NE:
                        if (CompareBorderTile(preset.layers[i].borders, borderTilemap, x + 1, y, bordersN) && CompareBorderTile(preset.layers[i].borders, borderTilemap, x, y + 1, bordersE)) {
                           cornerTilemap.SetTile(cellPos, corner.cornerTile);
                           availableTiles[x, y] = false;

                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case CornerDirection.SE:
                        if (CompareBorderTile(preset.layers[i].borders, borderTilemap, x + 1, y, bordersS) && CompareBorderTile(preset.layers[i].borders, borderTilemap, x, y - 1, bordersE)) {
                           cornerTilemap.SetTile(cellPos, corner.cornerTile);
                           availableTiles[x, y] = false;

                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case CornerDirection.SW:
                        if (CompareBorderTile(preset.layers[i].borders, borderTilemap, x - 1, y, bordersS) && CompareBorderTile(preset.layers[i].borders, borderTilemap, x, y - 1, bordersW)) {
                           cornerTilemap.SetTile(cellPos, corner.cornerTile);
                           availableTiles[x, y] = false;

                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;

                     case CornerDirection.NW:
                        if (CompareBorderTile(preset.layers[i].borders, borderTilemap, x - 1, y, bordersN) && CompareBorderTile(preset.layers[i].borders, borderTilemap, x, y + 1, bordersW)) {
                           cornerTilemap.SetTile(cellPos, corner.cornerTile);
                           availableTiles[x, y] = false;

                           if (grid[x, y] != null && preset.layers[i].name == preset.river.layerToPlaceRiver) {
                              grid[x, y].nodeType = NodeType.Wall;
                           }
                        }
                        break;
                  }
               }
            }
         }
         j++;
      }
   }

   /// <summary>
   /// compare tile sprite
   /// </summary>
   /// <param name="borders">borders</param>
   /// <param name="borderTilemap">borders tilemap</param>
   /// <param name="x">x position</param>
   /// <param name="y">y position</param>
   /// <param name="borderTypes">border types</param>
   /// <returns></returns>
   static bool CompareBorderTile (Border[] borders, Tilemap borderTilemap, int x, int y, BorderDirection[] borderTypes) {
      foreach (var border in borders) {
         foreach (var borderType in borderTypes) {
            if (border.borderDirection == borderType) {
               Vector3Int cellPos = new Vector3Int(x, y, 0);

               var tileSprite = borderTilemap.GetSprite(cellPos);

               if (tileSprite) {
                  if (tileSprite == border.borderTile.sprite) {
                     return true;
                  }
               }
            }
         }
      }
      return false;
   }

   /// <summary>
   /// find path on the map 
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="grid">a* grid</param>
   /// <param name="pseudoRandom">random</param>
   /// <returns></returns>
   static List<Node> FindPath (MapGeneratorPreset preset, Node[,] grid, System.Random pseudoRandom) {
      List<Node> OpenList = new List<Node>();
      HashSet<Node> ClosedList = new HashSet<Node>();

      Vector2Int startPosition = Vector2Int.zero;

      do {
         startPosition = new Vector2Int(pseudoRandom.Next(0, preset.mapSize.x), pseudoRandom.Next(0, preset.mapSize.y));

      } while (grid[startPosition.x, startPosition.y].nodeType != NodeType.Water);

      Vector2Int targetPosition = Vector2Int.one;
      do {
         targetPosition = new Vector2Int(pseudoRandom.Next(0, preset.mapSize.x), pseudoRandom.Next(0, preset.mapSize.y));

      } while (grid[targetPosition.x, targetPosition.y].nodeType != NodeType.Water);

      Node startNode = grid[startPosition.x, startPosition.y];
      Node targetNode = grid[targetPosition.x, targetPosition.y];

      OpenList.Add(startNode);

      while (OpenList.Count > 0) {
         Node currentNode = OpenList[0];
         for (int i = 1; i < OpenList.Count; i++) {
            if (OpenList[i].fCost <= currentNode.fCost && OpenList[i].hCost < currentNode.hCost) {
               currentNode = OpenList[i];
            }
         }
         OpenList.Remove(currentNode);
         ClosedList.Add(currentNode);

         if (currentNode == targetNode) {
            return GetFinalPath(startNode, targetNode);
         }

         foreach (Node NeighborNode in GetNeighboringNodes(currentNode, grid, preset)) {
            if (NeighborNode.nodeType == NodeType.Wall || ClosedList.Contains(NeighborNode)) {
               continue;
            }
            int moveCost = currentNode.gCost + GetManhattenDistance(currentNode, NeighborNode);

            if (moveCost < NeighborNode.gCost || !OpenList.Contains(NeighborNode)) {
               NeighborNode.gCost = moveCost;
               NeighborNode.hCost = GetManhattenDistance(NeighborNode, targetNode);
               NeighborNode.parent = currentNode;

               if (!OpenList.Contains(NeighborNode)) {
                  OpenList.Add(NeighborNode);
               }
            }
         }
      }

      return null;
   }

   /// <summary>
   /// Manhatten Distance from two nodes
   /// </summary>
   /// <param name="nodeA">node a</param>
   /// <param name="nodeB">node b</param>
   /// <returns></returns>
   static int GetManhattenDistance (Node nodeA, Node nodeB) {
      int iX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
      int iY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

      return iX + iY;
   }

   /// <summary>
   /// get all neighboring nodes from on node
   /// </summary>
   /// <param name="node">node</param>
   /// <param name="grid">a* grid</param>
   /// <param name="preset">preset</param>
   /// <returns></returns>
   static List<Node> GetNeighboringNodes (Node node, Node[,] grid, MapGeneratorPreset preset) {
      List<Node> NeighboringNodes = new List<Node>();

      // right side
      if (CheckNodeNeighbor(node, preset, node.gridX + 1, node.gridY)) {
         NeighboringNodes.Add(grid[node.gridX + 1, node.gridY]);
      }

      // left side
      if (CheckNodeNeighbor(node, preset, node.gridX - 1, node.gridY)) {
         NeighboringNodes.Add(grid[node.gridX - 1, node.gridY]);
      }

      // top side
      if (CheckNodeNeighbor(node, preset, node.gridX, node.gridY + 1)) {
         NeighboringNodes.Add(grid[node.gridX, node.gridY + 1]);
      }

      // bottom side

      if (CheckNodeNeighbor(node, preset, node.gridX, node.gridY - 1)) {
         NeighboringNodes.Add(grid[node.gridX, node.gridY - 1]);
      }

      return NeighboringNodes;
   }

   /// <summary>
   /// check if neighbor position is valid
   /// </summary>
   /// <param name="node">node</param>
   /// <param name="preset">preset</param>
   /// <param name="xCheck">x position</param>
   /// <param name="yCheck">y position</param>
   /// <returns></returns>
   static bool CheckNodeNeighbor (Node node, MapGeneratorPreset preset, int xCheck, int yCheck) {
      if (xCheck >= 0 && xCheck < preset.mapSize.x) {
         if (yCheck >= 0 && yCheck < preset.mapSize.y) {
            if (node != null) {
               return true;
            }
         }
      }
      return false;
   }

   /// <summary>
   /// return the final path
   /// </summary>
   /// <param name="startNode">start node</param>
   /// <param name="endNode">end node</param>
   /// <returns></returns>
   static List<Node> GetFinalPath (Node startNode, Node endNode) {
      List<Node> finalPath = new List<Node>();
      Node currentNode = endNode;

      while (currentNode != startNode) {
         finalPath.Add(currentNode);
         if (currentNode.nodeType == currentNode.parent.nodeType && currentNode.nodeType != NodeType.Land && currentNode.nodeType != NodeType.Water) {
            return null;
         }
         currentNode = currentNode.parent;

      }
      finalPath.Reverse();

      return finalPath;
   }

   /// <summary>
   /// place objects on the map
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="gridLayers">grid object</param>
   /// <param name="availableTiles">tiles to place objects</param>
   /// <param name="i">i</param>
   static void SetObjects (MapGeneratorPreset preset, GameObject gridLayers, bool[,] availableTiles, int i) {
      System.Random pseudoRandom = new System.Random(preset.seed);
      foreach (var objectLayer in preset.layers[i].objectLayers) {

         int numberOfObjects = 0;

         GameObject objectLayerGameObject = new GameObject(objectLayer.name);
         objectLayerGameObject.transform.position = new Vector3(objectLayerGameObject.transform.position.x, objectLayerGameObject.transform.position.y, (float) (preset.layers.Length - i) / 10 - (float) (preset.layers.Length - i) / 100 - (float) (preset.layers.Length - i) / 1000);
         objectLayerGameObject.transform.parent = gridLayers.transform;
         Tilemap objectTilemap = objectLayerGameObject.AddComponent(typeof(Tilemap)) as Tilemap;
         TilemapRenderer objectTilemapRenderer = objectLayerGameObject.AddComponent(typeof(TilemapRenderer)) as TilemapRenderer;
         objectTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

         for (int x = 0; x < preset.mapSize.x; x++) {
            for (int y = 0; y < preset.mapSize.y; y++) {
               Vector3Int cellPos = new Vector3Int(x, y, 0);
               if (availableTiles[x, y] && numberOfObjects < objectLayer.maxNumberOfObjects && objectLayer.percentageOfRejection <= pseudoRandom.Next(0, 100)) {
                  objectTilemap.SetTile(cellPos, objectLayer.tile);
                  availableTiles[x, y] = false;
                  numberOfObjects++;
               }
            }
         }
      }
   }

   /// <summary>
   /// automatically calculate the layer height
   /// </summary>
   /// <param name="layerLength">number of layers</param>
   /// <returns></returns>
   static float[] CalculateLayerHeight (int layerLength) {
      List<float> layersHeight = null;

      layersHeight = new List<float>();

      float maxHeight = float.MinValue;
      float minHeight = float.MaxValue;

      for (int i = 0; i < layerLength; i++) {
         layersHeight.Add(i + 1f);
         if (layersHeight[i] > maxHeight) {
            maxHeight = layersHeight[i];
         }
         if (layersHeight[i] < minHeight) {
            minHeight = layersHeight[i];
         }
      }

      for (int i = 0; i < layerLength; i++) {
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
   public static float[,] GenerateNoiseMap (Vector2Int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
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

   /// <summary>
   /// randomize avaliable tiles to place object
   /// </summary>
   /// <param name="preset">preset</param>
   /// <param name="availableTiles">tiles to place objects</param>
   /// <param name="seed">seed for random</param>
   /// <param name="percentageOfRejection">percentage of rejection</param>
   static void RandomizeAvailability (MapGeneratorPreset preset, bool[,] availableTiles, int seed, float percentageOfRejection) {
      float[,] noiseMap = GenerateNoiseMap(preset.mapSize, preset.seed, preset.noiseScale, preset.octaves, preset.persistance, preset.lacunarity, preset.offset);

      for (int x = 0; x < availableTiles.GetLength(0); x++) {
         for (int y = 0; y < availableTiles.GetLength(1); y++) {
            if (availableTiles[x, y] && noiseMap[x, y] <= percentageOfRejection) {
               availableTiles[x, y] = false;
            }
         }
      }
   }

   /// <summary>
   /// create the paramref name="gameObject" on the project
   /// </summary>
   /// <param name="path">path</param>
   /// <param name="gameObject">object</param>
   static void GeneratePrefab (string path, GameObject gameObject) {
#if UNITY_EDITOR
      if (!AssetDatabase.IsValidFolder(path)) {
         Directory.CreateDirectory(path);
      }
      // Set the path as within the Assets folder, and name it as the GameObject's name with the .prefab format
      string localPath = path + gameObject.name + ".prefab";

      PrefabUtility.SaveAsPrefabAsset(gameObject, localPath);
#endif
   }

   #region Private Variables

   // Relative path for tiles in Unity project
   static private string _tilesPath = "Assets/TileSets/";

   // List of generated maps in client game instance
   static List<Area.Type> _generatedAreas = new List<Area.Type>();

   // Start area width (in tiles)
   static int[] _areaSize = { 15, 12, 12, 10, 8, 8, 6, 6 };

   // Positions to draw debug rectangle (top spawn)
   static int[] _spawnStartsTop = new int[_areaSize.Length];
   static int[] _spawnEndsTop = new int[_areaSize.Length];

   // Positions to draw debug rectangle (bottom spawn)
   static int[] _spawnStartsBottom = new int[_areaSize.Length];
   static int[] _spawnEndsBottom = new int[_areaSize.Length];

   // Indicate which tiles are water (used for spawning start area)
   static bool[,] _waterTiles;

   // Allow to draw debug tiles for testing purposes
   static bool _enableDrawDebug = false;

   // List of tiles paths (to calculate once)
   static List<string> _assetsPath = new List<string>();

   // Minimum closed area size to create path to
   static int _minClosedPathSize = 50;

   #endregion
}
