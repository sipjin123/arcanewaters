using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

   // Button for creating templates
   public UnityEngine.UI.Button createButton;

   #endregion

   public void updatePanelWithNPCs(Dictionary<int, NPCData> _npcData) {
      // Clear all the rows
      rowsContainer.DestroyChildren();
      npcRowList = new List<NPCSelectionRow>();

      // Create a row for each npc
      foreach (NPCData npcData in _npcData.Values) {
         // Create a new row
         NPCSelectionRow row = GenericEntryTemplate.CreateGenericTemplate(npcRowPrefab.gameObject, npcToolManager, rowsContainer.transform).GetComponent<NPCSelectionRow>(); 
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForNPC(this, npcData.npcId, npcData.name);
         row.deleteButton.onClick.AddListener(() => deleteNPC(npcData.npcId));
         row.duplicateButton.onClick.AddListener(() => {
            npcData.npcId = 0;
            npcToolManager.duplicateFile(npcData);
         });

         if (npcData.iconPath != "") {
            try {
               row.itemIcon.sprite = ImageManager.getSprite(npcData.iconPath);
            } catch {
               // Should be an Error Icon
               row.itemIcon.sprite = ImageManager.getSprite("Assets/Sprites/Icons/Stats/icon_precision.png");
            }
         } else {
            // Should be a NULL Icon
            row.itemIcon.sprite = ImageManager.getSprite("Assets/Sprites/Icons/Stats/icon_luck.png");
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

      if (!MasterToolAccountManager.canAlterData()) {
         createButton.gameObject.SetActive(false);
      }
   }

   public void openMasterScene () {
      SceneManager.LoadScene(MasterToolScene.masterScene);
   }

   public void editNPC(int npcId) {
      // Retrieve the NPC data
      NPCData data = NPCToolManager.instance.getNPCData(npcId);

      // Initialize the NPC edition screen with the data
      npcEditScreen.updatePanelWithNPC(data);

      // Show the NPC edition screen
      npcEditScreen.show();
   }

   public void deleteNPC (int npcId) {
      NPCSelectionRow selectionRow = npcRowList.Find(_ => int.Parse(_.indexText.text) == npcId);
      GameObject rowObj = selectionRow.gameObject;
      npcRowList.Remove(selectionRow);
      Destroy(rowObj,.5f);

      // Retrieve the NPC data
      NPCData data = NPCToolManager.instance.getNPCData(npcId);

      // Initialize the NPC edition screen with the data
      NPCToolManager.instance.deleteNPCDataFile(data);
   }

   public void createNewNPCButtonClickedOn () {
      npcIdInputScreen.show();
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
