using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class MonsterToolManager : MonoBehaviour {
   #region Public Variables

   // Reference to the tool scene
   public MonsterDataScene monsterToolScreen;

   #endregion

   private void Awake () {
      loadAllDataFiles();
   }

   public void loadAllDataFiles () {
      monsterDataList = new Dictionary<string, MonsterRawData>();
      // Build the path to the folder containing the Monster data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "MonsterStats");

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
            MonsterRawData monsterData = ToolsUtil.xmlLoad<MonsterRawData>(filePath);

            // Save the Monster data in the memory cache
            monsterDataList.Add(monsterData.enemyName, monsterData);
         }
         monsterToolScreen.updatePanelWithMonsterRawData(monsterDataList);
      }
   }

   public void deleteMonsterDataFile (MonsterRawData data) {
      // Build the file name
      string fileName = data.enemyName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "MonsterStats", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public bool ifExists (string nameID) {
      return monsterDataList.ContainsKey(nameID);
   }
   
   public void saveDataToFile (MonsterRawData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "MonsterStats");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.enemyName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "MonsterStats", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #region Private Variables

   private Dictionary<string, MonsterRawData> monsterDataList = new Dictionary<string, MonsterRawData>();

   #endregion
}
