using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaManager : MonoBehaviour {
   #region Public Variables

   // The movement modes
   public enum MoveMode { Instant = 1, Delay = 2, Arrows = 4, }

   // The current movement mode
   public static MoveMode moveMode = MoveMode.Instant;

   // The combat modes
   public enum CombatMode { Circle = 1, Straight = 2, Select = 3 }

   // The current combat mode
   public static CombatMode combatMode = CombatMode.Circle;

   // The currently selected attack type
   public static Attack.Type selectedAttackType = Attack.Type.Cannon;

   // Self
   public static SeaManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public bool isOffensiveAbility () {
      if (selectedAttackType == Attack.Type.Heal || selectedAttackType == Attack.Type.SpeedBoost) {
         return false;
      }

      return true;
   }

   private void Update () {
      // We only handle player input when the player is in a ship
      if (!(Global.player is PlayerShipEntity) || ChatManager.isTyping()) {
         return;
      }

      // Allow pressing F1 through F3 to change the combat mode
      if (Input.GetKeyUp(KeyCode.Alpha1)) {
         selectedAttackType = Attack.Type.Cannon;
      }

      if (Input.GetKeyUp(KeyCode.Alpha2)) {
         selectedAttackType = Attack.Type.Ice;
      }

      if (Input.GetKeyUp(KeyCode.Alpha3)) {
         selectedAttackType = Attack.Type.Air;
      }

      // Allow pressing F1 through F2 to change the move mode
      if (Input.GetKeyUp(KeyCode.F1)) {
         Global.player.Cmd_ChangeMass(false);
         moveMode = MoveMode.Instant;
      }
      if (Input.GetKeyUp(KeyCode.F2)) {
         Global.player.Cmd_ChangeMass(false);
         moveMode = MoveMode.Delay;
      }
      if (Input.GetKeyUp(KeyCode.F3)) {
         Global.player.Cmd_ChangeMass(true);
      }
      if (Input.GetKeyUp(KeyCode.F4)) {
         Global.player.Cmd_ChangeMass(true);
         moveMode = MoveMode.Arrows;
      }

      // Allow spawning a pirate ship
      if (Input.GetKeyUp(KeyCode.F9) && Global.player is SeaEntity) {
         Global.player.rpc.Cmd_SpawnPirateShip(Util.getMousePos());
      }

      if (Input.GetKey(KeyCode.Z)) {
         // Allow spawning a horror
         if (Input.GetKeyUp(KeyCode.F1) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnBossParent(Util.getMousePos(), SeaMonsterEntity.Type.Horror);
         }

         // Allow spawning a Worm
         if (Input.GetKeyUp(KeyCode.F2) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Worm);
         }

         // Allow spawning a Giant
         if (Input.GetKeyUp(KeyCode.F3) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Reef_Giant);
         }

         // Allow spawning a Fishman
         if (Input.GetKeyUp(KeyCode.F4) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Fishman);
         }
      }
   }

   public SeaEntity getEntity (int userId) {
      if (_entities.ContainsKey(userId)) {
         return _entities[userId];
      }

      return null;
   }

   public void storeEntity (SeaEntity entity) {
      _entities[entity.userId] = entity;
   }

   #region Private Variables

   // A mapping of userId to Sea Entity
   protected Dictionary<int, SeaEntity> _entities = new Dictionary<int, SeaEntity>();

   #endregion
}
