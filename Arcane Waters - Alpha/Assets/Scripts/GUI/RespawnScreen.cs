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
      // Sync content visibility with canvas group interactable flag. UINavigation requirement.
      content.gameObject.SetActive(canvasGroup.interactable);

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

         if (playerShip != null) {         
            // If the player dies in a pvp or league area, respawn them in the same map
            if (!WorldMapManager.isWorldMapArea(playerShip.areaKey) && (VoyageManager.isPvpArenaArea(playerShip.areaKey) || VoyageManager.isAnyLeagueArea(playerShip.areaKey))) {
               D.adminLog("Player Ship Sending Request for Instance Respawn to server", D.ADMIN_LOG_TYPE.Respawn);
               setLifeboatVisibility(false);
               playerShip.Cmd_RespawnPlayerInInstance();
            } else {
               D.adminLog("Force Player Ship back to town", D.ADMIN_LOG_TYPE.Respawn);
               // Otherwise, return them to town
               respawnPlayerShipInTown(playerShip);
            }
         } else {
            D.adminLog("Warning! Respawning a Player Ship that does not exist!", D.ADMIN_LOG_TYPE.Respawn);
            Global.player.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.North);
         }
      } else {
         D.adminLog("Warning! Global player is Null!", D.ADMIN_LOG_TYPE.Respawn);
         D.debug("Global player is null, respawn failed.");
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
      if (!canvasGroup.IsShowing()) {
         // Hide tutorial panel so it doesn't block the respawn button
         if (TutorialManager3.self.panel.getMode() != TutorialPanel3.Mode.Closed) {
            TutorialManager3.self.panel.gameObject.SetActive(false);
         }

         updateButtonText();
         canvasGroup.interactable = true;
         canvasGroup.Show();

         // Hide all the extra content when the countdown starts
         if (tryShowCountdown()) {
            canvasGroup.interactable = false;
         }
      }
   }

   public void hide () {
      if (canvasGroup.IsShowing()) {
         canvasGroup.Hide();
      }

      setLifeboatVisibility(false);
   }

   public bool isShowing () {
      return canvasGroup.IsShowing();
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
      if (Global.player != null && VoyageManager.isPvpArenaArea(Global.player.areaKey) && !WorldMapManager.isWorldMapArea(Global.player.areaKey)) {
         buttonText.text = BUTTON_TEXT_PVP;
      } else if (Global.player != null && VoyageManager.isAnyLeagueArea(Global.player.areaKey)) {
         buttonText.text = BUTTON_TEXT_LEAGUE;
      } else {
         buttonText.text = BUTTON_TEXT_NORMAL;
      }
   }

   private bool tryShowCountdown () {
      // Do not show countdown for open world deaths
      if (WorldMapManager.isWorldMapArea(Global.player.areaKey)) {
         return false;
      }

      if (Global.player != null && !PanelManager.self.countdownScreen.isShowing() && VoyageManager.isPvpArenaArea(Global.player.areaKey)) {
         // Since the introduction of the UINavigation, the content can be hidden by making it not interactable
         Global.player.rpc.Cmd_RequestPvpRespawnTimeout();
         return true;
      }

      return false;
   }

   public void onRespawnTimeoutReceived (float timeout) {
      PanelManager.self.countdownScreen.customText.text = "Respawning in:";
      PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
      PanelManager.self.countdownScreen.seconds = timeout;
      PanelManager.self.countdownScreen.toggleCancelButton(false);
      PanelManager.self.countdownScreen.onCountdownEndEvent.AddListener(onRespawnButtonPress);
      PanelManager.self.countdownScreen.show();
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