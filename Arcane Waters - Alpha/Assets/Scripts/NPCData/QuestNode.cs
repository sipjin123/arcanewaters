using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;

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

   // A list of objectives that must be completed to be able to advance to the next node
   // Set to null when there are no objectives for this node
   [XmlArray("QuestObjectives")]
   [XmlArrayItem("Deliver", typeof(QuestObjectiveDeliver))]
   public List<QuestObjective> objectives = null;

   // A list of rewards given to the user when advancing from this node to the next
   // Set to null when there are no rewards for this node
   [XmlArray("Rewards")]
   [XmlArrayItem("Item", typeof(QuestRewardItem))]
   [XmlArrayItem("Friendship", typeof(QuestRewardFriendship))]
   public List<QuestReward> rewards = null;

   #endregion

   public QuestNode () {
      objectives = new List<QuestObjective>();
      rewards = new List<QuestReward>();
   }

   public QuestNode (int id, int nextNodeId, string npcText, string userText,
      List<QuestObjective> objectives, List<QuestReward> rewards) {
      this.nodeId = id;
      this.nextNodeId = nextNodeId;
      this.npcText = npcText;
      this.userText = userText;
      this.objectives = objectives;
      this.rewards = rewards;
   }

   #region Private Variables

   #endregion
}