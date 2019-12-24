using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;

[Serializable]
public class QuestNode
{
   #region Public Variables

   // The ID of the node
   public int nodeId = -1;

   // The ID of the next quest node
   public int nextNodeId = -1;

   // What the NPC says
   public string npcText;

   // What the player answers
   public string userText;

   // Lists of objectives that must be completed to be able to advance to the next node
   // Set to null when there are no objectives for this node
   [XmlArray("DeliverObjectives"), XmlArrayItem("Deliver")]
   public QuestObjectiveDeliver[] deliverObjectives = null;

   // List of rewards given to the user when advancing from this node to the next
   // Set to null when there are no rewards for this node
   [XmlArray("ItemRewards"), XmlArrayItem("Item")]
   public QuestRewardItem[] itemRewards = null;

   [XmlArray("FriendshipRewards"), XmlArrayItem("Friendship")]
   public QuestRewardFriendship[] friendshipRewards = null;

   // The required gold to provide to the npc
   public int goldRequirement;

   // The rewarded gold upon completion
   public int goldReward;

   #endregion

   public QuestNode () {
   }

   public QuestNode (int id, int nextNodeId, string npcText, string userText,
      QuestObjectiveDeliver[] deliverObjectives, QuestRewardItem[] itemRewards,
      QuestRewardFriendship[] friendshipRewards, int goldRequired, int goldReward) {
      this.nodeId = id;
      this.nextNodeId = nextNodeId;
      this.npcText = npcText;
      this.userText = userText;
      this.deliverObjectives = deliverObjectives;
      this.itemRewards = itemRewards;
      this.friendshipRewards = friendshipRewards;
      this.goldRequirement = goldRequired;
      this.goldReward = goldReward;
   }

   public List<QuestObjective> getAllQuestObjectives () {
      // If the concatenated list doesn't exist yet, create it
      if (_allQuestObjectives == null) {
         _allQuestObjectives = new List<QuestObjective>();

         // Add the deliver objectives
         if (deliverObjectives != null) {
            foreach(QuestObjective o in deliverObjectives) {
               _allQuestObjectives.Add(o);
            }
         }

         if (goldRequirement > 0) {
            QuestObjectiveDeliver questDeliver = new QuestObjectiveDeliver {
               category = Item.Category.Currency,
               count = goldRequirement,
               itemTypeId = 0,
            };
            _allQuestObjectives.Add(questDeliver);
         }
      }
      return _allQuestObjectives;
   }

   public List<QuestReward> getAllQuestRewards () {
      // If the concatenated list doesn't exist yet, create it
      if (_allQuestRewards == null) {
         _allQuestRewards = new List<QuestReward>();

         // Add the item rewards
         if (itemRewards != null) {
            foreach (QuestReward r in itemRewards) {
               _allQuestRewards.Add(r);
            }
         }

         // Add the friendship rewards
         if (friendshipRewards != null) {
            foreach (QuestReward r in friendshipRewards) {
               _allQuestRewards.Add(r);
            }
         }
      }

      return _allQuestRewards;
   }

   #region Private Variables

   // The list of all quest objectives
   private List<QuestObjective> _allQuestObjectives = null;

   // The list of all quest rewards
   private List<QuestReward> _allQuestRewards = null;

   #endregion
}