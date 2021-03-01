using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using TMPro;

public class VoyageStatusPanel : ClientMonoBehaviour
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

   // The PvP-PvE text
   public TextMeshProUGUI pvpPveText;

   // The player count text
   public TextMeshProUGUI playerCountText;

   // The player count in town text
   public TextMeshProUGUI playerCountTownText;

   // The time since the voyage started
   public TextMeshProUGUI timeText;

   // The treasure site count text
   public TextMeshProUGUI treasureSiteCountText;

   // The alive enemies count
   public TextMeshProUGUI aliveEnemiesCountText;

   // The index of the current league
   public TextMeshProUGUI leagueIndexText;

   // The lobby label
   public TextMeshProUGUI lobbyText;

   // The icons for PvP and PvE modes
   public Sprite pvpIcon;
   public Sprite pveIcon;

   // The statuses that are only relevant in voyage instances or league instances (common statuses should not be set here)
   public GameObject[] voyageStatuses = new GameObject[0];
   public GameObject[] leagueStatuses = new GameObject[0];
   public GameObject[] lobbyStatuses = new GameObject[0];
   public GameObject[] townStatuses = new GameObject[0];
   public GameObject[] treasureSiteStatuses = new GameObject[0];

   // Self
   public static VoyageStatusPanel self;

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
      if (RectTransformUtility.RectangleContainsScreenPoint(panelHoveringZone, Input.mousePosition)) {
         collapsingContainer.SetActive(true);
      } else {
         collapsingContainer.SetActive(false);
      }

      // Get the current instance
      Instance instance = Global.player.getInstance();
      if (instance == null) {
         return;
      }

      // Show different relevant statuses depending on the area type
      if (VoyageGroupManager.isInGroup(Global.player)) {
         if (instance.isLeague && VoyageManager.isLobbyArea(instance.areaKey)) {
            voyageStatuses.Hide();
            leagueStatuses.Hide();
            townStatuses.Hide();
            treasureSiteStatuses.Hide();
            lobbyStatuses.Show();
         } else if (instance.isLeague && !VoyageManager.isLobbyArea(instance.areaKey)) {
            voyageStatuses.Hide();
            lobbyStatuses.Hide();
            townStatuses.Hide();
            treasureSiteStatuses.Hide();
            leagueStatuses.Show();
         } else if (VoyageManager.isTreasureSiteArea(instance.areaKey)) {
            voyageStatuses.Hide();
            lobbyStatuses.Hide();
            townStatuses.Hide();
            leagueStatuses.Hide();
            treasureSiteStatuses.Show();

            // Disable the empty collapsing container (causes the panel to slightly resize when hovering it)
            collapsingContainer.SetActive(false);
         } else if (instance.isVoyage) {
            lobbyStatuses.Hide();
            leagueStatuses.Hide();
            townStatuses.Hide();
            treasureSiteStatuses.Hide();
            voyageStatuses.Show();
         } else {
            setDefaultStatus();
         }
      } else {
         setDefaultStatus();
      }

      playerCountTownText.text = EntityManager.self.getEntityCount() + "/" + instance.getMaxPlayers();

      // If the player is not in a group, there is no need to update the rest
      if (!VoyageGroupManager.isInGroup(Global.player)) {
         return;
      }

      // Set the status that are always visible
      if (instance.treasureSiteCount == 0) {
         // If the number of sites is 0, the area has not yet been instantiated on the server
         treasureSiteCountText.text = "?/?";
      } else {
         treasureSiteCountText.text = (instance.treasureSiteCount - instance.capturedTreasureSiteCount).ToString() + "/" + instance.treasureSiteCount;
      }
      aliveEnemiesCountText.text = instance.aliveNPCEnemiesCount.ToString() + "/" + (instance.getTotalNPCEnemyCount()).ToString();
      lobbyText.text = Voyage.getLeagueAreaName(instance.leagueIndex);

      // If the panel is collapsed, there is no need to update the rest
      if (!collapsingContainer.activeSelf) {
         return;
      }

      // Update the voyage status
      //mapNameText.text = Area.getName(instance.areaKey);
      pvpPveText.text = instance.isPvP ? "PvP" : "PvE";
      pvpPveModeImage.sprite = instance.isPvP ? pvpIcon : pveIcon;
      playerCountText.text = EntityManager.self.getEntityCount() + "/" + instance.getMaxPlayers();
      timeText.text = (DateTime.UtcNow - DateTime.FromBinary(instance.creationDate)).ToString(@"mm\:ss");
      leagueIndexText.text = Voyage.getLeagueAreaName(instance.leagueIndex);
   }

   private void setDefaultStatus () {
      lobbyStatuses.Hide();
      leagueStatuses.Hide();
      voyageStatuses.Hide();
      treasureSiteStatuses.Hide();
      townStatuses.Show();

      playerCountTownText.text = "?/?";

      // Disable the empty collapsing container (causes the panel to slightly resize when hovering it)
      collapsingContainer.SetActive(false);
   }

   public void show () {
      if (this.canvasGroup.alpha < 1f) {
         this.canvasGroup.alpha = 1f;
         this.canvasGroup.blocksRaycasts = true;
         this.canvasGroup.interactable = true;
         setDefaultStatus();
      }
   }

   public void hide () {
      if (this.canvasGroup.alpha > 0f) {
         this.canvasGroup.alpha = 0f;
         this.canvasGroup.blocksRaycasts = false;
         this.canvasGroup.interactable = false;
         setDefaultStatus();
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   #endregion
}
