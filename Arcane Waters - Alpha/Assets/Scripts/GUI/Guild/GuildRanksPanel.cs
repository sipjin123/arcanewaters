using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class GuildRanksPanel : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // A convenient reference to the Canvas Group
   public CanvasGroup canvasGroup;

   // Button which adds new rank to edit panel
   public Button addRankButton;

   // Button saving ranks permissions
   public Button saveRanksButton;

   // Self reference
   public static GuildRanksPanel self;

   // Row gameobjects containing data about each rank within guild
   public List<GuildRankRow> guildRankRows = new List<GuildRankRow>();

   // Text which informs user that rank names are not distinct
   public GameObject rankNamesValidation;

   #endregion

   private void Awake () {
      if (self == null) {
         self = this;
      } else {
         Destroy(this);
      }
   }

   public void initialize (GuildRankInfo[] guildRanks) {
      this.gameObject.SetActive(true);
      _initialRanksData = guildRanks;
      resetRows();
      addRankButton.interactable = guildRanks.Length < MAXIMUM_NUMBER_OF_RANKS;
      guildRankRows.ForEach(row => row.initialize(null));

      foreach (GuildRankInfo info in guildRanks) {
         guildRankRows.Find(row => row.rankId == info.rankId).initialize(info);
      }
      orderRows();
      guildRankRows.ForEach(row => row.updatePriorityButtons());

      this.gameObject.SetActive(false);
   }

   public void saveRanks () {
      List<GuildRankInfo> rowsToSend = new List<GuildRankInfo>();

      foreach (GuildRankRow row in guildRankRows) {
         if (!row.isActive) {
            continue;
         }

         int permissions = (row.inviteToggle.isOn ? (int) GuildPermission.Invite : 0) +
                           (row.kickToggle.isOn ? (int) GuildPermission.Kick : 0) +
                           (row.officerChatToggle.isOn ? (int) GuildPermission.OfficerChat : 0) +
                           (row.promoteToggle.isOn ? (int) GuildPermission.Promote : 0) +
                           (row.demoteToggle.isOn ? (int) GuildPermission.Demote : 0) +
                           (row.editRanksToggle.isOn ? (int) GuildPermission.EditRanks : 0);

         GuildRankInfo info = new GuildRankInfo();
         info.rankId = row.rankId;
         info.rankPriority = row.rankPriority;
         info.rankName = row.rankName.text.Trim().ToLower();
         info.permissions = permissions;
         info.guildId = _initialRanksData[0].guildId;
         info.id = info.rankId - 1 < _initialRanksData.Length ? _initialRanksData[info.rankId - 1].id : -1;

         rowsToSend.Add(info);
      }

      if (Global.player) {
         Global.player.rpc.Cmd_UpdateRanksGuild(rowsToSend.ToArray());
      }
   }

   public void addRank () {
      int activeRanks = guildRankRows.FindAll(x => x.isActive).Count;
      if (activeRanks == MAXIMUM_NUMBER_OF_RANKS) {
         return;
      }

      GuildRankRow row = guildRankRows[activeRanks];
      row.gameObject.SetActive(true);
      row.isActive = true;

      // Set new rank as last row
      foreach (GuildRankRow rankRow in guildRankRows) {
         if (rankRow.transform.GetSiblingIndex() > row.transform.GetSiblingIndex()) {
            int rowIndex = row.transform.GetSiblingIndex();
            int otherRowIndex = rankRow.transform.GetSiblingIndex();
            row.transform.SetSiblingIndex(otherRowIndex);
            rankRow.transform.SetSiblingIndex(rowIndex);
         }
      }

      guildRankRows.ForEach(y => y.updatePriorityButtons());

      addRankButton.interactable = canAddNewRanks();
      hideAllDeleteButtons();
   }

   public void onCancelButtonPressed () {
      hide();

      // Reset rows data to previous state if "cancel" button was pressed
      resetRows();
      orderRows();
      guildRankRows.ForEach(row => row.updatePriorityButtons());
   }

   public void hideAllDeleteButtons () {
      foreach (GuildRankRow row in guildRankRows) {
         row.deleteRankButton.interactable = false;
      }
   }

   private void orderRows () {
      List<int> rankIDs = new List<int>();
      foreach (GuildRankRow rankRow in guildRankRows) {
         if (rankRow.isActive) {
            rankIDs.Add(rankRow.rankId);
         }
      }

      // Bubble sort rows order
      for (int i = 0; i < rankIDs.Count - 1; i++) {
         for (int j = 0; j < rankIDs.Count - 1 - i; j++) {
            GuildRankRow leftRow = guildRankRows.Find(x => x.rankId == rankIDs[j]);
            GuildRankRow rightRow = guildRankRows.Find(x => x.rankId == rankIDs[j + 1]);
            if (leftRow.rankPriority > rightRow.rankPriority) {
               int tmp = rankIDs[j];
               rankIDs[j] = rankIDs[j + 1];
               rankIDs[j + 1] = tmp;
            }
         }
      }

      int lowestIndex = int.MaxValue;
      foreach (GuildRankRow rankRow in guildRankRows) {
         if (rankRow.isActive) {
            if (rankRow.transform.GetSiblingIndex() < lowestIndex) {
               lowestIndex = rankRow.transform.GetSiblingIndex();
            }
         }
      }

      for (int i = 0; i < guildRankRows.Count; i++) {
         if (rankIDs.Count > i && guildRankRows[rankIDs[i] - 1].isActive) {
            guildRankRows[rankIDs[i] - 1].transform.SetSiblingIndex(lowestIndex + i);
         }
      }
   }

   private void resetRows () {
      for (int i = 0; i < guildRankRows.Count; i++) {
         GuildRankRow row = guildRankRows[i];
         GuildRankInfo info = i < _initialRanksData.Length ? _initialRanksData[i] : null;

         if (info == null || !row.isActive) {
            row.demoteToggle.isOn = false;
            row.editRanksToggle.isOn = false;
            row.inviteToggle.isOn = false;
            row.kickToggle.isOn = false;
            row.officerChatToggle.isOn = false;
            row.promoteToggle.isOn = false;

            row.rankNameInputField.text = "New Rank";
            row.rankPriority = row.rankId;
            row.isActive = false;
            row.gameObject.SetActive(false);
         } else {
            row.demoteToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildPermission.Demote);
            row.editRanksToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildPermission.EditRanks);
            row.inviteToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildPermission.Invite);
            row.kickToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildPermission.Kick);
            row.officerChatToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildPermission.OfficerChat);
            row.promoteToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildPermission.Promote);

            row.rankPriority = info.rankPriority;
            row.rankNameInputField.text = info.rankName;
         }

         row.deleteRankButton.interactable = row.demoteToggle.interactable || row.editRanksToggle.interactable || row.inviteToggle.interactable || row.kickToggle.interactable || row.officerChatToggle.interactable || row.promoteToggle.interactable;
      }
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);

      // Initial data is always correct
      rankNamesValidation.SetActive(false);

      foreach (GuildRankRow row in guildRankRows) {
         row.deleteRankButton.interactable = row.demoteToggle.interactable || row.editRanksToggle.interactable || row.inviteToggle.interactable || row.kickToggle.interactable || row.officerChatToggle.interactable || row.promoteToggle.interactable;
      }

      addRankButton.interactable = canAddNewRanks();
   }

   private bool canAddNewRanks () {
      if (guildRankRows == null) {
         return false;
      }

      return guildRankRows.FindAll(x => x.isActive).Count < MAXIMUM_NUMBER_OF_RANKS;
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         hide();
      }
   }

   #region Private Variables

   // Maximum number of ranks which can be definied within each guild
   private const int MAXIMUM_NUMBER_OF_RANKS = 4;

   // Data of guild ranks from databased - used to reset to initial state after cancelling changes
   private GuildRankInfo[] _initialRanksData;

   #endregion
}
