using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class UsableItemDataToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the usable item data
   public UsableItemDataScene usableItemDataScene;

   #endregion

   private void Start () {
      loadXMLData();
   }

   public void saveXMLData (UsableItemData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "UsableItem");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.itemName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "UsableItem", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void deleteMonsterDataFile (UsableItemData data) {
      // Build the file name
      string fileName = data.itemName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "UsableItem", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void duplicateXMLData (UsableItemData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "UsableItem");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.itemName += "_copy";
      string fileName = data.itemName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "UsableItem", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void loadXMLData () {
      _usableItemDataList = new Dictionary<string, UsableItemData>();
      // Build the path to the folder containing the usable item data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "UsableItem");

      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      } else {
         // Get the list of XML files in the folder
         string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

         // Iterate over the files
         foreach (string fileName in fileNames) {
            // Build the path to a single file
            string filePath = Path.Combine(directoryPath, fileName);

            // Read and deserialize the file
            UsableItemData usableItemData = ToolsUtil.xmlLoad<UsableItemData>(filePath);

            // Save the usable item data in the memory cache
            _usableItemDataList.Add(usableItemData.itemName, usableItemData);
         }
         if (fileNames.Length > 0) {
            usableItemDataScene.loadUsableItemData(_usableItemDataList);
         }
      }
   }

   #region Private Variables

   // Holds the list of usable item data
   private Dictionary<string, UsableItemData> _usableItemDataList = new Dictionary<string, UsableItemData>();

   #endregion
}
