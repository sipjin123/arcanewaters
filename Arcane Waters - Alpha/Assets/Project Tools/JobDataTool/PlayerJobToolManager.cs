using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class PlayerJobToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the player job
   public PlayerJobScene jobSceneScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerJob";

   #endregion

   private void Start () {
      loadXMLData();
   }

   public void saveXMLData (PlayerJobData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.jobName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void deleteDataFile (PlayerJobData data) {
      // Build the file name
      string fileName = data.jobName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void duplicateXMLData (PlayerJobData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.jobName += "_copy";
      string fileName = data.jobName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void loadXMLData () {
      _playerJobData = new Dictionary<string, PlayerJobData>();
      // Build the path to the folder containing the data XML files
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
            PlayerJobData jobData = ToolsUtil.xmlLoad<PlayerJobData>(filePath);

            // Save the data in the memory cache
            _playerJobData.Add(jobData.jobName, jobData);
         }
         if (fileNames.Length > 0) {
            jobSceneScene.loadPlayerJobData(_playerJobData);
         }
      }
   }

   #region Private Variables

   // Holds the list of player job data
   private Dictionary<string, PlayerJobData> _playerJobData = new Dictionary<string, PlayerJobData>();

   #endregion
}
