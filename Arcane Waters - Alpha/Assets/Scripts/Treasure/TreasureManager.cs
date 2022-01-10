using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

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

   // The prefab we use for creating floating map fragment icons
   public GameObject floatingMapFragmentPrefab;

   // The prefab we use for creating floating powerup icons
   public GameObject powerupPopupIcon;

   // The map fragment name displayed by the floating icon
   public static string MAP_FRAGMENT_NAME = "Map Fragment";

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
      chest.lootGroupId = spot.lootGroupId;

      // Keep track of the chests that we've created
      _chests.Add(chest.id, chest);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(chest.gameObject);

      return chest;
   }

   public TreasureChest createSeaMonsterChest (Instance instance, Vector3 spot, int enemyXmlId, int userId, uint[] attackers, List<int> combatParticipants) {
      // Instantiate a new Treasure Chest
      TreasureChest chest = Instantiate(seaChestPrefab, spot, Quaternion.identity);

      // Raycast around spawned chest to ensure that there is some distance between chest and nearby colliders
      int layerMask = LayerMask.GetMask(LayerUtil.GRID_COLLIDERS);
      const float MIN_DISTANCE = 0.25f;

      RaycastHit2D hitLeft = Physics2D.Raycast(chest.transform.position, Vector2.left, MIN_DISTANCE, layerMask);
      RaycastHit2D hitRight = Physics2D.Raycast(chest.transform.position, Vector2.right, MIN_DISTANCE, layerMask);
      RaycastHit2D hitTop = Physics2D.Raycast(chest.transform.position, Vector2.up, MIN_DISTANCE, layerMask);
      RaycastHit2D hitBottom = Physics2D.Raycast(chest.transform.position, Vector2.down, MIN_DISTANCE, layerMask);

      if (hitLeft.distance > 0 && hitLeft.distance < MIN_DISTANCE) {
         chest.transform.position += new Vector3((MIN_DISTANCE - hitLeft.distance), 0, 0);
      }
      if (hitRight.distance > 0 && hitRight.distance < MIN_DISTANCE) {
         chest.transform.position -= new Vector3((MIN_DISTANCE - hitRight.distance), 0, 0);
      }
      if (hitTop.distance > 0 && hitTop.distance < MIN_DISTANCE) {
         chest.transform.position -= new Vector3(0, (MIN_DISTANCE - hitTop.distance), 0);
      }
      if (hitBottom.distance > 0 && hitBottom.distance < MIN_DISTANCE) {
         chest.transform.position += new Vector3(0, (MIN_DISTANCE - hitBottom.distance), 0);
      }

      // Setup the chest variables
      initEnemyDropChest(chest, (int) enemyXmlId, instance, true);

      // Allow the user who triggered the spawn to see and interact with this loot bag
      chest.allowedUserIds.Add(userId);

      List<PlayerShipEntity> instancePlayerEntities = instance.getPlayerShipEntities();
      int playerVoyageGroupId = instancePlayerEntities.Find(_ => _.userId == userId).voyageGroupId;
      List<int> validUserIds = new List<int>();

      // If this is a voyage, reward entire voyage party
      if ((playerVoyageGroupId > 0 && !instance.isPvP) || instance.isPvP) {
         // If there are players in the instance that share the same voyage group id with the player, then add them to the allowed list of user interaction
         foreach (uint attacker in attackers) {
            NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attacker);
            if (entity != null && playerVoyageGroupId == entity.voyageGroupId) {
               if (!validUserIds.Contains(entity.userId)) {
                  validUserIds.Add(entity.userId);
               }
            } else {
               D.debug("Null net entity for user: " + attacker);
            }
         }

         // If the net entity was not found using their netId, use the combat participant list and add them to reward list
         foreach (int combatant in combatParticipants) {
            if (!validUserIds.Contains(combatant)) {
               validUserIds.Add(combatant);
            }
         }

         // Process chest spawning and perks/powerup bonus chest spawn
         foreach (int validUserId in validUserIds) {
            if (!chest.allowedUserIds.Contains(validUserId)) {
               chest.allowedUserIds.Add(validUserId);

               int bonusChests = 0;
               if (PerkManager.self.perkActivationRoll(validUserId, Perk.Category.ItemDropChances)) {
                  D.debug("User {" + validUserId + "} Received a Bonus loot using Perks");
                  bonusChests++;
               }

               if (PowerupManager.self.powerupActivationRoll(validUserId, Powerup.Type.TreasureDropUp)) {
                  D.debug("User {" + validUserId + "} Received a Bonus loot using Powerup");
                  bonusChests++;
               }

               for (int i = 0; i < bonusChests; i++) {
                  // Instantiate a new Treasure Chest like the original
                  TreasureChest bonusChest = Instantiate(seaChestPrefab, spot + Vector3.up * 0.1f * (i + 1), Quaternion.identity);

                  // Setup the chest variables
                  initEnemyDropChest(bonusChest, (int) enemyXmlId, instance, true);

                  // Only the player who got the extra drop is allowed to open it (or see it)
                  bonusChest.allowedUserIds.Add(validUserId);

                  NetworkServer.Spawn(bonusChest.gameObject);
               }
            }
         }
      }

      // Spawn the network object on the Clients
      NetworkServer.Spawn(chest.gameObject);

      return chest;
   }

   public TreasureChest createBattlerMonsterChest (Instance instance, Vector3 spot, int enemyTypeId, int userId) {
      // Instantiate a new Treasure Chest
      TreasureChest chest = Instantiate(monsterBagPrefab, spot, Quaternion.identity);

      // Raycast around spawned chest to ensure that there is some distance between chest and nearby colliders
      const float MIN_DISTANCE = 0.1f;
      const int MAX_RETRIES = 20;
      int layerMask = LayerMask.GetMask(LayerUtil.GRID_COLLIDERS);
      int retries = 0;

      Vector2 originalPos = chest.transform.position;
      Collider2D result = Physics2D.OverlapCircle(chest.transform.position, MIN_DISTANCE, layerMask);
      Vector2 playerPos = EntityManager.self.getEntity(userId).transform.position;
      Vector2 dir = playerPos - originalPos;

      while (result != null) {
         retries++;
         Vector2 addPos = Random.insideUnitCircle * 0.3f;
         chest.transform.position = originalPos + dir * 0.2f + addPos;

         result = Physics2D.OverlapCircle(chest.transform.position, MIN_DISTANCE, layerMask);

         if (retries > MAX_RETRIES) {
            chest.transform.position = playerPos - dir * MIN_DISTANCE;
            break;
         }
      }

      // Setup the chest variables
      initEnemyDropChest(chest, enemyTypeId, instance, true);

      // Automatically allow the user who triggered the spawn loot bag to have permission to interact with this loot bag
      chest.allowedUserIds.Add(userId);

      List<PlayerBodyEntity> instancePlayerEntities = instance.getPlayerBodyEntities();
      int playerVoyageGroupId = instancePlayerEntities.Find(_ => _.userId == userId).voyageGroupId;

      // If there are players in the instance that share the same voyage group id with the player, then add them to the allowed list of user interaction
      if (playerVoyageGroupId > 0) {
         foreach (PlayerBodyEntity playerEntity in instancePlayerEntities) {
            if (playerEntity.userId != userId && playerEntity.voyageGroupId == playerVoyageGroupId) {
               chest.allowedUserIds.Add(playerEntity.userId);
            }
         }
      }

      // Spawn the network object on the Clients
      NetworkServer.Spawn(chest.gameObject);

      // Check if any players in the instance that got an extra loot drop
      foreach (PlayerBodyEntity playerEntity in instancePlayerEntities) {
         if (playerEntity.voyageGroupId == playerVoyageGroupId) {

            int bonusChests = 0;
            if (PerkManager.self.perkActivationRoll(playerEntity.userId, Perk.Category.ItemDropChances)) {
               bonusChests++;
            }

            if (PowerupManager.self.powerupActivationRoll(playerEntity.userId, Powerup.Type.TreasureDropUp)) {
               bonusChests++;
            }

            for (int i = 0; i < bonusChests; i++) {
               // Instantiate a new Treasure Chest like the original
               TreasureChest bonusChest = Instantiate(monsterBagPrefab, spot + Vector3.up * 0.1f * i, Quaternion.identity);

               // Setup the chest variables
               initEnemyDropChest(bonusChest, enemyTypeId, instance, true);

               // Only the player who got the extra drop is allowed to open it (or see it)
               bonusChest.allowedUserIds.Add(playerEntity.userId);

               NetworkServer.Spawn(bonusChest.gameObject);
            }
         }
      }


      return chest;
   }

   public TreasureChest getChest (int id) {
      return _chests[id];
   }

   private void initEnemyDropChest (TreasureChest chest, int enemyTypeId, Instance instance, bool autoDestroy = false) {
      // Set the enemy type, to determine possible drops
      chest.enemyType = enemyTypeId;

      // Give the chest a unique ID
      chest.id = _id++;

      // Give the chest a random rarity
      chest.rarity = Rarity.getRandom();

      // Keep track of this chest
      _chests.Add(chest.id, chest);

      // Add the chest to the instance
      instance.entities.Add(chest);

      // Setup other variables
      chest.areaKey = instance.areaKey;
      chest.transform.SetParent(this.transform, true);
      chest.instanceId = instance.id;
      chest.autoDestroy = autoDestroy;
   }

   [Server]
   public bool isLastUnopenedChestInInstanceForUser (int chestId, int instanceId, int userId) {
      foreach (TreasureChest chest in _chests.Values) {
         if (chest.instanceId == instanceId && chest.id != chestId && chest.chestType == ChestSpawnType.Site) {
            bool isOpened = false;
            foreach (int interactedUserId in chest.userIds) {
               if (interactedUserId == userId) {
                  isOpened = true;
                  break;
               }
            }
            
            if (!isOpened) {
               return false;
            }
         }
      }

      return true;
   }

   [Server]
   public bool areAllChestsOpenedForGroup (int instanceId, List<int> userList) {
      foreach (TreasureChest chest in _chests.Values) {
         if (chest.instanceId == instanceId && chest.chestType == ChestSpawnType.Site) {
            if (userList.Except(chest.userIds).Any()) {
               return false;
            }
         }
      }

      return true;
   }

   #region Private Variables

   // Stores the treasure chests we've created
   protected Dictionary<int, TreasureChest> _chests = new Dictionary<int, TreasureChest>();

   // An ID we use to uniquely identify treasure
   protected static int _id = 1;

   #endregion
}
