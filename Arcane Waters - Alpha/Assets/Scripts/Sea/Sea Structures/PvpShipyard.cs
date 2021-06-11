using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpShipyard : SeaStructure {
   #region Public Variables

   // An enum describing where the shipyard will spawn ships around itself
   public enum SpawnLocation { None = 0, Left = 1, Bottom = 2, Right = 3, Top = 4 }

   // Where the shipyard will spawn ships around itself
   [HideInInspector]
   public SpawnLocation spawnLocation = SpawnLocation.Bottom;

   // Which lane this shipyard will send its ships to
   [HideInInspector]
   public PvpLane laneType = PvpLane.None;

   // A target point in the center of this lane, which ships spawned from this shipyard will path to
   public Transform laneCenterTarget;

   // The location at which this shipyard will spawn ships
   public Transform leftSpawnLocation, bottomSpawnLocation, rightSpawnLocation, topSpawnLocation;

   // A list of sea structures that ships spawned from this shipyard will target
   public List<SeaStructure> targetStructures;

   #endregion

   [Server]
   public BotShipEntity spawnShip () {
      checkReferences();

      BotShipEntity botShip = Instantiate(PrefabsManager.self.botShipPrefab, getSpawnLocation(), Quaternion.identity);
      botShip.areaKey = _instance.areaKey;
      botShip.facing = Direction.South;
      botShip.setAreaParent(_area, true);
      botShip.seaEntityData = _shipData;
      botShip.maxHealth = _shipData.maxHealth;
      botShip.currentHealth = _shipData.maxHealth;
      botShip.shipType = Ship.Type.Type_1;
      botShip.guildId = BotShipEntity.PIRATES_GUILD_ID;
      botShip.setShipData(_shipData.xmlId, Ship.Type.Type_1, _instance.difficulty);
      botShip.pvpTeam = pvpTeam;
      botShip.setPvpLaneTarget(laneCenterTarget);
      botShip.setPvpTargetStructures(targetStructures);

      InstanceManager.self.addSeaMonsterToInstance(botShip, _instance);
      NetworkServer.Spawn(botShip.gameObject);

      return botShip;
   }

   private void checkReferences () {
      if (_shipData == null) {
         _shipData = SeaMonsterManager.self.getAllSeaMonsterData().Find(ent => ent.subVarietyTypeId == (int) Ship.Type.Type_1);
      }

      if (_instance == null) {
         _instance = InstanceManager.self.getInstance(instanceId);
      }

      if (_area == null) {
         _area = AreaManager.self.getArea(_instance.areaKey);
      }
   }

   private Vector3 getSpawnLocation () {
      switch (spawnLocation) {
         case SpawnLocation.Left:
            return leftSpawnLocation.position;
         case SpawnLocation.Bottom:
            return bottomSpawnLocation.position;
         case SpawnLocation.Right:
            return rightSpawnLocation.position;
         case SpawnLocation.Top:
            return topSpawnLocation.position;
         default:
            return bottomSpawnLocation.position;
      }
   }

   #region Private Variables

   // The cached data for the ships we spawn
   private SeaMonsterEntityData _shipData = null;

   // A reference to the area we are in
   private Area _area = null;

   // A reference to the instance we are in
   private Instance _instance = null;

   #endregion
}
