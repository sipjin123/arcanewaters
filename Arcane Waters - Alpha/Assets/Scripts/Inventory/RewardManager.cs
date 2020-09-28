using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class RewardManager : MonoBehaviour {
   #region Public Variables

   // Self reference
   public static RewardManager self;
   
   #endregion

   private void Awake () {
      self = this;
   }

   public void showItemsInRewardPanel (List<Item> loots) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(loots);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void showItemsInRewardPanel (IEnumerable<ItemInstance> items) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = PanelManager.self.get<RewardScreen>(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(items);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void showRecruitmentNotice (string recruitedName, string recruitedIconPath) {
      // Calls the panel and injects the info of the recruited npc
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setRecruitmentData(recruitedName, recruitedIconPath);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void showAbilityRewardNotice (string abilityName, string abilityIconPath) {
      // Calls the panel and injects the info of the rewarded ability
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setAbilityReward(abilityName, abilityIconPath);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void showItemInRewardPanel (Item loot) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemData(loot);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   #region Private Variables

   #endregion
}