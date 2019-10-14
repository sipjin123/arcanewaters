using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class RewardManager : MonoBehaviour {
   #region Public Variables

   // List of items that can be crafted
   public List<CraftableItemRequirements> craftableDataList;

   // Self reference
   public static RewardManager self;

   // List of drops of enemies
   public List<EnemyLootLibrary> enemyLootList;

   // List of drops of ores
   public List<OreLootLibrary> oreLootList;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      craftableDataList = CraftingManager.self.getAllCraftableData();
   }

   public void showItemsInRewardPanel (List<Item> loots) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(loots);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void showItemInRewardPanel (Item loot) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemData(loot);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }
}

[Serializable]
public class EnemyLootLibrary
{
   public Enemy.Type enemyType;

   public GenericLootData dropTypes;
}

[Serializable]
public class OreLootLibrary
{
   public OreNode.Type oreType;

   public GenericLootData dropTypes;
}