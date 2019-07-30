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
      Debug.LogError((object) ("-------------------- I received this list : " + loots.Count));
      List<Item> itemList = new List<Item>();
      for (int i = 0; i < loots.Count; i++) {
         Debug.LogError((object) loots[i].lootType);

         CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) loots[i].lootType, ColorType.DarkGreen, ColorType.DarkPurple, "");
         craftingIngredients.itemTypeId = (int) craftingIngredients.type;
         Item item = craftingIngredients;
         itemList.Add(item);
      }
      //----------------------------------------

      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(itemList);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void requestIngredient(CraftingIngredients.Type ingredient) {
      CraftingIngredients craftingIngredients = new CraftingIngredients(0, (int) ingredient, ColorType.DarkGreen, ColorType.DarkPurple, "");
      craftingIngredients.itemTypeId = (int) craftingIngredients.type;
      Item item = craftingIngredients;
      requestItem(item);
   }

   public void requestItem(Item item) {
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemData(item);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }
}
