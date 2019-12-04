using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class JobManager : XmlManager {
   #region Public Variables

   // Self
   public static JobManager self;

   // Holds the xml raw data
   public List<TextAsset> textAssets;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public PlayerJobData getJobData (Jobs.Type jobtype) {
      PlayerJobData returnData = _jobData[jobtype];
      if (returnData == null) {
         Debug.LogWarning("The Job Does not Exist yet!: " + jobtype);
      }
      return returnData;
   }

   private void initializeDataCache () {
      _jobData = new Dictionary<Jobs.Type, PlayerJobData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerJobData jobData = Util.xmlLoad<PlayerJobData>(textAsset);
         Jobs.Type uniqueID = jobData.type;

         // Save the data in the memory cache
         if (!_jobData.ContainsKey(uniqueID)) {
            _jobData.Add(uniqueID, jobData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();

      textAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "PlayerJob");

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
            TextAsset textAsset = (TextAsset) UnityEditor.AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset));
            textAssets.Add(textAsset);
         }
      }
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #region Private Variables

   // The cached data collection
   private Dictionary<Jobs.Type, PlayerJobData> _jobData = new Dictionary<Jobs.Type, PlayerJobData>();

   #endregion
}