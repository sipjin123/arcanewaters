using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class NPCIdInputScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The input field for the npc ID
   public InputField npcIdInput;

   // The gameobject displaying the 'id already exists' message
   public GameObject npcIdExistMessage;

   #endregion

   public void cancelButtonClickedOn () {
      hide();
   }

   public void createButtonClickedOn () {
      // Get the id from the input field
      int npcId = int.Parse(npcIdInput.text);

      // Verify that the id is not already used
      if (!NPCToolManager.self.isNPCIdFree(npcId)) {
         // Display an error message
         npcIdExistMessage.SetActive(true);
         return;
      } else {
         // Deactivate the error message
         npcIdExistMessage.SetActive(false);
      }

      // Create the new npc
      NPCToolManager.self.createNewNPC(npcId);

      // Hide this screen
      hide();
   }

   public void inputFieldValueChanged () {
      // Deactivate the 'id already used' error message
      npcIdExistMessage.SetActive(false);
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);

      // Clear the input field
      npcIdInput.text = "";
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   #region Private Variables

   #endregion
}
