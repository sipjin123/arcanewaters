using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using UnityEngine.Events;
using DG.Tweening;

public class RespawnScreen : MonoBehaviour
{
   #region Public Variables

   // The number of seconds after the player ship dies before the screen is shown
   public static float SHOW_DELAY = 3f;

   // The number of seconds after the player ship dies before the lifeboat is shown
   public static float LIFEBOAT_SHOW_DELAY = 1.0f;

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   #endregion

   public void Update () {
      if (Global.player == null || !Global.player.isPlayerShip() || !Global.player.isDead()) {
         _deadTime = 0;
         hide();
         return;
      }

      if (!isShowing()) {
         _deadTime += Time.deltaTime;
         if (_deadTime > SHOW_DELAY) {
            show();
         }
      }

      if (!isLifeboatShowing() && _deadTime > LIFEBOAT_SHOW_DELAY) {
         setLifeboatVisibility(true);
      }
   }

   public void onRespawnButtonPress() {
      if (Global.player != null && Global.player.isPlayerShip()) {
         ((PlayerShipEntity) Global.player).requestRespawn();

         // Re enable the tutorial panel
         if (TutorialManager3.self.panel.getMode() != TutorialPanel3.Mode.Closed) {
            TutorialManager3.self.panel.gameObject.SetActive(true);
         }
      }
      _deadTime = 0;
      hide();
   }

   public void show () {
      if (!this.canvasGroup.IsShowing()) {
         // Hide tutorial panel so it doesn't block the respawn button
         if (TutorialManager3.self.panel.getMode() != TutorialPanel3.Mode.Closed) {
            TutorialManager3.self.panel.gameObject.SetActive(false);
         }
         this.canvasGroup.Show();
      }
   }

   public void hide () {
      if (this.canvasGroup.IsShowing()) {
         this.canvasGroup.Hide();

         setLifeboatVisibility(false);
      }
   }

   public bool isShowing () {
      return this.canvasGroup.IsShowing();
   }

   public void setLifeboatVisibility (bool shouldShow) {
      if (Global.player != null) {
         PlayerShipEntity ship = Global.player.getPlayerShipEntity();
         if (ship != null) {
            ship.setLifeboatVisibility(shouldShow);
            ship.Cmd_SetLifeboatVisibility(shouldShow);
         }
      }
   }

   private bool isLifeboatShowing () {
      if (Global.player == null) {
         return false;
      }

      if (Global.player.getPlayerShipEntity() == null) {
         return false;
      }

      return Global.player.getPlayerShipEntity().lifeboat.activeInHierarchy;
   }

   #region Private Variables

   // The number of seconds the player has been dead
   private float _deadTime = 0;

   #endregion
}