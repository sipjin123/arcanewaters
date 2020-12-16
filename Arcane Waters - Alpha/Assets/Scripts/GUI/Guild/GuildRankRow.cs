using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GuildRankRow : MonoBehaviour {
   #region Public Variables

   // Determines whether this rank is being used - changes to true after pressing "add rank" button
   public bool isActive;

   // Increases/decreases priority of chosen rank
   public Button priorityUp;
   public Button priorityDown;

   // Presents rank name to user
   public Text rankName;

   // Allows user to change rank name
   public InputField rankNameInputField;

   // Determines rank row in database
   public int rankId;

   // Priority - the lower the better, counting from 1 (0 is guild owner)
   public int rankPriority;

   // Toggles determining rank permissions
   public Toggle inviteToggle;
   public Toggle kickToggle;
   public Toggle officerChatToggle;
   public Toggle promoteToggle;
   public Toggle demoteToggle;
   public Toggle editRanksToggle;

   #endregion

   public void initialize (GuildRankInfo info) {
      isActive = (info != null);
      gameObject.SetActive(isActive);
      if (!isActive) {
         return;
      }

      rankPriority = info.rankPriority;
      if (rankNameInputField != null) {
         rankNameInputField.text = info.rankName.ToLower();
      }

      inviteToggle.isOn = (info.permissions & (int) GuildRankInfo.GuildPermission.Invite) != 0;
      kickToggle.isOn = (info.permissions & (int) GuildRankInfo.GuildPermission.Kick) != 0;
      officerChatToggle.isOn = (info.permissions & (int) GuildRankInfo.GuildPermission.OfficerChat) != 0;
      promoteToggle.isOn = (info.permissions & (int) GuildRankInfo.GuildPermission.Promote) != 0;
      demoteToggle.isOn = (info.permissions & (int) GuildRankInfo.GuildPermission.Demote) != 0;
      editRanksToggle.isOn = (info.permissions & (int) GuildRankInfo.GuildPermission.EditRanks) != 0;

      // Guild member has to have higher rank priority than ranks thay he's trying to modify
      if (Global.player != null) {
         bool interactable = Global.player.guildRankPriority < info.rankPriority;
         inviteToggle.interactable = kickToggle.interactable = officerChatToggle.interactable =
            promoteToggle.interactable = demoteToggle.interactable = editRanksToggle.interactable = interactable;

         inviteToggle.graphic.color = kickToggle.graphic.color = officerChatToggle.graphic.color =
            promoteToggle.graphic.color = demoteToggle.graphic.color = editRanksToggle.graphic.color = interactable ? _standardColor : _disabledColor;
      }
   }

   public void moveRankUp () {
      if (priorityUp.IsInteractable()) {
         GuildRankRow higherRow = GuildRanksPanel.self.guildRankRows.Find(x => x.rankPriority == rankPriority - 1);
         
         // Decrease priority of rank which we swapped with
         higherRow.rankPriority += 1;

         // Increase priority of this rank
         rankPriority -= 1;

         int lowerRowIndex = this.transform.GetSiblingIndex();
         int higherRowIndex = higherRow.transform.GetSiblingIndex();
         transform.SetSiblingIndex(higherRowIndex);
         higherRow.transform.SetSiblingIndex(lowerRowIndex);

         // Update buttons
         updatePriorityButtons();
         higherRow.updatePriorityButtons();
      }
   }

   public void moveRankDown () {
      if (priorityDown.IsInteractable()) {
         GuildRankRow lowerRow = GuildRanksPanel.self.guildRankRows.Find(x => x.rankPriority == rankPriority + 1);

         // Increase priority of rank which we swapped with
         lowerRow.rankPriority -= 1;

         // Decrease priority of this rank
         rankPriority += 1;

         int higherRowIndex = this.transform.GetSiblingIndex();
         int lowerRowIndex = lowerRow.transform.GetSiblingIndex();
         transform.SetSiblingIndex(lowerRowIndex);
         lowerRow.transform.SetSiblingIndex(higherRowIndex);
         
         // Update buttons
         updatePriorityButtons();
         lowerRow.updatePriorityButtons();
      }
   }

   public void updatePriorityButtons () {
      int lowestPriority = int.MinValue;
      foreach (GuildRankRow row in GuildRanksPanel.self.guildRankRows) {
         if (row.isActive && row.rankPriority > lowestPriority) {
            lowestPriority = row.rankPriority;
         }
      }

      priorityUp.interactable = rankPriority > 1;
      priorityDown.interactable = (rankPriority != lowestPriority);

      if (Global.player.guildRankPriority > 0) {
         priorityDown.interactable &= (Global.player.guildRankPriority < rankPriority);
         priorityUp.interactable &= (Global.player.guildRankPriority + 1 < rankPriority);
      }
   }

   public void onModifiedRankName (string newValue) {
      HashSet<string> rankNames = new HashSet<string>();
      int activeCount = 0;

      foreach (GuildRankRow row in GuildRanksPanel.self.guildRankRows) {
         if (row.isActive) {
            rankNames.Add(row.rankNameInputField.text.ToLower());
            activeCount++;
            
            if (row.rankNameInputField.text.Trim() == "") {
               GuildRanksPanel.self.rankNamesValidation.SetActive(true);
               GuildRanksPanel.self.saveRanksButton.enabled = false;
               GuildRanksPanel.self.rankNamesValidation.GetComponentInChildren<Text>().text = "Rank names cannot be empty!";
               return;
            }
         }
      }

      GuildRanksPanel.self.rankNamesValidation.SetActive(rankNames.Count != activeCount);
      GuildRanksPanel.self.saveRanksButton.enabled = (rankNames.Count == activeCount);

      if (rankNames.Count != activeCount) {
         GuildRanksPanel.self.rankNamesValidation.GetComponentInChildren<Text>().text = "Rank names should be distinct!";
      }
   }

   #region Private Variables

   // Standard toggle color
   private Color _standardColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

   // Color of toggle which is not interactable
   private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

   #endregion
}
