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

   // Self reference
   public static GuildRanksPanel self;

   // Row gameobjects containing data about each rank within guild
   public List<GuildRankRow> guildRankRows = new List<GuildRankRow>();

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
      guildRankRows.ForEach(row => row.initialize(null));

      foreach (GuildRankInfo info in guildRanks) {
         guildRankRows.Find(row => row.rankId == info.rankId).initialize(info);
      }
      orderRows();
      guildRankRows.ForEach(row => row.updatePriorityButtons());

      this.gameObject.SetActive(false);
   }

   public void fillRowsWithData () {

   }

   public void saveRanks () {
      List<GuildRankInfo> rowsToSend = new List<GuildRankInfo>();

      foreach (GuildRankRow row in guildRankRows) {
         if (!row.isActive) {
            continue;
         }

         int permissions = (row.inviteToggle.isOn ? (int) GuildRankInfo.GuildPermission.Invite : 0) +
                           (row.kickToggle.isOn ? (int) GuildRankInfo.GuildPermission.Kick : 0) +
                           (row.officerChatToggle.isOn ? (int) GuildRankInfo.GuildPermission.OfficerChat : 0) +
                           (row.promoteToggle.isOn ? (int) GuildRankInfo.GuildPermission.Promote : 0) +
                           (row.demoteToggle.isOn ? (int) GuildRankInfo.GuildPermission.Demote : 0) +
                           (row.editRanksToggle.isOn ? (int) GuildRankInfo.GuildPermission.EditRanks : 0);

         GuildRankInfo info = new GuildRankInfo();
         info.rankId = row.rankId;
         info.rankPriority = row.rankPriority;
         info.rankName = row.rankName.text;
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

      if (activeRanks + 1 == MAXIMUM_NUMBER_OF_RANKS) {
         addRankButton.interactable = false;
      }
   }

   public void onCancelButtonPressed () {
      hide();

      // Reset rows data to previous state if "cancel" button was pressed
      resetRows();
   }

   private void orderRows () {
      Dictionary<int, int> siblingIndexToRankId = new Dictionary<int, int>();
      foreach (GuildRankRow rankRow in guildRankRows) {
         if (rankRow.isActive) {
            siblingIndexToRankId.Add(rankRow.rankId, rankRow.transform.GetSiblingIndex());
         }
      }

      // Bubble sort rows order
      for (int i = 1; i < siblingIndexToRankId.Count + 1; i++) {
         for (int j = 1; j < siblingIndexToRankId.Count + 1 - i; j++) {
            if (guildRankRows.Find(x=>x.rankId == j).rankPriority > guildRankRows.Find(x => x.rankId == j + 1).rankPriority) {
               int tmp = siblingIndexToRankId[j];
               siblingIndexToRankId[j] = siblingIndexToRankId[j + 1];
               siblingIndexToRankId[j + 1] = tmp;
            }
         }
      }

      // Apply rows order
      foreach (KeyValuePair<int, int> pair in siblingIndexToRankId) {
         guildRankRows[pair.Key - 1].transform.SetSiblingIndex(pair.Value);
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
            row.demoteToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildRankInfo.GuildPermission.Demote);
            row.editRanksToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildRankInfo.GuildPermission.EditRanks);
            row.inviteToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildRankInfo.GuildPermission.Invite);
            row.kickToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildRankInfo.GuildPermission.Kick);
            row.officerChatToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildRankInfo.GuildPermission.OfficerChat);
            row.promoteToggle.isOn = GuildRankInfo.canPerformAction(info.permissions, GuildRankInfo.GuildPermission.Promote);

            row.rankPriority = info.rankPriority;
         }
      }
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
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
