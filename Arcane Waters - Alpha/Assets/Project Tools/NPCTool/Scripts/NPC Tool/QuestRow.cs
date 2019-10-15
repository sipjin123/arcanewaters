using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuestRow : MonoBehaviour
{
   #region Public Variables

   // The components displaying the main quest parameters
   public Text questIdText;
   public InputField title;
   public InputField friendshipRankRequired;
   public Toggle isRepeatable;

   // The container for the quest nodes
   public GameObject rowsContainer;

   // The prefab we use for creating quest node rows
   public QuestNodeRow questNodePrefab;

   // The reference to the current quest node being modified
   public QuestNodeRow currentQuestNode;

   // Reference to the npc edition screen
   public NPCEditScreen npcEditionScreen;

   // Holds the contents of the quest node
   public GameObject[] contentView;

   // If is showing content
   public bool showContents = true;

   #endregion

   public void setRowForQuest (Quest quest) {
      _questId = quest.questId;
      _lastUsedNodeId = quest.lastUsedNodeId;
      questIdText.text = quest.questId.ToString();
      title.text = quest.title;
      friendshipRankRequired.text = ((int) quest.friendshipRankRequired).ToString();
      isRepeatable.isOn = quest.isRepeatable;

      if (quest.nodes != null && quest.nodes.Length > 0) {
         // Create a dictionary with all the quest nodes
         Dictionary<int, QuestNode> nodeDictionary = new Dictionary<int, QuestNode>();

         // Add the nodes to the dictionary
         foreach (QuestNode node in quest.nodes) {
            nodeDictionary.Add(node.nodeId, node);
         }

         // Create a new list for the nodes
         List<QuestNode> nodeList = new List<QuestNode>();

         // Add the first node to the list
         nodeList.Add(quest.getFirstNode());
         int nextNodeId = quest.getFirstNode().nextNodeId;

         // Add the other nodes to the list, in the order defined by their nextNodeId
         while (nextNodeId != -1) {
            // Verify that the node exists
            if (!nodeDictionary.ContainsKey(nextNodeId)) {
               Debug.LogError(string.Format("The next node {0} of node {1} does not exists", nextNodeId.ToString(), nodeList[nodeList.Count - 1].nodeId));
               break;
            }

            // Add the node
            nodeList.Add(nodeDictionary[nextNodeId]);

            // Remove the node from the dictionary, to avoid inconsistencies
            nodeDictionary.Remove(nextNodeId);

            // Set the next node index
            nextNodeId = nodeList[nodeList.Count - 1].nextNodeId;
         }

         // Clear all the rows
         rowsContainer.DestroyChildren();

         // Create a row for each quest node
         foreach (QuestNode node in nodeList) {
            // Create a new row
            QuestNodeRow row = Instantiate(questNodePrefab, rowsContainer.transform, false);
            row.questRow = this;
            row.transform.SetParent(rowsContainer.transform, false);
            row.npcEditScreen = npcEditionScreen;
            row.setRowForQuestNode(node);
         }
      } else {
         rowsContainer.DestroyChildren();
      }
   }

   public void toggleContents() {
      showContents = !showContents;
      foreach (GameObject obj in contentView) {
         obj.SetActive(showContents);
      }
   }

   public void addNodeButtonClickedOn () {
      // Increment the last used node id
      _lastUsedNodeId++;

      // Create a new empty node
      QuestNode node = new QuestNode(_lastUsedNodeId, -1, "", "", null, null, null);

      // Create a new node row
      QuestNodeRow row = Instantiate(questNodePrefab, rowsContainer.transform, false);
      row.questRow = this;
      row.transform.SetParent(rowsContainer.transform, false);
      row.npcEditScreen = npcEditionScreen;
      row.setRowForQuestNode(node);
   }

   public void deleteButtonClickedOn () {
      Destroy(gameObject);
   }

   public Quest getModifiedQuest () {
      // Retrieve every quest node
      List<QuestNode> questNodeList = new List<QuestNode>();
      foreach (QuestNodeRow nodeRow in rowsContainer.GetComponentsInChildren<QuestNodeRow>()) {
         questNodeList.Add(nodeRow.getModifiedQuestNode());
      }

      // Set the order of the nodes
      if (questNodeList.Count > 0) {
         for (int i = 0; i < questNodeList.Count - 1; i++) {
            questNodeList[i].nextNodeId = questNodeList[i + 1].nodeId;
         }
         questNodeList[questNodeList.Count - 1].nextNodeId = -1;
      }

      // Create a new quest object
      Quest quest = new Quest(_questId, title.text, (NPCFriendship.Rank) int.Parse(friendshipRankRequired.text),
         isRepeatable.isOn, _lastUsedNodeId, questNodeList.ToArray());

      return quest;
   }

   #region Private Variables

   // The id of the quest
   private int _questId;

   // The last used node id
   private int _lastUsedNodeId;

   #endregion
}
