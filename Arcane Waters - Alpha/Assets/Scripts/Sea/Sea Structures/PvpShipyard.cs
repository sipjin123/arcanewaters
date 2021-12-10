using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpShipyard : SeaStructure {
   #region Public Variables

   // An enum describing where the shipyard will spawn ships around itself
   public enum SpawnLocation { None = 0, Left = 1, Bottom = 2, Right = 3, Top = 4, TopRight = 5, BottomLeft = 6 }

   // Where the shipyard will spawn ships around itself
   [HideInInspector]
   public SpawnLocation spawnLocation = SpawnLocation.Bottom;

   // A target point in the center of this lane, which ships spawned from this shipyard will path to
   public Transform laneCenterTarget;

   // The location at which this shipyard will spawn ships
   public Transform leftSpawnLocation, bottomSpawnLocation, rightSpawnLocation, topSpawnLocation, topRightSpawnLocation, bottomLeftSpawnLocation;

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
      botShip.dataXmlId = ShipDataManager.STARTING_SHIP_ID;
      _shipData.xmlId = ShipDataManager.STARTING_SHIP_ID;
      botShip.guildId = BotShipEntity.PIRATES_GUILD_ID;
      botShip.pvpTeam = pvpTeam;
      botShip.faction = faction;
      botShip.setPvpLaneTarget(laneCenterTarget);
      botShip.setPvpTargetStructures(targetStructures);

      botShip.setShipData(_shipData.xmlId, Ship.Type.Type_1, _instance.difficulty);

      InstanceManager.self.addSeaMonsterToInstance(botShip, _instance);
      NetworkServer.Spawn(botShip.gameObject);

      return botShip;
   }

   protected override Sprite getSprite () {
      Sprite[] shipyardSprites = ImageManager.getSprites("Sprites/SeaStructures/pvp_shipyard");
      return shipyardSprites[getSpriteIndex()];
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
         case SpawnLocation.TopRight:
            return topRightSpawnLocation.position;
         case SpawnLocation.BottomLeft:
            return bottomLeftSpawnLocation.position;
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
