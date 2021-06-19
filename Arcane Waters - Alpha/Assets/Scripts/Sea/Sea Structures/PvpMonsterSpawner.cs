using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

public class PvpMonsterSpawner : MonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The type of seamonster this spawner will generate
   public SeaMonsterEntity.Type seaMonsterType = SeaMonsterEntity.Type.None;

   // The type of powerup this spawner will generate
   public Powerup.Type powerupType;

   // The instance id
   public int instanceId;

   // The delay before the spawning starts
   public const int START_DELAY = 10;

   // The delay before the the monster will respawn
   public const int RESPAWN_DELAY = 30;

   #endregion

   private void Start () {
      Invoke(nameof(spawnMonster), START_DELAY);
   }

   private void Update () {
      if (!NetworkServer.active) {
         return;
      }

      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.X)) {
         spawnMonster();
      }
   }

   public void initializeSpawner () {
      // TODO: This function should be called by the pvp game when the match officially starts
      spawnMonster();
   }

   private void spawnMonster () {
      SeaMonsterEntity seaEntity = Instantiate(PrefabsManager.self.seaMonsterPrefab);
      SeaMonsterEntityData data = SeaMonsterManager.self.getMonster(seaMonsterType);
      Instance instance = InstanceManager.self.getInstance(instanceId);

      // Basic setup
      seaEntity.monsterType = data.seaMonsterType;
      seaEntity.areaKey = instance.areaKey;
      seaEntity.facing = Direction.South;
      seaEntity.isPvpMonster = true;

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
                  if (memberEntity != null && memberEntity is PlayerShipEntity) {
                     memberEntity.rpc.Target_ReceivePowerup(powerupType, seaEntity.transform.position);
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
      }
   }

   #region Private Variables

   #endregion
}
