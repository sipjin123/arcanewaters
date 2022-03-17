using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using UnityEngine.InputSystem;

public class SeaManager : MonoBehaviour {
   #region Public Variables

   // The movement modes
   public enum MoveMode { Instant = 1, Delay = 2, ServerAuthoritative = 5 }

   // The current movement mode
   public static MoveMode moveMode = MoveMode.ServerAuthoritative;

   // The combat modes
   public enum CombatMode { Circle = 1, Straight = 2, Select = 3 }

   // The current combat mode
   public static CombatMode combatMode = CombatMode.Select;

   // The currently selected ability id
   public static int selectedAbilityId = 1;

   // Self
   public static SeaManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public bool isOffensiveAbility () {
      if (getAttackType(selectedAbilityId) == Attack.Type.Heal || getAttackType(selectedAbilityId) == Attack.Type.SpeedBoost) {
         return false;
      }

      return true;
   }

   public static Attack.Type getAttackType (int ablityId) {
      return ShipAbilityManager.self.getAbility(ablityId).selectedAttackType;
   }

   public static Attack.Type getAttackType () {
      return ShipAbilityManager.self.getAbility(selectedAbilityId).selectedAttackType;
   }

   private void Update () {
      // We only handle player input when the player is in a ship
      if (!(Global.player is PlayerShipEntity) || ChatManager.isTyping()) {
         return;
      }

      // Allow pressing F1 through F3 to change the combat mode
      //if (KeyUtils.GetKeyDown(Key.Digit1)) {
      //   selectedAbilityId = CannonPanel.self.getAbilityId(0);
      //}

      //if (KeyUtils.GetKeyDown(Key.Digit2)) {
      //   selectedAbilityId = CannonPanel.self.getAbilityId(1);
      //}

      //if (KeyUtils.GetKeyDown(Key.Digit3)) {
      //   selectedAbilityId = CannonPanel.self.getAbilityId(2);
      //}

      // Allow pressing F1 through F2 to change the move mode
      //if (KeyUtils.isKeyPressedUp(Key.F1)) {
      //   Global.player.Cmd_ChangeMass(false);
      //   moveMode = MoveMode.Instant;
      //}
      //if (KeyUtils.isKeyPressedUp(Key.F2)) {
      //   Global.player.Cmd_ChangeMass(false);
      //   moveMode = MoveMode.Delay;
      //}
      //if (KeyUtils.isKeyPressedUp(Key.F3)) {
      //   Global.player.Cmd_ChangeMass(true);
      //}
      //if (KeyUtils.isKeyPressedUp(Key.F4)) {
      //   Global.player.Cmd_ChangeMass(true);
      //   moveMode = MoveMode.Arrows;
      //}
      //if (KeyUtils.isKeyPressedUp(Key.F5)) {
      //   Global.player.Cmd_SetServerAuthoritativeMode();
      //   moveMode = MoveMode.ServerAuthoritative;
      //}
      //if (KeyUtils.isKeyPressedUp(Key.F6)) {
      //   Global.player.Cmd_ToggleVelocityDrivenTransform();
      //}

      // Allow admin to spawn sea enemies for test purposes
      if (Global.player.isAdmin() && KeyUtils.GetKey(Key.Z)) {
         // Allow spawning a horror
         if (KeyUtils.GetKeyUp(Key.F1) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnBossParent(Util.getMousePos(), SeaMonsterEntity.Type.Horror);
         }

         // Allow spawning a Worm
         if (KeyUtils.GetKeyUp(Key.F2) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Worm);
         }

         // Allow spawning a Giant
         if (KeyUtils.GetKeyUp(Key.F3) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Reef_Giant);
         }

         // Allow spawning a Fishman
         if (KeyUtils.GetKeyUp(Key.F4) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Fishman);
         }
         
         // Allow spawning a Sea Mine
         if (KeyUtils.GetKeyUp(Key.F5) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMine(Util.getMousePos());
         }

         // Allow spawning a pirate ship
         if (KeyUtils.GetKeyUp(Key.F9) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnPirateShip(Util.getMousePos(), BotShipEntity.PIRATES_GUILD_ID, false);
         }

         // Allow spawning a privateer ship
         if (KeyUtils.GetKeyUp(Key.F10) && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnPirateShip(Util.getMousePos(), BotShipEntity.PRIVATEERS_GUILD_ID, false);
         }
      }
   }

   public SeaEntity getEntity (uint netId) {
      if (_entities.ContainsKey(netId)) {
         return _entities[netId];
      }

      return null;
   }

   public SeaEntity getEntityByUserId (int userId) {
      SeaEntity seaEntity = _entities.Values.ToList().Find(_ => _.userId == userId); 
      return seaEntity;
   }

   public void storeEntity (SeaEntity entity) {
      _entities[entity.netId] = entity;
   }

   #region Private Variables

   // A mapping of netId to Sea Entity
   protected Dictionary<uint, SeaEntity> _entities = new Dictionary<uint, SeaEntity>();

   #endregion
}
