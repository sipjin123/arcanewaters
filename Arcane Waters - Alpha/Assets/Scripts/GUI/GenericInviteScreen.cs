using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericInviteScreen : MonoBehaviour {
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The inviter name
   public Text inviterNameText;

   // The accept button
   public Button acceptButton;

   // The refuse button
   public Button refuseButton;

   #endregion

   public void show () {
      if (this.canvasGroup.alpha < 1f) {
         this.canvasGroup.alpha = 1f;
         this.canvasGroup.blocksRaycasts = true;
         this.canvasGroup.interactable = true;
      }
   }

   public void hide () {
      if (this.canvasGroup.alpha > 0f) {
         this.canvasGroup.alpha = 0f;
         this.canvasGroup.blocksRaycasts = false;
         this.canvasGroup.interactable = false;
      }
   }

   public void activate (string inviterName) {
      // Set the inviter name
      inviterNameText.text = inviterName;

      // Make the panel visible
      this.gameObject.SetActive(true);
      show();
   }

   public virtual void deactivate () {
      this.gameObject.SetActive(false);
   }

   #region Private Variables

   #endregion
}
