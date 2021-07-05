using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpInviteScreen : FullScreenSeparatePanel {
   #region Public Variables

   // Self
   public static PvpInviteScreen self;

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The inviter name
   public Text inviterNameText;

   // The accept button
   public Button acceptButton;

   // The refuse button
   public Button refuseButton;

   #endregion

   private void Awake () {
      self = this;
   }

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

   // TODO: Setup pvp invite logic here for team combat

   #region Private Variables

   #endregion
}
