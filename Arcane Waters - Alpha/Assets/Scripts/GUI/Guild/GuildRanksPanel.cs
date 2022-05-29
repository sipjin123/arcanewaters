using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Crosstales.BWF.Manager;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GuildRanksPanel : SubPanel
{
   #region Public Variables
   // Guild rank tabs parent
   public GameObject guildRankTabParent;
   
   // Guild rank tab prefab
   public GuildRankTab guildRankTabPrefab;
   
   // Guild rank permissions parent
   public GameObject guildRankPermissionsParent;
   
   // Guild rank permission prefab
   public GuildPermissionRow guildRankPermissionPrefab;
   
   // Rank name field
   public InputField rankNameField;

   // Button saving ranks permissions
   public Button saveRanksButton;

   // Self reference
   public static GuildRanksPanel self;

   #endregion

   private void Awake () {
      if (self == null) {
         self = this;
      } else {
         Destroy(this);
      }
   }

   public void initialize (GuildRankInfo[] guildRanks) {
      _selectedRankId = -1;
      this.gameObject.SetActive(true);
      _initialRanksData = guildRanks;

      initPermissionsRows();
      initializeTabs();
   }

   private void initializeTabs () {
      initTabs();

      _selectedRankId = -1;
      _guildRankTabs.First().Value.selectTab();
      // Update tab content
      updateTabContent();
      
   }
   
   public override void show () {
      initializeTabs();
      base.show();
   }

   public void selectRank (int rankId) {
      // Skip selection logic for currently selected tab
      if (rankId == _selectedRankId) {
         return;
      }
      
      // Pack permissions for previously selected rank
      packCurrentRankPermissions();

      // Store selected rank id
      _selectedRankId = rankId;

      // Deselect all tabs, new tab will be selected by caller
      foreach (var guildRankTab in _guildRankTabs.Values) {
         guildRankTab.deselectTab();
      }
      
      // Update tab content
      updateTabContent();
   }

   public void onRankDeleted (int rankId) {
      _guildRankTabs[rankId].gameObject.SetActive(false);
      Destroy(_guildRankTabs[rankId].gameObject, 3);
      _guildRankTabs.Remove(rankId);
      selectRank(_guildRankTabs.First().Key);
      if (_guildRankTabs.Keys.Count == 1) {
         _guildRankTabs.First().Value.disableDeleteButton();
      }
   }

   private void updateTabContent() {
      // Name
      rankNameField.text = _guildRankTabs[_selectedRankId].rankInfo.rankName;

      // Unpacking permissions for selected rank
      int permissions = _guildRankTabs[_selectedRankId].rankInfo.permissions;
      _guildPermissionRows[GuildPermission.Invite].statusToggle.isOn = GuildRankInfo.canPerformAction(permissions, GuildPermission.Invite);
      _guildPermissionRows[GuildPermission.Kick].statusToggle.isOn = GuildRankInfo.canPerformAction(permissions, GuildPermission.Kick);
      _guildPermissionRows[GuildPermission.OfficerChat].statusToggle.isOn = GuildRankInfo.canPerformAction(permissions, GuildPermission.OfficerChat);
      _guildPermissionRows[GuildPermission.Promote].statusToggle.isOn = GuildRankInfo.canPerformAction(permissions, GuildPermission.Promote);
      _guildPermissionRows[GuildPermission.Demote].statusToggle.isOn = GuildRankInfo.canPerformAction(permissions, GuildPermission.Demote);
      _guildPermissionRows[GuildPermission.EditRanks].statusToggle.isOn = GuildRankInfo.canPerformAction(permissions, GuildPermission.EditRanks);
      
      // Guild member has to have higher rank priority than ranks that he's trying to modify
      if (Global.player != null) {
         bool interactable = Global.player.guildRankPriority < _guildRankTabs[_selectedRankId].rankInfo.rankPriority;
         foreach (var guildPermissionRow in _guildPermissionRows.Values) {
            guildPermissionRow.statusToggle.interactable = interactable;
            guildPermissionRow.statusToggle.graphic.color = interactable ? _standardColor : _disabledColor;
         }
      }
   }

   private void packCurrentRankPermissions () {
      int permissions;
      if (_guildRankTabs.ContainsKey(_selectedRankId)) {
         permissions = 
            (_guildPermissionRows[GuildPermission.Invite].statusToggle.isOn ? (int) GuildPermission.Invite : 0) +
            (_guildPermissionRows[GuildPermission.Kick].statusToggle.isOn ? (int) GuildPermission.Kick : 0) +
            (_guildPermissionRows[GuildPermission.OfficerChat].statusToggle.isOn ? (int) GuildPermission.OfficerChat : 0) +
            (_guildPermissionRows[GuildPermission.Promote].statusToggle.isOn ? (int) GuildPermission.Promote : 0) +
            (_guildPermissionRows[GuildPermission.Demote].statusToggle.isOn ? (int) GuildPermission.Demote : 0) +
            (_guildPermissionRows[GuildPermission.EditRanks].statusToggle.isOn ? (int) GuildPermission.EditRanks : 0);      
         _guildRankTabs[_selectedRankId].rankInfo.permissions = permissions;
      }
   }
   
   private void initTabs () {
      // Remove old tabs
      for (int i = 0; i < guildRankTabParent.transform.childCount; i++) {
         Destroy(guildRankTabParent.transform.GetChild(i).gameObject);
      }

      // Add rank tabs
      _guildRankTabs = new Dictionary<int, GuildRankTab>();
      foreach (var rankInfo in _initialRanksData.OrderBy(x => x.rankId)) {
         GuildRankTab guildRankTab = Instantiate(guildRankTabPrefab, guildRankTabParent.transform);
         guildRankTab.init(rankInfo);
         _guildRankTabs.Add(rankInfo.id, guildRankTab);
      }
      
      // Set ranks order
      foreach (var rankTab in _guildRankTabs.Values) {
         rankTab.transform.SetSiblingIndex(rankTab.rankInfo.rankId);
      }
      
      // Disable deletion of single rank 
      if (_guildRankTabs.Keys.Count == 1) {
         _guildRankTabs.First().Value.disableDeleteButton();
      }
   }

   private void initPermissionsRows () {
      // Remove old permission rows
      for (int i = 0; i < guildRankPermissionsParent.transform.childCount; i++) {
         Destroy(guildRankPermissionsParent.transform.GetChild(i).gameObject);
      }

      // Add permissions rows
      _guildPermissionRows = new Dictionary<GuildPermission, GuildPermissionRow>();
      addPermission(GuildPermission.Invite, "Invite Member");
      addPermission(GuildPermission.Kick, "Kick Member");
      addPermission(GuildPermission.OfficerChat, "Officer Chat");
      addPermission(GuildPermission.Promote, "Promote Member");
      addPermission(GuildPermission.Demote, "Demote Member");
      addPermission(GuildPermission.EditRanks, "Edit Ranks");
   }
   
   private void addPermission (GuildPermission permission, string permissionName) {
      GuildPermissionRow guildPermissionRow = Instantiate(guildRankPermissionPrefab, guildRankPermissionsParent.transform);
      guildPermissionRow.init(permissionName);
      _guildPermissionRows.Add(permission, guildPermissionRow);
   }
   
   public void addRank () {
      if (_guildRankTabs.Count == MAXIMUM_NUMBER_OF_RANKS) {
         return;
      }

      // Add rank tab
      GuildRankInfo rankInfo = new GuildRankInfo();
      GuildRankTab guildRankTab = Instantiate(guildRankTabPrefab, guildRankTabParent.transform);
      guildRankTab.init(rankInfo);
      // Random negative rank id for new rank
      rankInfo.id = Random.Range(-20000, -10000);
      rankInfo.rankName = "new rank";
      rankInfo.rankId = guildRankTab.transform.GetSiblingIndex();
      
      _guildRankTabs.Add(rankInfo.id, guildRankTab);
      
      // Select new tab
      _guildRankTabs[rankInfo.id].selectTab();
   }

   public void onModifiedRankName () {
      _guildRankTabs[_selectedRankId].rankInfo.rankName = rankNameField.text.Trim().ToLower();
      _guildRankTabs[_selectedRankId].setName();
   }

   public void moveRankOrderLeft () {
      if (_guildRankTabs[_selectedRankId].transform.GetSiblingIndex() == 0) return;
      _guildRankTabs[_selectedRankId].transform.SetSiblingIndex(_guildRankTabs[_selectedRankId].transform.GetSiblingIndex()-1);
   }

   public void moveRankOrderRight () {
      _guildRankTabs[_selectedRankId].transform.SetSiblingIndex(_guildRankTabs[_selectedRankId].transform.GetSiblingIndex()+1);
   }
   
   public void onCancelButtonPressed () {
      hide();
   }

   public void saveRanks () {
      bool hasProhibitedWord = _guildRankTabs.Values.Any(item => BadWordManager.GetAll(item.rankInfo.rankName).Any());
      // Cancel guild rank creation if name contains prohibited word and show warning
      if (hasProhibitedWord) {
         PanelManager.self.noticeScreen.show(PROHIBITED_WORD_WARNING);
         return;
      }

      // Pack permissions for currently selected rank
      packCurrentRankPermissions();

      // Preparing rows to send
      List<GuildRankInfo> rowsToSend = new List<GuildRankInfo>();

      foreach (var guildRankTab in _guildRankTabs.Values) {
         // TODO: Yevgen - as for me, the rankPriority is obsolete, and rankId could be used instead for sorting ranks.
         guildRankTab.rankInfo.rankId = guildRankTab.transform.GetSiblingIndex()+1;
         guildRankTab.rankInfo.rankPriority = guildRankTab.rankInfo.rankId;
         guildRankTab.rankInfo.guildId = _initialRanksData[0].guildId;
         guildRankTab.rankInfo.id = guildRankTab.rankInfo.id < 0 ? -1 : guildRankTab.rankInfo.id;

         rowsToSend.Add(guildRankTab.rankInfo);
      }

      if (Global.player) {
         Global.player.rpc.Cmd_UpdateRanksGuild(rowsToSend.ToArray());
      }      
   }   

   #region Private Variables
   // Currently selected rank
   private int _selectedRankId;

   // Maximum number of ranks which can be definied within each guild
   private const int MAXIMUM_NUMBER_OF_RANKS = 4;

   // Message shown when prohibited word are detected in rank name
   private const string PROHIBITED_WORD_WARNING = "This name contains prohibited characters, please try a different name for the rank";

   // Data of guild ranks from databased - used to reset to initial state after cancelling changes
   private GuildRankInfo[] _initialRanksData;

   // UI tabs
   private Dictionary<int, GuildRankTab> _guildRankTabs;

   // Permissions
   private Dictionary<GuildPermission, GuildPermissionRow> _guildPermissionRows;
   
   // Standard toggle color
   private Color _standardColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

   // Color of toggle which is not interactable
   private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
   
   #endregion
}
