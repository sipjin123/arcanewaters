using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class AdminBattlePanel : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The parameter rows
   public AdminBattleParamRow idleAnimSpeedRow;
   public AdminBattleParamRow jumpDurationRow;
   public AdminBattleParamRow attackDurationRow;
   public AdminBattleParamRow attackCooldownRow;

   // A message that displays when the changes have been applied
   public CanvasGroup changesAppliedMessage;

   // Self
   public static AdminBattlePanel self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void togglePanel () {
      if (Global.player == null || !Global.player.isAdmin()) {
         return;
      }

      if (!isShowing()) {
         refreshPanel();
         show();
      } else {
         hide();
      }
   }

   public void refreshPanel () {
      idleAnimSpeedRow.initialize(AdminBattleManager.self.idleAnimationSpeedMultiplier);
      jumpDurationRow.initialize(AdminBattleManager.self.jumpDurationMultiplier);
      attackDurationRow.initialize(AdminBattleManager.self.attackDurationMultiplier);
      attackCooldownRow.initialize(AdminBattleManager.self.attackCooldownMultiplier);
      changesAppliedMessage.Show();
   }

   public void onParameterRowChanged () {
      if (hasAtLeastOneParameterChanged()) {
         StopAllCoroutines();
         StartCoroutine(CO_ApplyChangesAfterDelay());
         changesAppliedMessage.Hide();
      } else {
         changesAppliedMessage.Show();
      }
   }

   private IEnumerator CO_ApplyChangesAfterDelay () {
      yield return new WaitForSeconds(1f);
      Global.player.rpc.Cmd_SetAdminBattleParameters(idleAnimSpeedRow.getValue(), jumpDurationRow.getValue(), attackDurationRow.getValue(), attackCooldownRow.getValue());
      changesAppliedMessage.Show();
   }

   private bool hasAtLeastOneParameterChanged () {
      return idleAnimSpeedRow.hasChanged()
         || jumpDurationRow.hasChanged()
         || attackDurationRow.hasChanged()
         || attackCooldownRow.hasChanged();
   }

   public void onUserLogOut () {
      hide();
   }

   public void show () {
      this.canvasGroup.Show();
      this.gameObject.SetActive(true);
   }

   public void hide () {
      StopAllCoroutines();
      this.canvasGroup.Hide();
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   #endregion
}

