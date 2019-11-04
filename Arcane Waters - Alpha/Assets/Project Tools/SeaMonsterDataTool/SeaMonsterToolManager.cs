using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class SeaMonsterToolManager : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool scene
   public SeaMonsterDataScene monsterToolScreen;

   #endregion

   private void Awake () {
      loadAllDataFiles();
   }

   public void loadAllDataFiles () {
      monsterDataList = new Dictionary<string, SeaMonsterEntityDataCopy>();
      // Build the path to the folder containing the Monster data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "SeaMonsterStats");

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
            SeaMonsterEntityDataCopy monsterData = ToolsUtil.xmlLoad<SeaMonsterEntityDataCopy>(filePath);

            // Save the Monster data in the memory cache
            monsterDataList.Add(monsterData.monsterName, monsterData);
         }
         monsterToolScreen.updatePanelWithData(monsterDataList);
      }
   }

   public void deleteMonsterDataFile (SeaMonsterEntityDataCopy data) {
      // Build the file name
      string fileName = data.monsterName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "SeaMonsterStats", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public bool ifExists (string nameID) {
      return monsterDataList.ContainsKey(nameID);
   }

   public void saveDataToFile (SeaMonsterEntityDataCopy data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "SeaMonsterStats");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.monsterName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "SeaMonsterStats", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #region Private Variables

   private Dictionary<string, SeaMonsterEntityDataCopy> monsterDataList = new Dictionary<string, SeaMonsterEntityDataCopy>();

   #endregion
}
