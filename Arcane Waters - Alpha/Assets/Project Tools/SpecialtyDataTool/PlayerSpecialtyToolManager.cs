using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class PlayerSpecialtyToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the player specialty
   public PlayerSpecialtyScene specialtySceneScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerSpecialty";

   #endregion

   private void Start () {
      loadXMLData();
   }

   public void saveXMLData (PlayerSpecialtyData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.specialtyName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void deleteDataFile (PlayerSpecialtyData data) {
      // Build the file name
      string fileName = data.specialtyName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void duplicateXMLData (PlayerSpecialtyData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.specialtyName += "_copy";
      string fileName = data.specialtyName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void loadXMLData () {
      _playerSpecialtyData = new Dictionary<string, PlayerSpecialtyData>();
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
            PlayerSpecialtyData specialtyData = ToolsUtil.xmlLoad<PlayerSpecialtyData>(filePath);

            // Save the data in the memory cache
            _playerSpecialtyData.Add(specialtyData.specialtyName, specialtyData);
         }
         if (fileNames.Length > 0) {
            specialtySceneScene.loadPlayerSpecialtyData(_playerSpecialtyData);
         }
      }
   }

   #region Private Variables

   // Holds the list of player specialty data
   private Dictionary<string, PlayerSpecialtyData> _playerSpecialtyData = new Dictionary<string, PlayerSpecialtyData>();

   #endregion
}
