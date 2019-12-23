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
   
   // List of drops of ores
   public List<OreLootLibrary> oreLootList;

   #endregion

   private void Awake () {
      self = this;
   }

   public SeaMonsterLootLibrary fetchSeaMonsterLootData (SeaMonsterEntity.Type seaMonsterType) {
      SeaMonsterLootLibrary newLootLibrary = _seaMonsterLootList.Find(_ => _.enemyType == seaMonsterType);
      if (newLootLibrary == null) {
         Debug.LogWarning("Returning Null Sea Monster Loot Library");
      }

      return newLootLibrary;
   }

   public EnemyLootLibrary fetchLandMonsterLootData (Enemy.Type landMonsterType) {
      EnemyLootLibrary newLootLibrary = _landMonsterLootList.Find(_ => _.enemyType == landMonsterType);
      if (newLootLibrary == null) {
         Debug.LogWarning("Returning Null Land Monster Loot Library");
      }

      return newLootLibrary;
   }

   public void initSeaMonsterLootList () {
      _seaMonsterLootList = new List<SeaMonsterLootLibrary>();
      foreach (SeaMonsterEntityData seaMonsterData in SeaMonsterManager.self.seaMonsterDataList) {
         SeaMonsterLootLibrary newLibrary = new SeaMonsterLootLibrary();
         newLibrary.enemyType = seaMonsterData.seaMonsterType;
         newLibrary.dropTypes = seaMonsterData.lootData;

         _seaMonsterLootList.Add(newLibrary);
      }
   }

   public void initLandMonsterLootList () {
      _landMonsterLootList = new List<EnemyLootLibrary>();
      foreach (BattlerData monsterData in MonsterManager.self.monsterDataList) {
         EnemyLootLibrary newLibrary = new EnemyLootLibrary();
         newLibrary.enemyType = monsterData.enemyType;
         newLibrary.dropTypes = monsterData.battlerLootData;

         _landMonsterLootList.Add(newLibrary);
      }
   }

   public void receiveListFromServer (CraftableItemRequirements[] requirements) {
      craftableDataList = new List<CraftableItemRequirements>();
      foreach(CraftableItemRequirements requirement in requirements) {
         craftableDataList.Add(requirement);
      }
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

   #region Private Variables

   // List of drops of sea monster enemies
   [SerializeField]
   protected List<SeaMonsterLootLibrary> _seaMonsterLootList;

   // List of drops of land monster enemies
   [SerializeField]
   protected List<EnemyLootLibrary> _landMonsterLootList;

   #endregion
}

[Serializable]
public class EnemyLootLibrary
{
   // The type of land monster
   public Enemy.Type enemyType;

   // The loot the land monster drops
   public RawGenericLootData dropTypes;
}

[Serializable]
public class SeaMonsterLootLibrary
{
   // The type of seamonster
   public SeaMonsterEntity.Type enemyType;

   // The loot the seamonster drops
   public RawGenericLootData dropTypes;
}

[Serializable]
public class OreLootLibrary
{
   // The type of ore item
   public OreNode.Type oreType;

   // The loots earned for mining the ore
   public GenericLootData dropTypes;
}