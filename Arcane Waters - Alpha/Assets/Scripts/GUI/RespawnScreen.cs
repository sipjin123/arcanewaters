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

   // Singleton instance
   public static RespawnScreen self;

   // The number of seconds after the player ship dies before the screen is shown
   public static float SHOW_DELAY = 3f;

   // The number of seconds after the player ship dies before the lifeboat is shown
   public static float LIFEBOAT_SHOW_DELAY = 1.0f;

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // A reference to the text component displayed on the respawn screen button
   public Text buttonText;

   // Reference to the respawn button
   public Button button;

   #endregion

   private void Awake () {
      self = this;
   }

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

   public void onRespawnButtonPress () {
      if (PanelManager.self.countdownScreen.isShowing()) {
         return;
      }

      if (Global.player != null) {
         PlayerShipEntity playerShip = Global.player.getPlayerShipEntity();

         // If the player dies in a pvp area, respawn them in the same map
         if (VoyageManager.isPvpArenaArea(playerShip.areaKey)) {
            setLifeboatVisibility(false);
            playerShip.Cmd_RespawnPlayerInInstance();

            // Otherwise, return them to town
         } else {
            respawnPlayerShipInTown(playerShip);
         }
      }

      _deadTime = 0;
      hide();
   }

   private void onCountDownElapsed () {
      if (button == null) {
         return;
      }

      button.interactable = true;
   }

   public void respawnPlayerShipInTown (PlayerShipEntity playerShip) {
      playerShip.requestRespawn();

      // Re enable the tutorial panel
      if (TutorialManager3.self.panel.getMode() != TutorialPanel3.Mode.Closed) {
         TutorialManager3.self.panel.gameObject.SetActive(true);
      }

      // When dying in a voyage area, show a tip explaining how to return
      if (Global.player.isInGroup() && VoyageManager.isAnyLeagueArea(Global.player.areaKey)) {
         NotificationManager.self.add(Notification.Type.ReturnToVoyage);
      }
   }

   public void onRespawnTimeoutReceived (float timeout) {
      PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
      PanelManager.self.countdownScreen.onCountdownEndEvent.AddListener(onCountDownElapsed);
      PanelManager.self.countdownScreen.seconds = timeout;
      PanelManager.self.countdownScreen.toggleCancelButton(false);
      PanelManager.self.countdownScreen.show();
   }

   public void show () {
      if (!this.canvasGroup.IsShowing()) {
         // Hide tutorial panel so it doesn't block the respawn button
         if (TutorialManager3.self.panel.getMode() != TutorialPanel3.Mode.Closed) {
            TutorialManager3.self.panel.gameObject.SetActive(false);
         }

         updateButtonText();
         this.canvasGroup.Show();

         // Enable the button
         if (button != null) {
            button.interactable = true;
         }

         if (Global.player != null && !PanelManager.self.countdownScreen.isShowing() && VoyageManager.isPvpArenaArea(Global.player.areaKey)) {
            if (button != null) {
               button.interactable = false;
            }

            Global.player.rpc.Cmd_RequestPvpRespawnTimeout();
         }
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

   private void updateButtonText () {
      if (Global.player != null && VoyageManager.isPvpArenaArea(Global.player.areaKey)) {
         buttonText.text = BUTTON_TEXT_PVP;
      } else {
         buttonText.text = BUTTON_TEXT_NORMAL;
      }
   }

   #region Private Variables

   // The number of seconds the player has been dead
   private float _deadTime = 0;

   // The text to be displayed on the button by default
   private const string BUTTON_TEXT_NORMAL = "Go Home";

   // The text to be displayed on the button when in a pvp game
   private const string BUTTON_TEXT_PVP = "Respawn";

   #endregion
}