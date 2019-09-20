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

   // The friendship level required to start this quest
   public int friendshipLevelRequired;

   // The list of quest nodes
   [XmlArray("Nodes"), XmlArrayItem("Node")]
   public List<QuestNode> nodes;   

   // The quest nodes retrievable with their id
   [XmlIgnore]
   public Dictionary<int, QuestNode> nodesDictionary;

   #endregion

   public Quest () {

   }

   public Quest (int id, string title, int friendshipLevelRequired, List<QuestNode> nodes) {
      this.questId = id;
      this.title = title;
      this.friendshipLevelRequired = friendshipLevelRequired;
      this.nodes = nodes;
   }

   public QuestNode getFirstNode () {
      return nodes[0];
   }

   #region Private Variables

   #endregion
}