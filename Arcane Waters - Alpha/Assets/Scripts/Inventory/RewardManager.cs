using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class RewardManager : MonoBehaviour {
   #region Public Variables

   // Self reference
   public static RewardManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void processLoots (List<LootInfo> loots) {
      // Item list extraction
      List<Item> itemList = new List<Item>();
      for (int i = 0; i < loots.Count; i++) {
         CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) loots[i].lootType, ColorType.DarkGreen, ColorType.DarkPurple, "");
         craftingIngredients.itemTypeId = (int) craftingIngredients.type;
         Item item = craftingIngredients;
         itemList.Add(item);
      }

      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(itemList);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void requestIngredient (CraftingIngredients.Type ingredient) {
      // Ingredient to item conversion
      CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) ingredient, ColorType.DarkGreen, ColorType.DarkPurple, "");
      craftingIngredients.itemTypeId = (int) craftingIngredients.type;
      Item item = craftingIngredients;
      showItemInRewardPanel(item);
   }

   public void showItemInRewardPanel (Item item) {
      // Calls the panel and a single item
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemData(item);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }
}