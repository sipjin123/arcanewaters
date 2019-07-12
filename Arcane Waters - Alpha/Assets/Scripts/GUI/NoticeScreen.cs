using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NoticeScreen : MonoBehaviour {
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // Our various components that we need references to
   public Text text;
   public Button confirmButton;

   #endregion

   public void show (string newText) {
      text.text = newText;

      // Now make us visible
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

   #region Private Variables

   #endregion
}
