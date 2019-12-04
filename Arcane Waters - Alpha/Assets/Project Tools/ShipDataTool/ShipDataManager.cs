using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class ShipDataManager : XmlManager {
   #region Public Variables

   // Self
   public static ShipDataManager self;

   // Holds the xml raw data
   public List<TextAsset> textAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<ShipData> shipDataList;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public ShipData getShipData (Ship.Type shipType) {
      ShipData returnData = shipDataList.Find(_ => _.shipType == shipType);
      return returnData;
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         shipDataList = new List<ShipData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in textAssets) {
            // Read and deserialize the file
            ShipData shipData = Util.xmlLoad<ShipData>(textAsset);
            Ship.Type uniqueID = shipData.shipType;

            // Save the ship data in the memory cache
            if (!_shipData.ContainsKey(uniqueID)) {
               _shipData.Add(uniqueID, shipData);
               shipDataList.Add(shipData);
            }
         }
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();

      textAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "ShipStats");

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

   // The cached ship data 
   private Dictionary<Ship.Type, ShipData> _shipData = new Dictionary<Ship.Type, ShipData>();

   #endregion
}
