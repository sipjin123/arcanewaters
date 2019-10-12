using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCSelectionScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The screen we use to enter a new NPC ID
   public NPCIdInputScreen npcIdInputScreen;

   // The screen we use to edit NPCs
   public NPCEditScreen npcEditScreen;

   // The container for the npc rows
   public GameObject rowsContainer;

   // The prefab we use for creating rows
   public NPCSelectionRow npcRowPrefab;
   
   #endregion

   public void updatePanelWithNPCs(Dictionary<int, NPCData> _npcData) {
      // Clear all the rows
      rowsContainer.DestroyChildren();

      // Create a row for each npc
      foreach(NPCData npcData in _npcData.Values) {
         // Create a new row
         NPCSelectionRow row = Instantiate(npcRowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForNPC(this, npcData.npcId, npcData.name);

         if (npcData.iconPath != "") {
            try {
               row.npcIcon.sprite = ImageManager.getSprite(npcData.iconPath);
            } catch {
               // Should be an Error Icon
               row.npcIcon.sprite = ImageManager.getSprite("Assets/Sprites/Icons/Stats/icon_vitality.png");
            }
         } else {
            // Should be a NULL Icon
            row.npcIcon.sprite = ImageManager.getSprite("Assets/Sprites/Icons/Stats/icon_luck.png");
         }
      }
   }

   public void editNPC(int npcId) {
      // Retrieve the NPC data
      NPCData data = NPCToolManager.self.getNPCData(npcId);

      // Initialize the NPC edition screen with the data
      npcEditScreen.updatePanelWithNPC(data);

      // Show the NPC edition screen
      npcEditScreen.show();
   }

   public void createNewNPCButtonClickedOn () {
      npcIdInputScreen.show();
   }

   public void exitButtonClickedOn() {
      Application.Quit();
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
      this.gameObject.SetActive(false);
   }

   #region Private Variables

   #endregion
}
