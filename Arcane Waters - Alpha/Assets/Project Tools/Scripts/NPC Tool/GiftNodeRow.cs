using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GiftNodeRow : MonoBehaviour
{
   #region Public Variables

   // The container for the item rewards
   public GameObject itemRewardRowsContainer;

   // The prefab we use for creating item rewards
   public ItemRewardRow itemRewardPrefab;

   // The button for triggering creation of reward
   public Button createRewardButton;

   // Data cache for gifts
   public NPCGiftData cachedGiftData;

   // Cache List of gifts
   public List<NPCGiftData> cachedGiftList;

   #endregion

   public void setRowForQuestNode (List<NPCGiftData> node) {
      // Clear all the rows
      itemRewardRowsContainer.DestroyChildren();

      cachedGiftList = new List<NPCGiftData>();
      // Create a row for each deliver objective
      if (node != null) {
         foreach (NPCGiftData itemReward in node) {
            // Create a new row
            ItemRewardRow row = Instantiate(itemRewardPrefab, itemRewardRowsContainer.transform, false);
            row.transform.SetParent(itemRewardRowsContainer.transform, false);

            QuestRewardItem questReward = new QuestRewardItem();
            questReward.itemTypeId = itemReward.itemTypeId;
            questReward.category = itemReward.itemCategory;

            row.setRowForItemReward(questReward);
            cachedGiftList.Add(itemReward);
         }
      }

      createRewardButton.onClick.AddListener(() => createGiftButtonClickedOn());
   }

   private void createGiftButtonClickedOn() {
      NPCGiftData newGiftData = new NPCGiftData();
      ItemRewardRow row = Instantiate(itemRewardPrefab, itemRewardRowsContainer.transform, false);
      row.transform.SetParent(itemRewardRowsContainer.transform, false);

      cachedGiftList.Add(newGiftData);

      row.updateButton.onClick.AddListener(() => updateRewardButtonClicked(row, newGiftData));
   }

   private void updateRewardButtonClicked (ItemRewardRow row, NPCGiftData giftData) {
      giftData.itemCategory = (Item.Category) int.Parse(row.itemCategory.text.ToString());
      giftData.itemTypeId = int.Parse(row.itemTypeId.text.ToString());

      QuestRewardItem questReward = new QuestRewardItem();
      questReward.itemTypeId = giftData.itemTypeId;
      questReward.category = giftData.itemCategory;

      row.setRowForItemReward(questReward);
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

   #region Private Variables

   // The id of the quest node
   private int _questNodeId;

   #endregion
}
