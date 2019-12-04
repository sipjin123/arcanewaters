using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class FactionManager : XmlManager {
   #region Public Variables

   // Self
   public static FactionManager self;

   // Holds the xml raw data
   public List<TextAsset> textAssets;

   #endregion

   public void Awake () {
      self = this;
      translateXMLData();
   }

   public PlayerFactionData getFactionData (Faction.Type factionType) {
      PlayerFactionData returnData = _factionData[factionType];
      if (returnData == null) {
         Debug.LogWarning("The Faction Does not Exist yet!: " + factionType);
      }
      return returnData;
   }

   private void translateXMLData () {
      _factionData = new Dictionary<Faction.Type, PlayerFactionData>();

      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         PlayerFactionData factionData = Util.xmlLoad<PlayerFactionData>(textAsset);
         Faction.Type uniqueID = factionData.type;

         // Save the data in the memory cache
         if (!_factionData.ContainsKey(uniqueID)) {
            _factionData.Add(uniqueID, factionData);
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();

      textAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "PlayerFaction");

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
   private Dictionary<Faction.Type, PlayerFactionData> _factionData = new Dictionary<Faction.Type, PlayerFactionData>();

   #endregion
}