using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;

public class VoyageInviteScreen : MonoBehaviour
{
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

   public void show (int voyageGroupId, string inviterName) {
      // Set the voyage group id
      _voyageGroupId = voyageGroupId;

      // Set the inviter name
      inviterNameText.text = inviterName;

      // Make the panel visible
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public void Update () {
      if (Global.player == null) {
         return;
      }

      // Automatically ignore the invite if the player enters battle or if he joins the group by other means
      if (Global.player.isInBattle() || Global.player.voyageGroupId == _voyageGroupId) {
         hide();
      }
   }

   public int getVoyageGroupId () {
      return _voyageGroupId;
   }

   #region Private Variables

   // The id of the voyage group the user was invited to
   private int _voyageGroupId = -1;

   #endregion
}