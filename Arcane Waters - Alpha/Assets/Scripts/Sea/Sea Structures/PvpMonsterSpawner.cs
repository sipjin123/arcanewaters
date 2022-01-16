using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

public class PvpMonsterSpawner : NetworkBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The type of seamonster this spawner will generate
   public SeaMonsterEntity.Type seaMonsterType = SeaMonsterEntity.Type.None;

   // The type of powerup this spawner will generate
   public Powerup.Type powerupType;

   // The instance id
   public int instanceId;

   // The delay before the spawning starts
   public const int START_DELAY = 30;

   // The delay before the the monster will respawn
   public const int RESPAWN_DELAY = 30;

   // The index spawn id
   [SyncVar]
   public int spawnId = 0;

   // The loot group id
   [SyncVar]
   public int lootGroupId = 0;

   // Radius that determines if the spawn is valid
   public const float radiusCheck = 1.15f;

   #endregion

   public void initializeSpawner () {
      // TODO: Do initialization logic here
      // This initializes the monster spawning everytime the previous monster dies, this function is continuous until the game ends
      Invoke(nameof(spawnMonster), START_DELAY);
   }

   public void endSpawners () {
      // TODO: Do end logic here

      StopAllCoroutines();
   }

   private void spawnMonster () {
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radiusCheck);
      bool isSpawnBlocked = false;
      foreach (Collider2D collidedEntity in hits) {
         if (collidedEntity != null) {
            PlayerShipEntity shipEntity = collidedEntity.GetComponent<PlayerShipEntity>();
            if (shipEntity != null) {
               isSpawnBlocked = true;
               D.editorLog("Distance to the target {" + shipEntity.userId + "} {" + Vector2.Distance(shipEntity.transform.position, transform.position) + "}", Color.magenta);
            }
         }
      }

      // If a ship is in the spawn box, cancel spawn and reset timer
      if (isSpawnBlocked) {
         Invoke(nameof(spawnMonster), RESPAWN_DELAY);
         return;
      }

      SeaMonsterEntity seaEntity = Instantiate(PrefabsManager.self.seaMonsterPrefab);
      SeaMonsterEntityData data = SeaMonsterManager.self.getMonster(seaMonsterType);
      Instance instance = InstanceManager.self.getInstance(instanceId);

      // Basic setup
      seaEntity.monsterType = data.seaMonsterType;
      seaEntity.areaKey = instance.areaKey;
      seaEntity.facing = Direction.South;
      seaEntity.isPvpAI = true;
      seaEntity.instanceId = instanceId;
      seaEntity.difficulty = instance.difficulty;

      // Transform setup
      seaEntity.transform.SetParent(transform, false);
      seaEntity.transform.localPosition = Vector3.zero;

      // Network Setup
      InstanceManager.self.addSeaMonsterToInstance(seaEntity, instance);
      NetworkServer.Spawn(seaEntity.gameObject);

      seaEntity.hasDiedEvent.AddListener(() => {
         uint lastAttackerId = seaEntity.lastAttackerId();
         NetEntity lastAttacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(lastAttackerId);
         if (lastAttacker != null) {
            if (lastAttacker.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
               // Send the result to all group members
               foreach (int userId in voyageGroup.members) {
                  NetEntity memberEntity = EntityManager.self.getEntity(userId);
                  if (memberEntity != null && memberEntity is PlayerShipEntity && seaEntity.wasAttackedBy(memberEntity.netId)) {
                     // Assign default powerup as fall back option if there are no valid loot group powerup
                     Powerup newPowerupData = new Powerup {
                        powerupRarity = Rarity.Type.Common,
                        powerupType = powerupType,
                        expiry = Powerup.Expiry.None
                     };

                     // Assign random powerup based on the loot group id set in map tool
                     if (TreasureDropsDataManager.self.lootDropsCollection.ContainsKey(lootGroupId)) {
                        LootGroupData lootData = TreasureDropsDataManager.self.lootDropsCollection[lootGroupId];
                        List<TreasureDropsData> validPowerupLoots = lootData.treasureDropsCollection.FindAll(_ => _.powerUp != Powerup.Type.None && _.powerupChance > 0);
                        if (validPowerupLoots.Count > 0) {
                           TreasureDropsData randomLoot = validPowerupLoots.ChooseRandom();
                           newPowerupData.powerupRarity = randomLoot.rarity;
                           newPowerupData.powerupType = randomLoot.powerUp;
                           newPowerupData.expiry = Powerup.Expiry.None;
                        }
                     }
                     
                     PowerupManager.self.addPowerupServer(memberEntity.userId, newPowerupData);
                     memberEntity.rpc.Target_ReceivePowerup(newPowerupData.powerupType, newPowerupData.powerupRarity, seaEntity.transform.position);
                  }
               }
            }
         }
         Invoke(nameof(spawnMonster), RESPAWN_DELAY);
         seaEntity.hasDiedEvent.RemoveAllListeners();
      });
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SEA_ENEMY_DATA_KEY) == 0) {
            try {
               SeaMonsterEntity.Type seaMonsterType = (SeaMonsterEntity.Type) Enum.Parse(typeof(SeaMonsterEntity.Type), field.v);
               this.seaMonsterType = seaMonsterType;
            } catch {

            }
         }

         if (field.k.CompareTo(DataField.PVP_MONSTER_POWERUP) == 0) {
            try {
               Powerup.Type powerupType = (Powerup.Type) Enum.Parse(typeof(Powerup.Type), field.v);
               this.powerupType = powerupType;
            } catch {

            }
         }

         if (field.k.CompareTo(DataField.LOOT_GROUP_ID) == 0) {
            try {
               lootGroupId = int.Parse(field.v);
            } catch {

            }
         }
      }
   }

   #region Private Variables

   #endregion
}
