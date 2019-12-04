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

   // Holds the path of the folder
   public const string FOLDER_PATH = "SeaMonsterStats";

   #endregion

   private void Awake () {
      loadAllDataFiles();
   }

   public void loadAllDataFiles () {
      monsterDataList = new Dictionary<string, SeaMonsterEntityData>();
      // Build the path to the folder containing the Monster data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);

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
            SeaMonsterEntityData monsterData = ToolsUtil.xmlLoad<SeaMonsterEntityData>(filePath);

            // Save the Monster data in the memory cache
            monsterDataList.Add(monsterData.monsterName, monsterData);
         }
         monsterToolScreen.updatePanelWithData(monsterDataList);
      }
   }

   public void deleteMonsterDataFile (SeaMonsterEntityData data) {
      // Build the file name
      string fileName = data.monsterName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public bool ifExists (string nameID) {
      return monsterDataList.ContainsKey(nameID);
   }

   public void saveDataToFile (SeaMonsterEntityData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.monsterName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void duplicateFile (SeaMonsterEntityData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.monsterName += "_copy";
      string fileName = data.monsterName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #region Private Variables

   // Holds the list of sea monster data
   private Dictionary<string, SeaMonsterEntityData> monsterDataList = new Dictionary<string, SeaMonsterEntityData>();

   #endregion
}
