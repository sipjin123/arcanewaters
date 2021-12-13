using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using UnityEngine.Events;
using DG.Tweening;

public class RespawnScreen : MonoBehaviour {
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

   // Reference to the content of the screen
   public RectTransform content;

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
      if (Global.player != null) {
         PlayerShipEntity playerShip = Global.player.getPlayerShipEntity();

         // If the player dies in a pvp or league area, respawn them in the same map
         if (VoyageManager.isPvpArenaArea(playerShip.areaKey) || VoyageManager.isAnyLeagueArea(playerShip.areaKey)) {
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

   public void show () {
      if (!this.canvasGroup.IsShowing()) {
         // Hide tutorial panel so it doesn't block the respawn button
         if (TutorialManager3.self.panel.getMode() != TutorialPanel3.Mode.Closed) {
            TutorialManager3.self.panel.gameObject.SetActive(false);
         }

         updateButtonText();
         content.gameObject.SetActive(true);
         this.canvasGroup.Show();

         tryShowCountdown();
      }
   }

   private void tryShowCountdown () {
      if (Global.player != null && !PanelManager.self.countdownScreen.isShowing() && VoyageManager.isPvpArenaArea(Global.player.areaKey)) {
         // If the countdown should be displayed, hide the Respawn Screen's content
         content.gameObject.SetActive(false);

         try {
            Global.player.rpc.Cmd_RequestPvpRespawnTimeout();
         } catch {
            content.gameObject.SetActive(true);
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
      } else if (Global.player != null && VoyageManager.isAnyLeagueArea(Global.player.areaKey)) {
         buttonText.text = BUTTON_TEXT_LEAGUE;
      } else {
         buttonText.text = BUTTON_TEXT_NORMAL;
      }
   }

   public void onRespawnTimeoutReceived (float timeout) {
      PanelManager.self.countdownScreen.customText.text = "Respawning in:";
      PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
      PanelManager.self.countdownScreen.seconds = timeout;
      PanelManager.self.countdownScreen.toggleCancelButton(false);
      PanelManager.self.countdownScreen.show();

      InvokeRepeating(nameof(checkCountdown), 0.0f, 1.0f);
   }

   private void checkCountdown () {
      if (PanelManager.self.countdownScreen.isShowing()) {
         return;
      }

      CancelInvoke(nameof(checkCountdown));
      onRespawnButtonPress();
   }

   #region Private Variables

   // The number of seconds the player has been dead
   private float _deadTime = 0;

   // The text to be displayed on the button by default
   private const string BUTTON_TEXT_NORMAL = "Go Home";

   // The text to be displayed on the button when in a pvp game
   private const string BUTTON_TEXT_PVP = "Respawn";

   // The text to be displayed on the button when in a league area
   private const string BUTTON_TEXT_LEAGUE = "Respawn";

   #endregion
}