using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;

public class VoyageInviteScreen : FullScreenSeparatePanel
{
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

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public void Update () {
      // Hide the panel during battle
      if (Global.player == null || Global.player.isInBattle()) {
         hide();
      } else {
         show();
      }
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

   #region Private Variables

   #endregion
}