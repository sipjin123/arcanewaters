using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackPanel : MonoBehaviour {
   #region Public Variables

   // The selected ability index
   public int selectedAbilityIndex = 0;

   // The recent ability clicked on
   public AbilityRequest recentAbilityRequest = new AbilityRequest();

   // Reference to self
   public static AttackPanel self;

   public class AbilityRequest {
      public AbilityType abilityType;
      public uint targetNetId;
      public int abilityIndex;
   }

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _button = GetComponentInChildren<Button>();
   }

   public void clearCachedAbilityCast () {
      recentAbilityRequest.abilityType = AbilityType.Undefined;
      recentAbilityRequest.abilityIndex = -1;
   }

   public void cancelAbility (AbilityType abilityType, int abilityIndex) {
      Battler target = BattleSelectionManager.self.selectedBattler;

      // We have to have a target to attack
      if (target == null) {
         return;
      }

      // Send the request to the server
      Global.player.rpc.Cmd_RequestAbility((int)abilityType, target.netId, abilityIndex, true);
   }

   // Currently only used in the UI for the local client
   public void requestAttackTarget (int abilityIndex) {
      Battler target = BattleSelectionManager.self.selectedBattler;

      // We have to have a target to attack
      if (target == null) {
         return;
      }

      // Cancels recent ability triggered
      if (isValidRecentAbility()) {
         cancelAbility(recentAbilityRequest.abilityType, recentAbilityRequest.abilityIndex);
      }

      // Send the request to the server
      recentAbilityRequest.abilityType = AbilityType.Standard;
      recentAbilityRequest.targetNetId = target.netId;
      recentAbilityRequest.abilityIndex = abilityIndex;

      if (BattleManager.self.getPlayerBattler().canCastAbility()) {
         BattleManager.self.getPlayerBattler().setBattlerCanCastAbility(false);

         Global.player.rpc.Cmd_RequestAbility((int) AbilityType.Standard, target.netId, abilityIndex, false);

         // Trigger the tutorial
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.AttackBattleTarget);
      } 
   }

   private bool isValidRecentAbility () {
      if (recentAbilityRequest.abilityType != AbilityType.Undefined) {
         return true;
      }
      return false;
   }

   // Currently only used in the UI for the local client
   public void requestBuffTarget (int abilityIndex) {
      Battler target = BattleSelectionManager.self.selectedBattler;

      // We have to have a target to attack
      if (target == null) {
         return;
      }

      // Cancels recent ability triggered
      if (isValidRecentAbility()) {
         cancelAbility(recentAbilityRequest.abilityType, recentAbilityRequest.abilityIndex);
      }

      // Send the request to the server
      recentAbilityRequest.abilityType = AbilityType.BuffDebuff;
      recentAbilityRequest.targetNetId = target.netId;
      recentAbilityRequest.abilityIndex = abilityIndex;

      // Send the request to the server
      if (BattleManager.self.getPlayerBattler().canCastAbility()) {
         recentAbilityRequest.abilityType = AbilityType.BuffDebuff;
         recentAbilityRequest.targetNetId = target.netId;
         recentAbilityRequest.abilityIndex = abilityIndex;
         Global.player.rpc.Cmd_RequestAbility((int) AbilityType.BuffDebuff, target.netId, abilityIndex, false);
      }
   }

   protected Battler getBattler () {
      // If we're not in a battle, there isn't one
      if (!Global.isInBattle()) {
         return null;
      }

      // If we already have it, we're done
      if (_battler != null && _battler.connectionToServer != null) {
         return _battler;
      }

      // Find our Battler and keep track of it
      foreach (Battler battler in FindObjectsOfType<Battler>()) {
         if (battler.player == Global.player) {
            _battler = battler;
         }
      }

      return _battler;
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our Button
   protected Button _button;

   // The player's Battler
   protected Battler _battler;
      
   #endregion
}
