using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class AchievementToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the data templates
   public AchievementToolScene achievementToolScene;

   #endregion

   private void Start () {
      loadXMLData();
   }

   public void saveXMLData (AchievementData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "Achievement");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.achievementName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "Achievement", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void deleteMonsterDataFile (AchievementData data) {
      // Build the file name
      string fileName = data.achievementName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "Achievement", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void duplicateXMLData (AchievementData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "Achievement");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.achievementName += "_copy";
      string fileName = data.achievementName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "Achievement", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void loadXMLData () {
      _achievementDataList = new Dictionary<string, AchievementData>();
      // Build the path to the folder containing the achievement data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "Achievement");

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
            AchievementData achievementData = ToolsUtil.xmlLoad<AchievementData>(filePath);

            // Save the achievement data in the memory cache
            _achievementDataList.Add(achievementData.achievementName, achievementData);
         }
         if (fileNames.Length > 0) {
            achievementToolScene.loadAchievementData(_achievementDataList);
         }
      }
   }

   #region Private Variables

   // Holds the list of achievement data
   private Dictionary<string, AchievementData> _achievementDataList = new Dictionary<string, AchievementData>();

   #endregion
}
