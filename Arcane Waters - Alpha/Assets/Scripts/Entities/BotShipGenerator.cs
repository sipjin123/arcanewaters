using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System.Linq;
using Pathfinding;

public class BotShipGenerator
{
   #region Public Variables

   // The list of warp destinations that define a warp as a privateer spawn point
   public static HashSet<string> WARP_DESTINATIONS = new HashSet<string>() {
      "Tutorial Town" };

   // The list of areas where the bot ship generator is active
   public static HashSet<string> AREAS = new HashSet<string>() {
      "Tutorial Bay" };

   // The distance from the spawn point where the bot ships will be generated
   public static float SPAWN_POSITION_DISTANCE_MIN = 0.9f;
   public static float SPAWN_POSITION_DISTANCE_MAX = 1.2f;

   #endregion

   public static bool shouldGenerateBotShips (string areaKey) {
      return AreaManager.self.isSeaArea(areaKey) && AREAS.Contains(areaKey);
   }

   public static Warp getPrivateerSpawnPoint (Area area) {
      // Get the first warp that satisfies the conditions (entrance of a defined town)
      foreach (Warp warp in area.getWarps()) {
         if (WARP_DESTINATIONS.Contains(warp.areaTarget)) {
            return warp;
         }
      }

      return null;
   }

   public static void generateBotShips (Instance instance) {
      if (!NetworkServer.active || !shouldGenerateBotShips(instance.areaKey)) {
         return;
      }

      Area area = AreaManager.self.getArea(instance.areaKey);
      if (area == null || instance == null) {
         return;
      }

      // Get the list of bot ships present in the instance
      List<BotShipEntity> existingShips = new List<BotShipEntity>();
      foreach (NetworkBehaviour networkBehaviour in instance.getEntities()) {
         NetEntity entity = networkBehaviour as NetEntity;

         if (entity == null || !entity.isBotShip()) {
            continue;
         }

         existingShips.Add((BotShipEntity) entity);
      }

      // Count the number of missing guild members in the instance
      int pirateShipCount = existingShips.Where(s => s.guildId == BotShipEntity.PIRATES_GUILD_ID).Count();
      int missingPirates = area.pirateShipDataFields.Count - pirateShipCount;

      int privateerShipCount = existingShips.Where(s => s.guildId == BotShipEntity.PRIVATEERS_GUILD_ID).Count();
      int missingPrivateers = area.privateerShipDataFields.Count - privateerShipCount;
 
      // Pirates only spawn next to treasure sites that are not yet captured
      List<TreasureSite> freeTreasureSites = new List<TreasureSite>();
      foreach (TreasureSite site in instance.treasureSites) {
         if (!site.isCaptured()) {
            freeTreasureSites.Add(site);
         }
      }

      // Generate pirate ships
      if (freeTreasureSites.Count > 0) {
         for (int i = 0; i < missingPirates; i++) {
            TreasureSite pirateSpawnPoint = instance.treasureSites.ChooseRandom();
            spawnSingleBotShip(instance, area, area.pirateShipDataFields, pirateSpawnPoint.transform.position);
         }
      }

      // Generate privateer ships
      Warp privateerSpawnPoint = getPrivateerSpawnPoint(area);
      if (privateerSpawnPoint != null) {
         for (int i = 0; i < missingPrivateers; i++) {
            spawnSingleBotShip(instance, area, area.privateerShipDataFields, privateerSpawnPoint.transform.position);
         }
      }
   }

   private static void spawnSingleBotShip (Instance instance, Area area, List<ExportedPrefab001> shipsData, Vector2 spawnPoint) {
      // Randomly choose a type among the ones initially placed in the map
      ExportedPrefab001 dataField = shipsData.ChooseRandom();

      // Get a random walkable position around the spawn point
      Vector2 spawnPosition = spawnPoint + Random.insideUnitCircle.normalized * Random.Range(SPAWN_POSITION_DISTANCE_MIN, SPAWN_POSITION_DISTANCE_MAX);
      NNInfo nodeInfo = AstarPath.active.GetNearest(spawnPosition, NNConstraint.Default);
      spawnPosition = (Vector3) nodeInfo.node.position - area.transform.position;

      // Spawn the ship
      BotShipEntity botShip = instance.spawnBotShip(dataField, spawnPosition, area, instance.biome);
   }
   
   #region Private Variables

   #endregion
}
