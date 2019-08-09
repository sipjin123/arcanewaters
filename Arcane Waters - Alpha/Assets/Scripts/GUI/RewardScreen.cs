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
      disableAll();
      rewardRows[0].rewardIcon.sprite = ImageManager.getSprite(item.getCastItem().getIconPath());
      rewardRows[0].gameObject.SetActive(true);
      rewardRows[0].setQuantityText(item.count.ToString());
      rewardRows[0].rewardName.text = item.getCastItem().getName();
      rewardRows[0].quantityContainer.SetActive(item.count > 1);
   }

   public void setItemDataGroup (List<Item> itemList) {
      disableAll();
      for (int i = 0; i < itemList.Count; i++) {
         Item currItem = itemList[i].getCastItem();
         rewardRows[i].gameObject.SetActive(true);
         rewardRows[i].rewardIcon.sprite = ImageManager.getSprite(currItem.getIconPath());
         rewardRows[i].setQuantityText(currItem.count.ToString());
         rewardRows[i].rewardName.text = currItem.getName();
         rewardRows[i].quantityContainer.SetActive(currItem.count > 1);
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