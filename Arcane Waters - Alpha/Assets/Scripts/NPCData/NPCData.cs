using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System;

[XmlRoot("NPCData")]
public class NPCData
{
   #region Public Variables

   // The NPC ID
   public int npcId;

   // The greeting text of the NPC, shown every time the conversation starts
   public string greetingText;

   // The list of quests
   [XmlArray("Quests"), XmlArrayItem("Quest")]
   public List<Quest> quests;

   // The quests retrievable with their id
   [XmlIgnore]
   public Dictionary<int, Quest> questsDictionary;

   #endregion

   public NPCData () {

   }

   public NPCData (int npcId, string greetingText, List<Quest> quests) {
      this.npcId = npcId;
      this.greetingText = greetingText;
      this.quests = quests;
   }

   #region Private Variables

   #endregion
}
