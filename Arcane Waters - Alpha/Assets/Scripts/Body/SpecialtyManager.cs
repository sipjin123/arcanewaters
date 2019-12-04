using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class SpecialtyManager : XmlManager {
   #region Public Variables

   // Self
   public static SpecialtyManager self;

   // Holds the xml raw data
   public List<TextAsset> textAssets;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public PlayerSpecialtyData getSpecialtyData (Specialty.Type specialtytype) {
      PlayerSpecialtyData returnData = _specialtyData[specialtytype];
      if (returnData == null) {
         Debug.LogWarning("The Specialty Does not Exist yet!: " + specialtytype);
      }
      return returnData;
   }

   private void initializeDataCache () {
      _specialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerSpecialtyData specialtyData = Util.xmlLoad<PlayerSpecialtyData>(textAsset);
         Specialty.Type uniqueID = specialtyData.type;

         // Save the data in the memory cache
         if (!_specialtyData.ContainsKey(uniqueID)) {
            _specialtyData.Add(uniqueID, specialtyData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();

      textAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "PlayerSpecialty");

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
   private Dictionary<Specialty.Type, PlayerSpecialtyData> _specialtyData = new Dictionary<Specialty.Type, PlayerSpecialtyData>();

   #endregion
}