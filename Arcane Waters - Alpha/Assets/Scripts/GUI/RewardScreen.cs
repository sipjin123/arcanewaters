using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardScreen : Panel
{
   #region Public Variables

   // Our various components that we need references to
   public Text text;

   // Closes the popup
   public Button confirmButton;

   // The icon of the reward item
   public List<RewardRow> rewardRows;

   #endregion Public Variables

   public override void Start () {
      base.Start();
      confirmButton.onClick.AddListener(() => {
         PanelManager.self.popPanel();
      });
   }

   public void setItemData (Item item) {
      // Get the casted item
      Item castedItem = item.getCastItem();

      // Disable all the rows
      disableAll();

      // Initialize the first row
      rewardRows[0].gameObject.SetActive(true);
      rewardRows[0].setRowForItem(castedItem);
   }

   public void setItemDataGroup (List<Item> itemList) {
      // Disable all the rows
      disableAll();

      for (int i = 0; i < itemList.Count; i++) {
         // Get the casted item
         Item currItem = itemList[i].getCastItem();

         // Enable the row
         rewardRows[i].gameObject.SetActive(true);

         // Initialize the row
         rewardRows[0].setRowForItem(currItem);
      }
   }

   private void disableAll() {
      for(int i = 0; i < rewardRows.Count; i++) {
         rewardRows[i].gameObject.SetActive(false);
      }
   }

   public override void show () {
      base.show();
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public override void hide () {
      base.hide();
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
   }

   public void disableButtons () {
      canvasGroup.interactable = false;
   }
}