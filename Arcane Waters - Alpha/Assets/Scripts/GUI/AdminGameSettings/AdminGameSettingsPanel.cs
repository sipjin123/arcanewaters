using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class AdminGameSettingsPanel : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The parameter rows
   public AdminGameSettingsRow battleJumpDurationRow;
   public AdminGameSettingsRow battleAttackDurationRow;
   public AdminGameSettingsRow battleAttackCooldownRow;
   public AdminGameSettingsRow battleTimePerFrameRow;
   public AdminGameSettingsRow seaSpawnsPerSpotRow;
   public AdminGameSettingsRow seaAttackCooldownRow;
   public AdminGameSettingsRow seaMaxHealthRow;
   public AdminGameSettingsRow landBossAddedHealthRow;
   public AdminGameSettingsRow landBossAddedDamageRow;
   public AdminGameSettingsRow landDifficultyScalingRow; 

   // The number of enemy spawns per spawner in leagues after applying the admin parameter
   public Text spawnsPerSpot1;
   public Text spawnsPerSpot2;
   public Text spawnsPerSpot3;
   public Text spawnsPerSpot4;
   public Text spawnsPerSpot5;
   public Text spawnsPerSpot6;

   // A message that displays when the changes have been applied
   public CanvasGroup changesAppliedMessage;

   // Self
   public static AdminGameSettingsPanel self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void togglePanel () {
      if (Global.player == null || !Global.player.isAdmin()) {
         return;
      }

      if (!isShowing()) {
         SoundEffectManager.self.playGuiMenuOpenSfx();

         refreshPanel();
         show();
      } else {
         hide(true);
      }
   }

   public void refreshPanel () {
      battleJumpDurationRow.initialize(AdminGameSettingsManager.self.settings.battleJumpDuration);
      battleAttackCooldownRow.initialize(AdminGameSettingsManager.self.settings.battleAttackCooldown);
      battleAttackDurationRow.initialize(AdminGameSettingsManager.self.settings.battleAttackDuration);
      battleTimePerFrameRow.initialize(AdminGameSettingsManager.self.settings.battleTimePerFrame);
      seaSpawnsPerSpotRow.initialize(AdminGameSettingsManager.self.settings.seaSpawnsPerSpot);
      seaAttackCooldownRow.initialize(AdminGameSettingsManager.self.settings.seaAttackCooldown);
      seaMaxHealthRow.initialize(AdminGameSettingsManager.self.settings.seaMaxHealth);
      landBossAddedHealthRow.initialize(AdminGameSettingsManager.self.settings.bossHealthPerMember);
      landBossAddedDamageRow.initialize(AdminGameSettingsManager.self.settings.bossDamagePerMember);
      landDifficultyScalingRow.initialize(AdminGameSettingsManager.self.settings.landDifficultyScaling);

      updateSpawnsPerSpot();
      changesAppliedMessage.Show();
   }

   public void onParameterRowChanged () {
      StopAllCoroutines();
      StartCoroutine(CO_ApplyChangesAfterDelay());
      changesAppliedMessage.Hide();
      updateSpawnsPerSpot();
      _areChangesPending = true;
   }

   private IEnumerator CO_ApplyChangesAfterDelay () {
      yield return new WaitForSeconds(1f);
      applyChanges();
   }

   private void applyChanges () {
      AdminGameSettings settings = new AdminGameSettings(-1, DateTime.UtcNow,
         battleAttackCooldownRow.getValue(),
         battleJumpDurationRow.getValue(),
         battleAttackDurationRow.getValue(),
         battleTimePerFrameRow.getValue(),
         seaSpawnsPerSpotRow.getValue(), 
         seaAttackCooldownRow.getValue(),
         seaMaxHealthRow.getValue(),
         landBossAddedHealthRow.getValue(),
         landBossAddedDamageRow.getValue(),
         landDifficultyScalingRow.getValue());
      Global.player.rpc.Cmd_SetAdminGameSettings(settings);
      changesAppliedMessage.Show();
      _areChangesPending = false;
   }

   private void updateSpawnsPerSpot () {
      spawnsPerSpot1.text = EnemyManager.getSpawnsPerSpot(1, seaSpawnsPerSpotRow.getValue()).ToString();
      spawnsPerSpot2.text = EnemyManager.getSpawnsPerSpot(2, seaSpawnsPerSpotRow.getValue()).ToString();
      spawnsPerSpot3.text = EnemyManager.getSpawnsPerSpot(3, seaSpawnsPerSpotRow.getValue()).ToString();
      spawnsPerSpot4.text = EnemyManager.getSpawnsPerSpot(4, seaSpawnsPerSpotRow.getValue()).ToString();
      spawnsPerSpot5.text = EnemyManager.getSpawnsPerSpot(5, seaSpawnsPerSpotRow.getValue()).ToString();
      spawnsPerSpot6.text = EnemyManager.getSpawnsPerSpot(6, seaSpawnsPerSpotRow.getValue()).ToString();
   }

   public void onUserLogOut () {
      hide();
   }

   public void show () {
      this.canvasGroup.Show();
      this.gameObject.SetActive(true);
   }

   public void hide (bool shouldApplyPendingChanges = false) {
      StopAllCoroutines();

      if (shouldApplyPendingChanges && _areChangesPending) {
         applyChanges();
      }

      this.canvasGroup.Hide();
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // Gets set to true when changes are pending
   private bool _areChangesPending = false;

   #endregion
}


