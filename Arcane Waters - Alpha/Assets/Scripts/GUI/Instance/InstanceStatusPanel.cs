using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using TMPro;
using UnityEngine.InputSystem;

public class InstanceStatusPanel : ClientMonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // When the mouse is over this defined zone, we consider that it hovers the panel
   public RectTransform panelHoveringZone;

   // The section that is only visible when the mouse is over the panel
   public GameObject collapsingContainer;

   // The PvP-PvE mode image
   public Image pvpPveModeImage;

   // The pvp open world icons
   public GameObject pvpOpenWorldRoyale, pvpOpenWorldGuild, pvpOpenWorldGroup, disabledPvpDisplay;

   // Determines if the pvp state is dropped down
   public bool isPvpStateDroppedDown;

   // The current pvp game mode
   public PvpGameMode currentPvpGameMode;

   // The general pvp buttons
   public Button[] pvpActiveButtons, pvpInactiveButtons;

   // The PvP-PvE text
   public TextMeshProUGUI pvpPveText;

   // The player count text
   public TextMeshProUGUI playerCountText;

   // The player count in town text
   public TextMeshProUGUI playerCountTownText;

   // The player count in pvp text
   public TextMeshProUGUI playerCountPvpText;

   // The time since the instance was created
   public TextMeshProUGUI timeText;

   // The treasure site count text
   public TextMeshProUGUI treasureSiteCountText;

   // The alive enemies count
   public TextMeshProUGUI aliveEnemiesCountText;

   // The index of the current league
   public TextMeshProUGUI leagueIndexText;

   // The lobby label
   public TextMeshProUGUI lobbyText;

   // The id of the current group instance for pvp instances
   public TextMeshProUGUI groupInstanceIdText;

   // The ping
   public TextMeshProUGUI pingText;

   // The fps
   public TextMeshProUGUI fpsText;

   // Tooltip text
   public TextMeshProUGUI pvpTooltipText;

   // The tooltip object holder
   public RectTransform tooltipObject;

   // The image of the expand button
   public Image expandButtonImage;

   // The icons for PvP and PvE modes
   public Sprite pvpIcon;
   public Sprite pveIcon;

   // The textures we choose from for the ping icon
   public Texture2D pingGreen;
   public Texture2D pingYellow;
   public Texture2D pingRed;

   // The animation for our ping icon
   public SimpleAnimation pingAnimation;

   // The expand and collapse sprites
   public Sprite expandButtonSprite;
   public Sprite collapseButtonSprite;

   // The statuses that are relevant in each instance type (common statuses should not be set here)
   public GameObject[] pvpStatuses = new GameObject[0];
   public GameObject[] leagueStatuses = new GameObject[0];
   public GameObject[] lobbyStatuses = new GameObject[0];
   public GameObject[] townStatuses = new GameObject[0];
   public GameObject[] treasureSiteStatuses = new GameObject[0];

   // The objects that blocks the pvp button
   public GameObject[] pvpStatBlockers;

   // Blocks pvp panel when out of town
   public GameObject pvpStaticBlocker;

   // The togglers indicating this user can participate in pvp
   public Toggle[] enablePvpTogglers;

   // Object containing the info of the pvp state
   public GameObject[] pvpStatTextDisplay;

   // The pvp stat display that will always show on top of screen
   public GameObject[] passivePvpStatDisplay;

   // Buttons that can be interacted
   public Button[] pvpInteractableButtons;

   // If the pvp mode is active
   public bool isPvpActive;

   // Self
   public static InstanceStatusPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      // Hide the panel by default
      hide();
   }

   public void Update () {
      // Hide the panel in certain situations
      if (Global.player == null || Global.player.isInBattle()) {
         hide();
         return;
      }

      // Show the panel
      show();

      // Collapse the panel if the mouse is not over it
      if (_isAlwaysExpanded || RectTransformUtility.RectangleContainsScreenPoint(panelHoveringZone, MouseUtils.mousePosition)) {
         collapsingContainer.SetActiveIfNeeded(true);
      } else {
         collapsingContainer.SetActiveIfNeeded(false);
      }

      // Get the current instance
      Instance instance = Global.player.getInstance();
      if (instance == null) {
         return;
      }

      playerCountTownText.text = EntityManager.self.getEntityCount() + (CustomMapManager.isPrivateCustomArea(instance.areaKey) ? "" : "/" + instance.getMaxPlayers());
      playerCountPvpText.text = EntityManager.self.getEntityCount().ToString();
      
      // Update the color of the ping image based on the current ping
      int ping = Util.getPing();
      if (ping <= 120) {
         pingAnimation.setNewTexture(pingGreen);
      } else if (ping <= 240) {
         pingAnimation.setNewTexture(pingYellow);
      } else {
         pingAnimation.setNewTexture(pingRed);

         // Log high ping
         if (_highPingMessageCooldown > 0) {
            _highPingMessageCooldown -= Time.deltaTime;
         }
         
         if (_highPingMessageCooldown <= 0) {
            D.debug($"Ping to the server is critically high: {ping}");
            _highPingMessageCooldown = 60; // Add log message once per 60 seconds
         }
      }
      pingText.text = ping.ToString();

      if (Time.time - _lastFPSUpdate > 0.1f) {
         _lastFPSUpdate = Time.time;
         fpsText.text = ((int) (1f / Time.deltaTime)).ToString();
      }

      // Track low fps
      if (_lowFpsMessageCooldown > 0) {
         _lowFpsMessageCooldown -= Time.deltaTime;
      }

      if (_lowFpsMessageCooldown <= 0) {
         int fps = (int) (1f / Time.deltaTime);
         if (fps <= 15) {
            D.debug($"Client fps is critically low: {fps}");
            _lowFpsMessageCooldown = 60; // Add log message once per 60 seconds
         }
      }

      // If the player is not in a group, there is no need to update the rest
      if (!GroupManager.isInGroup(Global.player)) {
         return;
      }

      // Set the status that are always visible
      if (instance.treasureSiteCount == 0) {
         // If the number of sites is 0, the area has not yet been instantiated on the server
         treasureSiteCountText.text = "?/?";
      } else {
         treasureSiteCountText.text = (instance.treasureSiteCount - instance.capturedTreasureSiteCount).ToString() + "/" + instance.treasureSiteCount;
      }
      string aliveEnemiesCount = (instance.aliveNPCEnemiesCount == -1) ? "0" : instance.aliveNPCEnemiesCount.ToString();
      aliveEnemiesCountText.text = aliveEnemiesCount + "/" + (instance.getTotalNPCEnemyCount()).ToString();
      lobbyText.text = GroupInstance.getLeagueAreaName(instance.leagueIndex);

      // If the panel is collapsed, there is no need to update the rest
      if (!collapsingContainer.activeSelf) {
         return;
      }

      // Update the instance status
      //mapNameText.text = Area.getName(instance.areaKey);
      pvpPveText.text = instance.isPvP ? "PvP" : "PvE";
      pvpPveModeImage.sprite = instance.isPvP ? pvpIcon : pveIcon;
      playerCountText.text = EntityManager.self.getEntityCount() + "/" + instance.getMaxPlayers();
      timeText.text = (DateTime.UtcNow - DateTime.FromBinary(instance.creationDate)).ToString(@"mm\:ss");
      leagueIndexText.text = GroupInstance.getLeagueAreaName(instance.leagueIndex);
      groupInstanceIdText.text = instance.groupInstanceId.ToString();
   }

   public void refreshPvpStatDisplay () {
      if (Global.player == null) {
         return;
      }

      foreach (GameObject obj in pvpStatBlockers) {
         obj.SetActive(true);
      }
      Global.player.rpc.Cmd_RequestPvpToggle(isPvpActive);
   }

   public void enablePvpStatDisplay (bool isOn) {
      // Toggles the drop down UI
      foreach (GameObject obj in pvpStatTextDisplay) {
         obj.gameObject.SetActive(isOn);
      }
      foreach (GameObject obj in passivePvpStatDisplay) {
         obj.gameObject.SetActive(!isOn);
      }

      isPvpStateDroppedDown = isOn;
      tooltipObject.gameObject.SetActive(false);
   }

   public void enableTooltipDisabled () {
      tooltipObject.localPosition = new Vector3(tooltipObject.localPosition.x, 0, 0);
      pvpTooltipText.text = "Pvp is currently unavailable for you";
      tooltipObject.gameObject.SetActive(true);
   }

   public void enableTooltipPvpOpenWorld (bool isActive) {
      tooltipObject.localPosition = new Vector3(tooltipObject.localPosition.x, isPvpStateDroppedDown ? -25 : 0, 0);

      if (isActive) {
         pvpTooltipText.text = "Pvp is currently active, this can be toggled off when user is in town";
      } else {
         pvpTooltipText.text = "Pvp is currently inactive, this can be toggled on when user is in town";
      }
      tooltipObject.gameObject.SetActive(true);
   }

   public void clearTooltip () {
      pvpTooltipText.text = "";
      tooltipObject.gameObject.SetActive(false);
   }

   public void togglePvpButtons (bool isActive) {
      foreach (Button button in pvpInteractableButtons) {
         button.enabled = isActive;
      }
   }

   public void clickOnPvpToggle () {
      // Attempt to change the pvp state
      if (Global.player == null) {
         return;
      }

      foreach (GameObject obj in pvpStatBlockers) {
         obj.SetActive(true);
      }
      Global.player.rpc.Cmd_RequestPvpToggle(!isPvpActive);
   }

   public void togglePvpStatusInfo (bool isOn, bool isInTown) {
      // Server content now sent to player to determine if pvp state is active or not
      foreach (Button pvpInactiveButton in pvpInactiveButtons) {
         pvpInactiveButton.gameObject.SetActive(!isOn);
      }
      foreach (Button pvpActiveButton in pvpActiveButtons) {
         pvpActiveButton.gameObject.SetActive(isOn);
      }

      pvpStaticBlocker.SetActive(!isInTown);

      foreach (Toggle enablePvpToggler in enablePvpTogglers) {
         enablePvpToggler.isOn = isOn;
      }
      isPvpActive = isOn;
      foreach (GameObject obj in pvpStatBlockers) {
         obj.SetActive(false);
      }
   }

   public void setUserPvpMode (PvpGameMode gameMode) {
      pvpOpenWorldRoyale.SetActive(false);
      pvpOpenWorldGroup.SetActive(false);
      pvpOpenWorldGuild.SetActive(false);
      disabledPvpDisplay.SetActive(false);

      switch (gameMode) {
         case PvpGameMode.FreeForAll:
            pvpOpenWorldRoyale.SetActive(true);
            break;
         case PvpGameMode.GroupWars:
            pvpOpenWorldGroup.SetActive(true);
            break;
         case PvpGameMode.GuildWars:
            pvpOpenWorldGuild.SetActive(true);
            break;
         default:
            disabledPvpDisplay.SetActive(true);
            break;
      }
      currentPvpGameMode = gameMode;
   }

   public void onUserSpawn () {
      // Get the current instance
      Instance instance = Global.player.getInstance();
      if (instance == null) {
         return;
      }

      // Show different relevant statuses depending on the area type
      if (GroupManager.isInGroup(Global.player)) {
         if (instance.isLeague && GroupInstanceManager.isLobbyArea(instance.areaKey)) {
            pvpStatuses.Hide();
            leagueStatuses.Hide();
            townStatuses.Hide();
            treasureSiteStatuses.Hide();
            lobbyStatuses.Show();
         } else if (instance.isLeague && !GroupInstanceManager.isLobbyArea(instance.areaKey)) {
            pvpStatuses.Hide();
            lobbyStatuses.Hide();
            townStatuses.Hide();
            treasureSiteStatuses.Hide();
            leagueStatuses.Show();
         } else if (GroupInstanceManager.isTreasureSiteArea(instance.areaKey)) {
            pvpStatuses.Hide();
            lobbyStatuses.Hide();
            townStatuses.Hide();
            leagueStatuses.Hide();
            treasureSiteStatuses.Show();
         } else if (instance.isPvP) {
            lobbyStatuses.Hide();
            leagueStatuses.Hide();
            townStatuses.Hide();
            treasureSiteStatuses.Hide();
            pvpStatuses.Show();
         } else {
            setDefaultStatus();
         }
      } else {
         setDefaultStatus();
      }
   }

   private void setDefaultStatus () {
      lobbyStatuses.Hide();
      leagueStatuses.Hide();
      pvpStatuses.Hide();
      treasureSiteStatuses.Hide();
      townStatuses.Show();

      playerCountTownText.text = "?/?";
   }

   public void onExpandButtonPressed () {
      _isAlwaysExpanded = !_isAlwaysExpanded;
      if (_isAlwaysExpanded) {
         expandButtonImage.sprite = collapseButtonSprite;
      } else {
         expandButtonImage.sprite = expandButtonSprite;
      }
   }

   public void show () {
      if (this.canvasGroup.alpha < 1f) {
         this.canvasGroup.alpha = 1f;
         this.canvasGroup.blocksRaycasts = true;
         this.canvasGroup.interactable = true;
      }
   }

   public void hide () {
      if (this.canvasGroup.alpha > 0f) {
         this.canvasGroup.alpha = 0f;
         this.canvasGroup.blocksRaycasts = false;
         this.canvasGroup.interactable = false;
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // Gets set to true when the panel is constantly expanded
   private bool _isAlwaysExpanded = false;
   
   // Low fps last message time cool down
   private float _lowFpsMessageCooldown;
   private float _highPingMessageCooldown;

   // Last time we updated FPS counter
   private float _lastFPSUpdate = 0;

   #endregion
}
