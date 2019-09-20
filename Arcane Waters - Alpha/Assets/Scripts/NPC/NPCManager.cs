using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class NPCManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static NPCManager self;

   // The data files containing the NPC dialogues and quests
   public TextAsset[] npcDataAssets;

   #endregion

   public void Awake () {
      self = this;

      // Initializes the quest cache
      _npcData = new Dictionary<int, NPCData>();

      // Iterate over the files
      foreach(TextAsset textAsset in npcDataAssets) {
         // Deserialize the file
         //NPCData npcData = JsonUtility.FromJson<NPCData>(textAsset.text);

         // Read and deserialize the file
         NPCData npcData = Util.xmlLoad<NPCData>(textAsset);

         // Save the NPC data in the memory cache
         _npcData.Add(npcData.npcId, npcData);
      }

      // Create dictionaries of quests and nodes, for easy access later
      foreach (NPCData data in _npcData.Values) {
         // Create the quest dictionary for this npc
         data.questsDictionary = new Dictionary<int, Quest>();

         // Iterate over each quest
         foreach (Quest quest in data.quests) {
            // Add the quest to the dictionary
            data.questsDictionary.Add(quest.questId, quest);

            // Create the node dictionary for this quest
            quest.nodesDictionary = new Dictionary<int, QuestNode>();

            // Iterate over each node
            foreach(QuestNode node in quest.nodes) {
               // Add the node to the dictionary
               quest.nodesDictionary.Add(node.nodeId, node);
            }
         }
      }
   }

   public void storeNPC (NPC npc) {
      _npcs[npc.npcId] = npc;
   }

   public NPC getNPC (int npcId) {
      return _npcs[npcId];
   }

   public List<Quest> getQuests (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].quests;
      } else {
         return new List<Quest>();
      }
   }

   public Quest getQuest (int npcId, int questId) {
      NPCData npcData;
      if (!_npcData.TryGetValue(npcId, out npcData)) {
         D.error("The npc has no quest data: " + npcId);
         return null;
      }

      Quest quest;
      if (!npcData.questsDictionary.TryGetValue(questId, out quest)) {
         D.error("The quest does not exist: " + npcId + "/" + questId);
         return null;
      }

      return quest;
   }

   public QuestNode getQuestNode (int npcId, int questId, int questNodeId) {
      Quest quest = getQuest(npcId, questId);

      if (quest == null) {
         return null;
      }

      QuestNode node;
      if (!quest.nodesDictionary.TryGetValue(questNodeId, out node)) {
         D.error("The quest node does not exist: " + npcId + "/" + questId + "/" + questNodeId);
         return null;
      }

      return node;
   }

   public string getGreetingText (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].greetingText;
      } else {
         return "Hello! How can I help you?";
      }
   }

   #region Private Variables

   // Keeps track of the NPCs, based on their id
   protected Dictionary<int, NPC> _npcs = new Dictionary<int, NPC>();

   // The cached NPC data for interactive NPCs
   private Dictionary<int, NPCData> _npcData;

   #endregion
}
