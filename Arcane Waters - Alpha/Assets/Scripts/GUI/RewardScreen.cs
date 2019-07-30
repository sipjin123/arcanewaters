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
      rewardRows[0].rewardIcon.sprite = ImageManager.getSprite(item.getIconPath());
      rewardRows[0].gameObject.SetActive(true);
      Global.player.rpc.Cmd_RequestItem(item);
   }

   public void setItemDataGroup (List<Item> itemList) {
      disableAll();
      for (int i = 0; i < itemList.Count; i++) {
         rewardRows[i].gameObject.SetActive(true);
         rewardRows[i].rewardIcon.sprite = ImageManager.getSprite(itemList[i].getIconPath());
         Global.player.rpc.Cmd_RequestItem(itemList[i]);
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