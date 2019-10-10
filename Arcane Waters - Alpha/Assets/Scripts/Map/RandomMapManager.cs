using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using ProceduralMap;

public class RandomMapManager : MonoBehaviour
{
   #region Public Variables

   // A list of map presets we choose from
   public List<MapGeneratorPreset> presets = new List<MapGeneratorPreset>();

   // A list of map presets for each random biome
   public MapGeneratorPreset tropicalPreset;
   public MapGeneratorPreset desertPreset;
   public MapGeneratorPreset pinePreset;
   public MapGeneratorPreset snowPreset;
   public MapGeneratorPreset lavaPreset;
   public MapGeneratorPreset mushroomPreset;

   // The map configs that this server is going to use to create randomized maps
   public Dictionary<Area.Type, MapConfig> mapConfigs = new Dictionary<Area.Type, MapConfig>();

   // Self
   public static RandomMapManager self;

   // Tiles used to visually debug spawn area on random sea map
   public Tile debugTileRed;
   public Tile debugTileBlue;
   public Tile debugTileBlack;

   // Treasure site prefab - spawned on map
   public GameObject treasureSitePrefab;

   #endregion

   private void Awake () {
      self = this;

      debugTileRed.color = new Color(debugTileRed.color.r, debugTileRed.color.g, debugTileRed.color.b, 0.25f);
      debugTileBlue.color = new Color(debugTileBlue.color.r, debugTileBlue.color.g, debugTileBlue.color.b, 0.25f);
      debugTileBlack.color = new Color(debugTileBlack.color.r, debugTileBlack.color.g, debugTileBlack.color.b, 0.25f);
   }

   private void Update () {
      Server ourServer = ServerNetwork.self.server;

      // If we have a Server object ready to go, we can go ahead and create our maps
      if (ourServer != null && ourServer.port != 0 && mapConfigs.Count == 0) {
         createRandomMapsAndInstances();
      }

      // Temporary testing keys to request the random map data from the server
      if (Input.GetKeyUp(KeyCode.F7)) {
         Global.player.rpc.Cmd_GetSummaryOfGeneratedMaps();
      }
      if (Input.GetKeyUp(KeyCode.F8)) {
         Global.player.Cmd_SpawnInNewMap(Area.Type.StartingTown, Spawn.Type.ForestTownDock, Direction.North);
      }
   }

   public List<Instance> getInstances () {
      return _instances;
   }

   private void createRandomMapsAndInstances () {
      // Cycle over each of the random area types
      foreach (Area.Type randomAreaType in Area.getRandomAreaTypes()) {
         // Create a randomized map config
         MapConfig config = generateRandomMapConfig(randomAreaType);

         // Keep track of the map config
         mapConfigs[randomAreaType] = config;

         // Generate the random map tiles
         RandomMapCreator.generateRandomMap(config);

         // Generate an Instance for the map
         Instance instance = InstanceManager.self.createNewInstance(randomAreaType, config.biomeType);

         // Keep track of it locally
         _instances.Add(instance);
      }
   }

   private MapConfig generateRandomMapConfig (Area.Type areaType) {
      int seed = Random.Range(1, 1000);
      int seedPath = Random.Range(1, 1000);
      Biome.Type biomeType = Biome.getAllTypes().ChooseRandom();
      float lacunarity = Random.Range(1f, 3f);
      Vector2 offset = new Vector2(5f, 5f);
      float persistence = Random.Range(.4f, .6f);

      MapConfig config = new MapConfig(seed, persistence, lacunarity, offset, areaType, biomeType, seedPath);

      return config;
   }

   #region Private Variables

   // The Instances that we've created
   protected List<Instance> _instances = new List<Instance>();

   #endregion
}
