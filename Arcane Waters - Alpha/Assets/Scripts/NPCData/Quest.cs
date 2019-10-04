using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;

public class Quest
{
   #region Public Variables

   // The index of the quest
   public int questId;

   // The title of the quest
   public string title;

   // The friendship rank required to start this quest
   [XmlIgnore]
   public NPCFriendship.Rank friendshipRankRequired;

   [XmlElement("friendshipRankRequired")]
   public int FriendshipRankRequiredInt
   {
      get { return (int) friendshipRankRequired; }
      set { friendshipRankRequired = (NPCFriendship.Rank) value; }
   }

   // Gets set to true when the quest always reappears after completing it
   public bool isPermanent = false;

   // The list of quest nodes
   [XmlArray("Nodes"), XmlArrayItem("Node")]
   public QuestNode[] nodes;   

   #endregion

   public Quest () {

   }

   public Quest (int id, string title, NPCFriendship.Rank friendshipRankRequired, bool isPermanent, QuestNode[] nodes) {
      this.questId = id;
      this.title = title;
      this.friendshipRankRequired = friendshipRankRequired;
      this.isPermanent = isPermanent;
      this.nodes = nodes;
   }

   public QuestNode getFirstNode () {
      return nodes[0];
   }

   #region Private Variables

   #endregion
}