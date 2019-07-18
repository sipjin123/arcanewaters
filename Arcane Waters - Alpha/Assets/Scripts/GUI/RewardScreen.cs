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
   public Image imageIcon;

   #endregion Public Variables

   public override void Start () {
      base.Start();
      confirmButton.onClick.AddListener(() => {
         PanelManager.self.popPanel();
      });
   }

   public void setItemData (Item item) {
      imageIcon.sprite = ImageManager.getSprite(item.getIconPath());
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