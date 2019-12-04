using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class ClassManager : XmlManager {
   #region Public Variables

   // Self
   public static ClassManager self;

   // Holds the xml raw data
   public List<TextAsset> textAssets;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public PlayerClassData getClassData (Class.Type classType) {
      PlayerClassData returnData = _classData[classType];
      if (returnData == null) {
         Debug.LogWarning("The Class Does not Exist yet!: " + classType);
      }
      return returnData;
   }

   private void initializeDataCache () {
      _classData = new Dictionary<Class.Type, PlayerClassData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerClassData classData = Util.xmlLoad<PlayerClassData>(textAsset);
         Class.Type uniqueID = classData.type;

         // Save the data in the memory cache
         if (!_classData.ContainsKey(uniqueID)) {
            _classData.Add(uniqueID, classData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();

      textAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "PlayerClass");

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
   private Dictionary<Class.Type, PlayerClassData> _classData = new Dictionary<Class.Type, PlayerClassData>();

   #endregion
}