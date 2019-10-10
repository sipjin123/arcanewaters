using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuestNodeRow : MonoBehaviour
{
   #region Public Variables

   // The components displaying the main node parameters
   public Text questNodeIdText;
   public InputField npcText;
   public InputField userText;
   public InputField friendshipReward;

   // The container for the deliver objectives
   public GameObject deliverObjectivesRowsContainer;

   // The container for the item rewards
   public GameObject itemRewardRowsContainer;

   // The prefab we use for creating deliver objectives
   public DeliverObjectiveRow deliverObjectivePrefab;

   // The prefab we use for creating item rewards
   public ItemRewardRow itemRewardPrefab;

   // The button for triggering creation of quest delivery
   public Button createDeliveryQuestButton;

   // The button for triggering creation of reward
   public Button createRewardButton;

   // The cached Quest Delivery Objective
   public QuestObjectiveDeliver cachedDeliverObjective = new QuestObjectiveDeliver();
   
   // The cached Reward
   public QuestRewardItem cachedReward = new QuestRewardItem();

   // The current quest node being edited
   public QuestNode cachedQuestNode;

   // The list of delivery quests being edited
   public List<QuestObjectiveDeliver> cachedDeliverList;

   // The list of reward being edited
   public List<QuestRewardItem> cachedRewardList;

   #endregion

   public void setRowForQuestNode (QuestNode node) {
      _questNodeId = node.nodeId;
      questNodeIdText.text = node.nodeId.ToString();
      npcText.text = node.npcText;
      userText.text = node.userText;
      cachedQuestNode = node;

      // Calculate the total friendship reward and display only the sum
      int totalFriendshipReward = 0;
      if (node.friendshipRewards != null) {
         foreach (QuestRewardFriendship friendshipReward in node.friendshipRewards) {
            totalFriendshipReward += friendshipReward.rewardedFriendship;
         }
      }
      friendshipReward.text = totalFriendshipReward.ToString();

      // Clear all the rows
      deliverObjectivesRowsContainer.DestroyChildren();

      cachedDeliverList = new List<QuestObjectiveDeliver>();
      cachedRewardList = new List<QuestRewardItem>();

      // Create a row for each deliver objective
      if (node.deliverObjectives != null) {
         foreach (QuestObjectiveDeliver deliverObjective in node.deliverObjectives) {
            // Create a new row
            DeliverObjectiveRow row = Instantiate(deliverObjectivePrefab, deliverObjectivesRowsContainer.transform, false);
            row.transform.SetParent(deliverObjectivesRowsContainer.transform, false);
            row.setRowForDeliverObjective(deliverObjective);

            cachedDeliverList.Add(deliverObjective);
         }
      }

      cachedQuestNode.deliverObjectives = cachedDeliverList.ToArray();

      // Clear all the rows
      itemRewardRowsContainer.DestroyChildren();

      // Create a row for each deliver objective
      if (node.itemRewards != null) {
         foreach (QuestRewardItem itemReward in node.itemRewards) {
            // Create a new row
            ItemRewardRow row = Instantiate(itemRewardPrefab, itemRewardRowsContainer.transform, false);
            row.transform.SetParent(itemRewardRowsContainer.transform, false);
            row.setRowForItemReward(itemReward);

            cachedRewardList.Add(itemReward);
         }
      }
      cachedQuestNode.itemRewards = cachedRewardList.ToArray();

      createRewardButton.onClick.AddListener(() => createRewardButtonClickedOn());
      createDeliveryQuestButton.onClick.AddListener(() => createDeliveryQuestButtonClickedOn());
   }

   private void createRewardButtonClickedOn() {
      cachedReward = new QuestRewardItem();
      ItemRewardRow row = Instantiate(itemRewardPrefab, itemRewardRowsContainer.transform, false);
      row.transform.SetParent(itemRewardRowsContainer.transform, false);

      row.setRowForItemReward(cachedReward);

      row.updateButton.onClick.AddListener(() => updateRewardButtonClicked(row));
   }

   private void updateRewardButtonClicked (ItemRewardRow row) {
      cachedReward.count = int.Parse(row.count.text);
      cachedReward.category = (Item.Category) int.Parse(row.itemCategory.text);
      cachedReward.itemTypeId = int.Parse(row.itemTypeId.text);

      row.setRowForItemReward(cachedReward);
      cachedQuestNode.itemRewards = cachedRewardList.ToArray();
   }

   private void createDeliveryQuestButtonClickedOn () {
      cachedDeliverObjective = new QuestObjectiveDeliver();
      DeliverObjectiveRow row = Instantiate(deliverObjectivePrefab, deliverObjectivesRowsContainer.transform, false);
      row.transform.SetParent(deliverObjectivesRowsContainer.transform, false);

      row.setRowForDeliverObjective(cachedDeliverObjective);

      row.updateButton.onClick.AddListener(() => updateDataButtonClicked(row));
   }

   private void updateDataButtonClicked(DeliverObjectiveRow row) {
      cachedDeliverObjective.category = (Item.Category) int.Parse(row.itemCategory.text);
      cachedDeliverObjective.count = int.Parse(row.count.text);
      cachedDeliverObjective.itemTypeId = int.Parse(row.itemTypeId.text);
      cachedDeliverList.Add(cachedDeliverObjective);

      row.setRowForDeliverObjective(cachedDeliverObjective);

      cachedQuestNode.deliverObjectives = cachedDeliverList.ToArray();
   }

   public void moveNodeUpButtonClickedOn () {
      int siblingIndex = transform.GetSiblingIndex();
      if (siblingIndex > 0) {
         transform.SetSiblingIndex(siblingIndex - 1);
      }
   }

   public void moveNodeDownButtonClickedOn () {
      int siblingIndex = transform.GetSiblingIndex();
      if (siblingIndex < transform.parent.childCount - 1) {
         transform.SetSiblingIndex(siblingIndex + 1);
      }
   }

   public void deleteNodeButtonClickedOn () {
      Destroy(gameObject);
   }

   public QuestNode getModifiedQuestNode () {
      // Retrieve every quest objective
      List<QuestObjectiveDeliver> deliverObjectiveList = new List<QuestObjectiveDeliver>();
      foreach(DeliverObjectiveRow deliverObjectiveRow in deliverObjectivesRowsContainer.GetComponentsInChildren<DeliverObjectiveRow>()) {
         deliverObjectiveList.Add(deliverObjectiveRow.getModifiedDeliverObjective());
      }

      // Retrieve every item reward
      List<QuestRewardItem> itemRewardList = new List<QuestRewardItem>();
      foreach (ItemRewardRow itemRewardRow in itemRewardRowsContainer.GetComponentsInChildren<ItemRewardRow>()) {
         itemRewardList.Add(itemRewardRow.getModifiedItemReward());
      }

      // Create an array with the friendship reward value, if any
      QuestRewardFriendship[] friendshipRewardArray = null;
      int friendshipRewardValue = int.Parse(friendshipReward.text);
      if (friendshipRewardValue > 0) {
         friendshipRewardArray = new QuestRewardFriendship[1];
         friendshipRewardArray[0] = new QuestRewardFriendship(friendshipRewardValue);
      }

      // Create a new quest node object
      QuestNode node = new QuestNode(_questNodeId, -1, npcText.text, userText.text, deliverObjectiveList.ToArray(),
         itemRewardList.ToArray(), friendshipRewardArray);

      return node;
   }

   #region Private Variables

   // The id of the quest node
   private int _questNodeId;

   #endregion
}
