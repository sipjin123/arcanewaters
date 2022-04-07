using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;

public class GuildPanel : Panel {
   #region Public Variables

   // The guild id
   public int guildId;

   // The button for editing ranks in guild
   public Button ranksButton;

   // The button that will take a user to their guild map
   public Button guildMapbutton;

   // The button that open the guild inventory
   public Button guildInventoryButton;

   // The button for creating a guild
   public Button createButton;

   // The button for leaving a guild
   public Button leaveButton;

   // The container for our guild member list
   public GameObject memberContainer;

   // The prefab we use for creating a guild member row
   public GuildMemberRow guildMemberPrefab;

   // Our various texts
   public Text nameText;
   public Text dateText;
   public Text levelText;
   public Text countText;

   // Our various images
   public Image flagImage;

   // The guild icon
   public GuildIcon guildIcon;

   // The guild creation panel
   public GuildCreatePanel guildCreatePanel;

   // The guild ranks panel
   public GuildRanksPanel guildRanksPanel;

   [Header("Guild actions")]
   // Visual separator above "Ranks"
   public GameObject horizontalSeparatorLeft;

   // Contains all action buttons
   public GameObject actionButtonsContainer;

   // The button for promoting guild member to higher rank
   public Button promoteButton;

   // The button for demoting guild member to lower rank
   public Button demoteButton;

   // The button for kicking guild member from guild
   public Button kickButton;

   // The button for appointing guild member to be new guild leader
   public Button appointLeaderButton;

   [Header("Guild alliance")]

   // Shows the guild panel
   public Button showGuildAllyPanelButton;

   // The ui load blocker
   public GameObject guildAllyLoadBlocker;

   // The guild ally panel
   public GameObject guildAlliesPanel;

   // Closes the guild ally panel
   public Button closeGuildAllyPanelButton;

   // Prefab container
   public GameObject guildAllyEntryParent;

   // Prefab containing guild ally info
   public GuildAllyInfoTemplate guildAllyEntryPrefab;

   [Header("Sorting icons")]
   // Icons near name label describing sorting order
   public GameObject nameAsc;
   public GameObject nameDesc;

   // Icons near rank label describing sorting order
   public GameObject rankAsc;
   public GameObject rankDesc;

   // Icons near current map label describing sorting order
   public GameObject zoneAsc;
   public GameObject zoneDesc;

   // Icons near last logged-in date label describing sorting order
   public GameObject dateAsc;
   public GameObject dateDesc;

   // Icons near level label describing sorting order
   public GameObject levelAsc;
   public GameObject levelDesc;

   [Header("Others")]
   // Objects which are visible when player is not currently in guild
   public GameObject[] notInGuildObjects;

   // Decarations used for player who is not a leader
   public GameObject bottomDecarationNoLeader;

   // Sort Direction
   public enum SortDirection
   {
      // None
      None = 0,

      // Ascending order
      Ascending = 1,

      // Descending order
      Descending = 2
   }

   // Columns used for sorting
   public enum SortedColumn
   {
      // None
      None = 0,

      // Name
      Name = 1,

      // Level
      Level = 2,

      // rank
      Rank = 3,

      // Zone
      Zone = 4,

      // Date
      Date = 5
   }

   // Self
   public static GuildPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;

      showGuildAllyPanelButton.onClick.AddListener(() => showGuildAllies());
      closeGuildAllyPanelButton.onClick.AddListener(() => closeGuildAlliesPanel());
   }

   public void closeGuildAlliesPanel () {
      guildAlliesPanel.SetActive(false);
   }

   private void showGuildAllies () {
      if (Global.player == null) {
         return;
      }
      guildAllyLoadBlocker.SetActive(true);
      Global.player.rpc.Cmd_GetGuildAllies(guildId);
   }

   public void receiveGuildAlliesFromServer (List<GuildInfo> guildAlliesInfo) {
      guildAllyEntryParent.gameObject.DestroyChildren();
      foreach (GuildInfo guildInfo in guildAlliesInfo) {
         GuildAllyInfoTemplate allyInfo = Instantiate(guildAllyEntryPrefab, guildAllyEntryParent.transform);
         allyInfo.setGuildInfo(guildInfo);
         bool canRemoveGuildAlliances = Global.player == null ? false : Global.player.guildRankPriority <= 1;
         allyInfo.removeButtonObj.SetActive(canRemoveGuildAlliances);
      }
      guildAlliesPanel.SetActive(true);
      guildAllyLoadBlocker.SetActive(false);
   }

   public void receiveDataFromServer (GuildInfo info, GuildRankInfo[] guildRanks) {
      guildAlliesPanel.SetActive(false);
      bool inGuild = Global.player.guildId != 0;

      // Disable and enable images
      guildIcon.gameObject.SetActive(inGuild);
      flagImage.enabled = inGuild;

      // Activates buttons depending on user being in guild
      createButton.gameObject.SetActive(!inGuild);
      leaveButton.gameObject.SetActive(inGuild);
      guildInventoryButton.gameObject.SetActive(inGuild);
      ranksButton.gameObject.SetActive(inGuild);
      bottomDecarationNoLeader.SetActive(inGuild);
      guildMapbutton.gameObject.SetActive(inGuild);
      showGuildAllyPanelButton.gameObject.SetActive(inGuild);
      foreach (GameObject obj in notInGuildObjects) {
         obj.SetActive(!inGuild);
      }

      horizontalSeparatorLeft.gameObject.SetActive(inGuild);
      actionButtonsContainer.gameObject.SetActive(inGuild);

      // Fill in the texts
      nameText.text = inGuild ? info.guildName : "";
      dateText.text = inGuild ? DateTime.FromBinary(info.creationTime).ToString("MMMM yyyy") : "";
      levelText.text = "Level " + (inGuild ? "1" : "");
      countText.text = inGuild ? ("Members: " + info.guildMembers.Length + " / " + GuildManager.MAX_MEMBERS) : "";

      // Set the guild icon
      if (inGuild) {
         guildIcon.initialize(info);
         guildId = info.guildId;
      }

      // Clear out any old member info
      memberContainer.DestroyChildren();

      _guildMemberRowsReference.Clear();
      if (info.guildMembers != null) {
         foreach (UserInfo member in info.guildMembers) {
            GuildMemberRow memberRow = Instantiate(guildMemberPrefab, memberContainer.transform);
            memberRow.setRowForGuildMember(member, guildRanks);
            _guildMemberRowsReference.Add(memberRow);
         }
      }

      // Cache local player permissions for GUI purposes
      if (guildRanks != null && Global.player != null) {
         int rankId = -1;
         foreach (UserInfo member in info.guildMembers) {
            if (Global.player.userId == member.userId) {
               rankId = member.guildRankId;
               break;
            }
         }

         bottomDecarationNoLeader.SetActive(rankId != 0);

         // Guild leader has all permissions
         if (rankId == 0) {
            Global.player.guildPermissions = int.MaxValue;
            Global.player.guildRankPriority = 0;
         } else {
            foreach (GuildRankInfo rank in guildRanks) {
               if (rank.id == rankId) {
                  Global.player.guildPermissions = rank.permissions;
                  Global.player.guildRankPriority = rank.rankPriority;
                  break;
               }
            }
         }
      }

      // Fill guild ranks data
      if (guildRanks != null) {
         guildRanksPanel.initialize(guildRanks);
      }

      // Update buttons interactivity
      checkButtonPermissions();

      // Sync order
      sort(_sortedColumn, _sortDirection);
   }

   public void createGuildPressed () {
      guildCreatePanel.show();
   }

   public void ranksGuildPressed () {
      guildRanksPanel.show();
   }

   public void inventoryPressed () {
      GuildInventoryPanel.self.show();
   }

   public void leaveGuildPressed () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmedLeaveGuild());

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to leave your guild?");
   }

   protected void confirmedLeaveGuild () {
      Global.player.rpc.Cmd_LeaveGuild();

      // Exit all panels
      PanelManager.self.confirmScreen.hide();
      if (Global.player.guildRankPriority != 0) {
         PanelManager.self.unlinkPanel();
      }
   }

   public void checkButtonPermissions () {
      bool isActive = (_guildMemberRowsReference.Find(row => row.highlightRow.activeSelf) != null);
      promoteButton.interactable = isActive && Global.player.canPerformAction(GuildPermission.Promote);
      demoteButton.interactable = isActive && Global.player.canPerformAction(GuildPermission.Demote);
      kickButton.interactable = isActive && Global.player.canPerformAction(GuildPermission.Kick);
      appointLeaderButton.gameObject.SetActive(Global.player.guildId != 0 && Global.player.guildRankPriority == 0);
      appointLeaderButton.interactable = isActive;

      ranksButton.interactable = Global.player.canPerformAction(GuildPermission.EditRanks);
   }

   public List<GuildMemberRow> getGuildMemberRows () {
      return _guildMemberRowsReference;
   }

   public override void hide () {
      base.hide();
      guildRanksPanel.hide();
      guildCreatePanel.hide();
      GuildInventoryPanel.self.hide();
   }

   public void promoteButtonClicked () {
      if (Global.player == null) {
         return;
      }

      GuildMemberRow row = _guildMemberRowsReference.Find(x => x.highlightRow.activeSelf);
      if (row != null && !checkIfActionOnSelf(row)) {
         Global.player.rpc.Cmd_PromoteGuildMember(row.getUserId());
      }
   }

   public void demoteButtonClicked () {
      if (Global.player == null) {
         return;
      }

      GuildMemberRow row = _guildMemberRowsReference.Find(x => x.highlightRow.activeSelf);
      if (row != null && !checkIfActionOnSelf(row)) {
         Global.player.rpc.Cmd_DemoteGuildMember(row.getUserId());
      }
   }

   public void kickButtonClicked () {
      if (Global.player == null) {
         return;
      }

      GuildMemberRow row = _guildMemberRowsReference.Find(x => x.highlightRow.activeSelf);
      if (row != null && !checkIfActionOnSelf(row)) {
         Global.player.rpc.Cmd_KickGuildMember(row.getUserId());
      }
   }

   public void appointLeaderButtonClicked () {
      if (Global.player == null) {
         return;
      }

      GuildMemberRow row = _guildMemberRowsReference.Find(x => x.highlightRow.activeSelf);
      if (row != null && !checkIfActionOnSelf(row)) {
         Global.player.rpc.Cmd_AppointGuildLeader(row.getUserId());
      }
   }

   public void goToGuildMapButtonClicked () {
      Global.player.Cmd_GoToGuildMap();
      hide();
   }

   #region Sorting

   public void sortByName () {
      sort(SortedColumn.Name, computeNextSortDirection(SortedColumn.Name));
   }

   public void sortByRank () {
      sort(SortedColumn.Rank, computeNextSortDirection(SortedColumn.Rank));
   }

   public void sortByZone () {
      sort(SortedColumn.Zone, computeNextSortDirection(SortedColumn.Zone));
   }

   public void sortByDate () {
      sort(SortedColumn.Date, computeNextSortDirection(SortedColumn.Date));
   }

   public void sortByLevel () {
      sort(SortedColumn.Level, computeNextSortDirection(SortedColumn.Level));
   }

   public void sortByNone () {
      sort(SortedColumn.None, computeNextSortDirection(SortedColumn.None));
   }

   private SortDirection computeNextSortDirection (SortedColumn column) {
      SortDirection direction = _sortDirection;

      if (_sortedColumn != column) {
         direction = SortDirection.None;
      }

      switch (direction) {
         case SortDirection.None:
         case SortDirection.Descending:
            return SortDirection.Ascending;
         case SortDirection.Ascending:
            return SortDirection.Descending;
         default:
            return SortDirection.None;
      }
   }

   public void sort (SortedColumn column, SortDirection direction) {
      _sortDirection = direction;
      _sortedColumn = column;

      switch (column) {
         case SortedColumn.None:
            _sortedColumn = SortedColumn.None;
            _sortDirection = SortDirection.None;
            disableAllSortIcons();
            break;
         case SortedColumn.Name:
            toggleIcons(nameAsc, nameDesc);
            _guildMemberRowsReference.Sort((a, b) => {
               return _sortDirection == SortDirection.Ascending ? a.memberName.text.CompareTo(b.memberName.text) : b.memberName.text.CompareTo(a.memberName.text);
            });
            break;
         case SortedColumn.Level:
            toggleIcons(levelAsc, levelDesc);
            _guildMemberRowsReference.Sort((a, b) => {
               return _sortDirection == SortDirection.Ascending ? a.memberLevel.text.CompareTo(b.memberLevel.text) : b.memberLevel.text.CompareTo(a.memberLevel.text);
            });
            break;
         case SortedColumn.Rank:
            toggleIcons(rankAsc, rankDesc);
            _guildMemberRowsReference.Sort((a, b) => {
               return _sortDirection == SortDirection.Ascending ? a.memberRankName.text.CompareTo(b.memberRankName.text) : b.memberRankName.text.CompareTo(a.memberRankName.text);
            });
            break;
         case SortedColumn.Zone:
            toggleIcons(zoneAsc, zoneDesc);
            _guildMemberRowsReference.Sort((a, b) => {
               return _sortDirection == SortDirection.Ascending ? a.memberZone.text.CompareTo(b.memberZone.text) : b.memberZone.text.CompareTo(a.memberZone.text);
            });
            break;
         case SortedColumn.Date:
            toggleIcons(dateAsc, dateDesc);
            _guildMemberRowsReference.Sort((a, b) => {
               return _sortDirection == SortDirection.Ascending ? a.lastActiveInMinutes.CompareTo(b.lastActiveInMinutes) : b.lastActiveInMinutes.CompareTo(a.lastActiveInMinutes);
            });
            break;
      }

      orderRows();
   }

   #endregion

   private void toggleIcons (GameObject asc, GameObject desc) {
      disableAllSortIcons();
      asc.SetActive(this._sortDirection == SortDirection.Ascending);
      desc.SetActive(this._sortDirection == SortDirection.Descending);
   }

   private void disableAllSortIcons () {
      nameAsc.SetActive(false);
      nameDesc.SetActive(false);
      rankAsc.SetActive(false);
      rankDesc.SetActive(false);
      zoneAsc.SetActive(false);
      zoneDesc.SetActive(false);
      levelAsc.SetActive(false);
      levelDesc.SetActive(false);
      dateAsc.SetActive(false);
      dateDesc.SetActive(false);
   }

   private void orderRows () {
      if (_guildMemberRowsReference == null || _guildMemberRowsReference.Count == 0) {
         return;
      }

      Transform parent = _guildMemberRowsReference[0].transform.parent;

      if (parent == null) {
         return;
      }

      parent.DetachChildren();

      foreach (GuildMemberRow row in _guildMemberRowsReference) {
         row.transform.SetParent(parent);
      }
   }

   private bool checkIfActionOnSelf (GuildMemberRow row) {
      if (Global.player.userId == row.getUserId()) {
         PanelManager.self.noticeScreen.show("You cannot perform action on yourself!");
         return true;
      }
      return false;
   }

   #region Private Variables

   // References to the objects representing guild members
   private List<GuildMemberRow> _guildMemberRowsReference = new List<GuildMemberRow>();

   // The current sort direction
   private SortDirection _sortDirection;

   // The column used for sorting
   private SortedColumn _sortedColumn;

   #endregion
}
