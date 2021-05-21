using UnityEngine;
using System.Collections.Generic;
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

   // The apply button
   public Button applyButton;

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
   }

   public void onApplyButtonPressed () {
      Global.player.rpc.Cmd_SetAdminBattleParameters(idleAnimSpeedRow.getValue(), jumpDurationRow.getValue(), attackDurationRow.getValue(), attackCooldownRow.getValue());
      applyButton.interactable = false;
   }

   public void onUndoButtonPressed () {
      refreshPanel();
   }

   public void onParameterRowChanged () {
      if (hasAtLeastOneParameterChanged()) {
         applyButton.interactable = true;
      } else {
         applyButton.interactable = false;
      }
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
      this.canvasGroup.Hide();
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   #endregion
}

