using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class RewardManager : MonoBehaviour {
   #region Public Variables

   // List of items that can be crafted
   public CombinationDataList combinationDataList;

   // Self reference
   public static RewardManager self;

   // List of drops from ore mining
   public List<OreLootLibrary> oreLootList;

   // List of drops of enemies
   public List<EnemyLootLibrary> enemyLootList;

   #endregion

   private void Awake () {
      self = this;
   }

   public void processLoots (List<Item> loots) {
      // Calls the panel and injects the List of items
      RewardScreen rewardPanel = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);
      rewardPanel.setItemDataGroup(loots);
      PanelManager.self.pushPanel(Panel.Type.Reward);
   }

   public void processLoot (Item loot) {
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
   public OreType oreType;

   public GenericLootData dropTypes;
}