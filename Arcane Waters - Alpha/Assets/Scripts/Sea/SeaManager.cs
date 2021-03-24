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
      if (Keyboard.current.digit1Key.wasPressedThisFrame) {
         selectedAbilityId = CannonPanel.self.getAbilityId(0);
      }

      if (Keyboard.current.digit2Key.wasPressedThisFrame) {
         selectedAbilityId = CannonPanel.self.getAbilityId(1);
      }

      if (Keyboard.current.digit3Key.wasPressedThisFrame) {
         selectedAbilityId = CannonPanel.self.getAbilityId(2);
      }

      // Allow pressing F1 through F2 to change the move mode
      //if (Keyboard.current.f1Key.wasReleasedThisFrame) {
      //   Global.player.Cmd_ChangeMass(false);
      //   moveMode = MoveMode.Instant;
      //}
      //if (Keyboard.current.f2Key.wasReleasedThisFrame) {
      //   Global.player.Cmd_ChangeMass(false);
      //   moveMode = MoveMode.Delay;
      //}
      //if (Keyboard.current.f3Key.wasReleasedThisFrame) {
      //   Global.player.Cmd_ChangeMass(true);
      //}
      //if (Keyboard.current.f4Key.wasReleasedThisFrame) {
      //   Global.player.Cmd_ChangeMass(true);
      //   moveMode = MoveMode.Arrows;
      //}
      //if (Keyboard.current.f5Key.wasReleasedThisFrame) {
      //   Global.player.Cmd_SetServerAuthoritativeMode();
      //   moveMode = MoveMode.ServerAuthoritative;
      //}
      //if (Keyboard.current.f6Key.wasReleasedThisFrame) {
      //   Global.player.Cmd_ToggleVelocityDrivenTransform();
      //}

      if (Keyboard.current.zKey.isPressed) {
         // Allow spawning a horror
         if (Keyboard.current.f1Key.wasReleasedThisFrame && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnBossParent(Util.getMousePos(), SeaMonsterEntity.Type.Horror);
         }

         // Allow spawning a Worm
         if (Keyboard.current.f2Key.wasReleasedThisFrame && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Worm);
         }

         // Allow spawning a Giant
         if (Keyboard.current.f3Key.wasReleasedThisFrame && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Reef_Giant);
         }

         // Allow spawning a Fishman
         if (Keyboard.current.f4Key.wasReleasedThisFrame && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMonster(Util.getMousePos(), SeaMonsterEntity.Type.Fishman);
         }
         
         // Allow spawning a Sea Mine
         if (Keyboard.current.f5Key.wasReleasedThisFrame && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnSeaMine(Util.getMousePos());
         }

         // Allow spawning a pirate ship
         if (Keyboard.current.f9Key.wasReleasedThisFrame && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnPirateShip(Util.getMousePos(), BotShipEntity.PIRATES_GUILD_ID);
         }

         // Allow spawning a privateer ship
         if (Keyboard.current.f10Key.wasReleasedThisFrame && Global.player is SeaEntity) {
            Global.player.rpc.Cmd_SpawnPirateShip(Util.getMousePos(), BotShipEntity.PRIVATEERS_GUILD_ID);
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
