using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using UnityEngine.Events;

public class RespawnScreen : MonoBehaviour
{
   #region Public Variables

   // The number of seconds after the player ship dies before the screen is shown
   public static float SHOW_DELAY = 3f;

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
   }

   public void onRespawnButtonPress() {
      if (Global.player != null && Global.player.isPlayerShip()) {
         ((PlayerShipEntity) Global.player).requestRespawn();
      }
      _deadTime = 0;
      hide();
   }

   public void show () {
      if (!this.canvasGroup.IsShowing()) {
         this.canvasGroup.Show();
      }
   }

   public void hide () {
      if (this.canvasGroup.IsShowing()) {
         this.canvasGroup.Hide();
      }
   }

   public bool isShowing () {
      return this.canvasGroup.IsShowing();
   }

   #region Private Variables

   // The number of seconds the player has been dead
   private float _deadTime = 0;

   #endregion
}