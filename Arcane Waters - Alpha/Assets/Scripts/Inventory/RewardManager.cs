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

   public IEnumerator CO_CreatingFloatingIcon (Item item, Vector3 transformVector) {
      yield return new WaitForSeconds(.2f);

      // Create a floating icon
      GameObject floatingItem = Instantiate(TreasureManager.self.floatingItemPrefab, transformVector, Quaternion.identity);
      GameObject floatingIcon = floatingItem.transform.GetChild(0).gameObject;
      //floatingIcon.transform.SetParent(this.transform);
      floatingIcon.transform.localPosition = new Vector3(0f, .04f);
      Image image = floatingIcon.GetComponentInChildren<Image>();
      string itemName = "";
      if (item.category == Item.Category.Quest_Item) {
         QuestItem questItem = EquipmentXMLManager.self.getQuestItemById(item.itemTypeId);
         if (questItem != null) {
            image.sprite = ImageManager.getSprite(questItem.iconPath);
            itemName = questItem.itemName;
         }
      }

      // Create a new instance of the material we can use for recoloring
      Material sourceMaterial = MaterialManager.self.getGUIMaterial();
      if (sourceMaterial != null) {
         image.material = new Material(sourceMaterial);

         // Recolor
         floatingIcon.GetComponentInChildren<RecoloredSprite>().recolor(item.paletteNames);
      }

      // Set the name text
      FloatAndStop floatingComponent = floatingIcon.GetComponentInChildren<FloatAndStop>();
      if (floatingComponent.quantityText != null) {
         floatingComponent.nameText.text = itemName;
         if (item.count > 1) {
            floatingComponent.quantityText.text = item.count.ToString();
            floatingComponent.quantityText.gameObject.SetActive(true);
         }
      }

      // Show a confirmation in chat
      if (itemName.Length < 1) {
         D.debug("Invalid Item Name: The item found is: " + item.category + " : " + item.itemTypeId + " : " + item.data);
      } else {
         string msg = string.Format("You found <color=yellow>{0}</color> <color=red>{1}</color>", item.count, itemName);
         ChatManager.self.addChat(msg, ChatInfo.Type.System);
      }
   }

   public void showItemsInRewardPanel (List<Item> loots) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(loots);
      PanelManager.self.linkPanel(Panel.Type.Reward);
   }

   public void showItemsInRewardPanel (IEnumerable<ItemInstance> items) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = PanelManager.self.get<RewardScreen>(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(items);
      PanelManager.self.linkPanel(Panel.Type.Reward);
   }

   public void showRecruitmentNotice (string recruitedName, string recruitedIconPath) {
      // Calls the panel and injects the info of the recruited npc
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setRecruitmentData(recruitedName, recruitedIconPath);
      PanelManager.self.linkPanel(Panel.Type.Reward);
   }

   public void showAbilityRewardNotice (string abilityName, string abilityIconPath) {
      // Calls the panel and injects the info of the rewarded ability
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setAbilityReward(abilityName, abilityIconPath);
      PanelManager.self.linkPanel(Panel.Type.Reward);
   }

   public void showItemInRewardPanel (Item loot) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemData(loot);
      PanelManager.self.linkPanel(Panel.Type.Reward);
   }

   #region Private Variables

   #endregion
}