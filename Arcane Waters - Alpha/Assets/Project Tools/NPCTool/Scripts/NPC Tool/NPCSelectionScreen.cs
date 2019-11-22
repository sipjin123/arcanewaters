using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCSelectionScreen : MonoBehaviour
{
   #region Public Variables

   // Reference to npc tool manager
   public NPCToolManager npcToolManager;

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

   // The list of the rows created
   public List<NPCSelectionRow> npcRowList = new List<NPCSelectionRow>();

   #endregion

   public void updatePanelWithNPCs(Dictionary<int, NPCData> _npcData) {
      // Clear all the rows
      rowsContainer.DestroyChildren();
      npcRowList = new List<NPCSelectionRow>();

      // Create a row for each npc
      foreach (NPCData npcData in _npcData.Values) {
         // Create a new row
         NPCSelectionRow row = Instantiate(npcRowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForNPC(this, npcData.npcId, npcData.name);
         row.deleteButton.onClick.AddListener(() => deleteNPC(npcData.npcId));
         row.duplicateButton.onClick.AddListener(() => {
            npcData.npcId = 0;
            npcToolManager.duplicateFile(npcData);
         });

         if (npcData.iconPath != "") {
            try {
               row.npcIcon.sprite = ImageManager.getSprite(npcData.iconPath);
            } catch {
               // Should be an Error Icon
               row.npcIcon.sprite = ImageManager.getSprite("Assets/Sprites/Icons/Stats/icon_precision.png");
            }
         } else {
            // Should be a NULL Icon
            row.npcIcon.sprite = ImageManager.getSprite("Assets/Sprites/Icons/Stats/icon_luck.png");
         }

         npcRowList.Add(row);
      }

      if (!_hasBeenInitialized) {
         _hasBeenInitialized = true;
         string iconPath = "Assets/Sprites/Faces/";
         List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(iconPath);

         foreach (ImageManager.ImageData imgData in spriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            npcEditScreen.iconSpriteList.Add(imgData.imagePath, sourceSprite);
         }

         string spritePath = "Assets/Sprites/NPCs/Bodies/";
         List<ImageManager.ImageData> spriteFiles = ImageManager.getSpritesInDirectory(spritePath);

         foreach (ImageManager.ImageData imgData in spriteFiles) {
            Sprite sourceSprite = imgData.sprite;
            npcEditScreen.avatarSpriteList.Add(imgData.imagePath, sourceSprite);
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

   public void deleteNPC (int npcId) {
      NPCSelectionRow selectionRow = npcRowList.Find(_ => int.Parse(_.npcIdText.text) == npcId);
      GameObject rowObj = selectionRow.gameObject;
      npcRowList.Remove(selectionRow);
      Destroy(rowObj,.5f);

      // Retrieve the NPC data
      NPCData data = NPCToolManager.self.getNPCData(npcId);

      // Initialize the NPC edition screen with the data
      NPCToolManager.self.deleteEntireNPCData(data);
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

   // Determines if the sprites were initialied
   protected bool _hasBeenInitialized;

   #endregion
}
