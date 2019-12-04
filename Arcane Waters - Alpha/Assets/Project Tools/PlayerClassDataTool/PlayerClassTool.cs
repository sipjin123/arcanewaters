using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class PlayerClassTool : MonoBehaviour
{
   #region Public Variables

   // Holds the main scene for the player class
   public PlayerClassScene classScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerClass";

   #endregion

   private void Start () {
      loadXMLData();
   }

   public void saveXMLData (PlayerClassData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.className;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void deleteDataFile (PlayerClassData data) {
      // Build the file name
      string fileName = data.className;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void duplicateXMLData (PlayerClassData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.className += "_copy";
      string fileName = data.className;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void loadXMLData () {
      _playerClassData = new Dictionary<string, PlayerClassData>();
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
            PlayerClassData playerClassData = ToolsUtil.xmlLoad<PlayerClassData>(filePath);

            // Save the data in the memory cache
            _playerClassData.Add(playerClassData.className, playerClassData);
         }
         if (fileNames.Length > 0) {
            classScene.loadPlayerClass(_playerClassData);
         }
      }
   }

   #region Private Variables

   // Holds the list of player class data
   private Dictionary<string, PlayerClassData> _playerClassData = new Dictionary<string, PlayerClassData>();

   #endregion
}