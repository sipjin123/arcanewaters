using System;
using UnityEngine;
using UnityEngine.UI;

public class GuildRankTab : MonoBehaviour {
   #region Public Variables
   
   // Guild rank info
   public GuildRankInfo rankInfo;
   
   // Guild name text
   public Text nameText;
   
   // GO to set tab as active
   public GameObject activeTab;
   
   // Delete button  GO
   public GameObject deleteButton;
   
   #endregion

   public void init (GuildRankInfo rankInfo) {
      this.rankInfo = rankInfo;
      
      bool canDelete = false;
      if (Global.player != null) {
         canDelete = Global.player.guildRankPriority < rankInfo.rankPriority;
      }
      deleteButton.SetActive(canDelete);
      
      setName();
      deselectTab();
   }

   public void setName () {
      nameText.text = rankInfo.rankName;
   }

   public void selectTab () {
      GuildRanksPanel.self.selectRank(rankInfo.id);
      activeTab.SetActive(true);
   }

   public void deselectTab () {
      activeTab.SetActive(false);
   }

   public void deleteRank () {
      if (Global.player) {
         PanelManager.self.confirmScreen.show("Do you want to delete rank: " + nameText.text + "?");
         PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
         PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
            PanelManager.self.confirmScreen.hide();
            Global.player.rpc.Cmd_DeleteGuildRank(rankInfo.rankId);
            GuildRanksPanel.self.onRankDeleted(rankInfo.id);
         });
      }
   }

   public void disableDeleteButton () {
      deleteButton.SetActive(false);
   }

   #region Private Variables
   #endregion
}
