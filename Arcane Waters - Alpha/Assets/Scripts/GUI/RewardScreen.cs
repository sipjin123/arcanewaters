using UnityEngine;
using UnityEngine.UI;

public class RewardScreen : MonoBehaviour
{
   #region Public Variables

   public CanvasGroup canvasGroup;

   // Our various components that we need references to
   public Text text;

   public Button confirmButton;
   public Image imageIcon;

   #endregion Public Variables

   public void Show (Item item) {
      imageIcon.sprite = ImageManager.getSprite(item.getIconPath());
      show();
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
   }

   public void disableButtons () {
      canvasGroup.interactable = false;
   }
}