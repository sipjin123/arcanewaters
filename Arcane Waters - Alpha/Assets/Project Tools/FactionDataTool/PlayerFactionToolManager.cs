using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class PlayerFactionToolManager : MonoBehaviour
{
   #region Public Variables

   // Holds the main scene for the player faction
   public PlayerFactionScene factionScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerFaction";

   #endregion

   private void Start () {
      loadXMLData();
   }

   public void saveXMLData (PlayerFactionData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.factionName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void deleteDataFile (PlayerFactionData data) {
      // Build the file name
      string fileName = data.factionName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void duplicateXMLData (PlayerFactionData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.factionName += "_copy";
      string fileName = data.factionName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void loadXMLData () {
      _playerFactionData = new Dictionary<string, PlayerFactionData>();
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
            PlayerFactionData playerFactionData = ToolsUtil.xmlLoad<PlayerFactionData>(filePath);

            // Save the data in the memory cache
            _playerFactionData.Add(playerFactionData.factionName, playerFactionData);
         }
         if (fileNames.Length > 0) {
            factionScene.loadPlayerFaction(_playerFactionData);
         }
      }
   }

   #region Private Variables

   // Holds the list of player faction data
   private Dictionary<string, PlayerFactionData> _playerFactionData = new Dictionary<string, PlayerFactionData>();

   #endregion
}
