using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
   #region Public Variables

   // Self Reference
   public static QuestManager self;
   
   // List of items to provide on delivery quests
   public List<Item> deliverableItemList;

   // List of items to reward after delivery quests
   public List<Item> rewardItemList;

   // The npc data for all deliver quests
   public NPCQuestData deliveryQuestData;

   #endregion

   private void Awake () {
      self = this;
   }
}