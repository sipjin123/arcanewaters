using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class CraftingToolManager : MonoBehaviour {
   #region Public Variables

   // Reference to the tool scene
   public CraftingToolScene craftingToolScreen;

   #endregion

   private void Awake () {
      loadAllDataFiles();
   }

   public void loadAllDataFiles () {
      craftingDataList = new Dictionary<string, CraftableItemRequirements>();
      // Build the path to the folder containing the Crafting data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "Crafting");

      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      } else {
         // Get the list of XML files in the folder
         string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

         // Iterate over the files
         for (int i = 0; i < fileNames.Length; i++) {
            // Build the path to a single file
            string filePath = Path.Combine(directoryPath, fileNames[i]);

            // Read and deserialize the file
            CraftableItemRequirements craftingData = ToolsUtil.xmlLoad<CraftableItemRequirements>(filePath);

            // Save the Crafting data in the memory cache
            craftingDataList.Add(craftingData.resultItem.category == Item.Category.None ? "Undefined" : craftingData.resultItem.getCastItem().getName(), craftingData);
         }
         craftingToolScreen.updatePanelWithCraftingIngredients(craftingDataList);
      }
   }

   public void deleteCraftingDataFile (CraftableItemRequirements data) {
      // Build the file name
      string fileName = data.resultItem.category == Item.Category.None ? "Undefined" : data.resultItem.category.ToString() + "_" + data.resultItem.getCastItem().getName();

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "Crafting", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public bool ifExists (string nameID) {
      return craftingDataList.ContainsKey(nameID);
   }

   public void saveDataToFile (CraftableItemRequirements data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "Crafting");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.resultItem.category == Item.Category.None ? "Undefined" : data.resultItem.category.ToString() + "_" + data.resultItem.getCastItem().getName();

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "Crafting", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void duplicateDataFile (CraftableItemRequirements data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "Crafting");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      data.resultItem.category = Item.Category.None;
      data.resultItem.itemTypeId = 0;

      // Build the file name
      string fileName = data.resultItem.category == Item.Category.None ? "Undefined" : data.resultItem.category.ToString() + "_" + data.resultItem.getCastItem().getName();

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "Crafting", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #region Private Variables

   private Dictionary<string, CraftableItemRequirements> craftingDataList = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
