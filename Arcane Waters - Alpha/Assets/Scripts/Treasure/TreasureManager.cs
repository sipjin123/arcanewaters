using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureManager : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating Treasure Chests
   public TreasureChest chestPrefab;

   // The prefab we use for creating Treasure Chests for sea maps
   public TreasureChest seaChestPrefab;

   // The prefab we use for creating Treasure Chests for monster drops
   public TreasureChest monsterBagPrefab;

   // The prefab we use for creating floating icons
   public GameObject floatingIconPrefab;

   // Self
   public static TreasureManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void createTreasureForInstance (Instance instance) {
      // Look up the Area associated with this intance
      Area area = AreaManager.self.getArea(instance.areaKey);

      // Find all of the possible treasure spots in this Area
      foreach (TreasureSpot spot in area.GetComponentsInChildren<TreasureSpot>()) {
         // Have a random chance of spawning a treasure chest there
         if (Random.Range(0f, 1f) <= spot.spawnChance) {
            TreasureChest chest = createTreasure(instance, spot);
            chest.chestSpawnId = spot.mapDataId;
            chest.areaKey = instance.areaKey;

            // The Instance needs to keep track of all Networked objects inside
            instance.entities.Add(chest);
         }
      }
   }

   protected TreasureChest createTreasure (Instance instance, TreasureSpot spot) {
      // Instantiate a new Treasure Chest
      TreasureChest chest = Instantiate(chestPrefab, spot.transform.position, Quaternion.identity);

      // Keep it parented to this Manager
      chest.transform.SetParent(this.transform, true);

      // Assign a unique ID
      chest.id = _id++;

      // Determine chest content rarity
      chest.rarity = Rarity.getRandom();

      // Sets up the custom sprite of the treasure
      chest.useCustomSprite = spot.useCustomSprite;
      chest.customSpritePath = spot.customSpritePath;

      // Note which instance the chest is in
      chest.instanceId = instance.id;

      // Keep track of the chests that we've created
      _chests.Add(chest.id, chest);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(chest.gameObject);

      return chest;
   }

   public TreasureChest createSeaMonsterChest (Instance instance, Vector3 spot, SeaMonsterEntity.Type enemyType) {
      // Instantiate a new Treasure Chest
      TreasureChest chest = Instantiate(seaChestPrefab, spot, Quaternion.identity);

      // Sets the chest to be destroyed after interaction
      chest.autoDestroy = true;

      // Sets the type of enemy
      chest.enemyType = (int)enemyType;
      chest.areaKey = instance.areaKey;

      // Keep it parented to this Manager
      chest.transform.SetParent(this.transform, true);

      // Assign a unique ID
      chest.id = _id++;

      // Note which instance the chest is in
      chest.instanceId = instance.id;

      // Keep track of the chests that we've created
      _chests.Add(chest.id, chest);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(chest);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(chest.gameObject);

      return chest;
   }

   public TreasureChest createBattlerMonsterChest (Instance instance, Vector3 spot, int enemyTypeId, int userId) {
      // Instantiate a new Treasure Chest
      TreasureChest chest = Instantiate(monsterBagPrefab, spot, Quaternion.identity);

      // Sets the chest to be destroyed after interaction
      chest.autoDestroy = true;

      // Sets the type of enemy
      chest.enemyType = enemyTypeId;
      chest.areaKey = instance.areaKey;

      // Keep it parented to this Manager
      chest.transform.SetParent(this.transform, true);

      // Assign a unique ID
      chest.id = _id++;

      // Note which instance the chest is in
      chest.instanceId = instance.id;

      // Keep track of the chests that we've created
      _chests.Add(chest.id, chest);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(chest);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(chest.gameObject);

      // Automatically allow the user who triggered the spawn loot bag to have permission to interact with this loot bag
      chest.allowedUserIds.Add(userId);

      List<PlayerBodyEntity> instancePlayerEntities = instance.getPlayerBodyEntities();
      if (instancePlayerEntities.Find(_ => _.userId == userId).voyageGroupId > 0) {
         int playerVoyageGroupId = instancePlayerEntities.Find(_ => _.userId == userId).voyageGroupId;
         foreach (PlayerBodyEntity playerEntity in instancePlayerEntities) {
            // If there are players in the instance that share the same voyage group id with the player, then add them to the allowed list of user interaction
            if (playerEntity.userId != userId && playerEntity.voyageGroupId == playerVoyageGroupId) {
               chest.allowedUserIds.Add(playerEntity.userId); 
            }
         }
      }

      return chest;
   }

   public TreasureChest getChest (int id) {
      return _chests[id];
   }

   #region Private Variables

   // Stores the treasure chests we've created
   protected Dictionary<int, TreasureChest> _chests = new Dictionary<int, TreasureChest>();

   // An ID we use to uniquely identify treasure
   protected static int _id = 1;

   #endregion
}
