using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class ShipDataToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the ship data
   public ShipDataScene shipDataScene;

   #endregion

   private void Start () {
      loadXMLData();
   }

   public void saveXMLData (ShipData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "ShipStats");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.shipName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "ShipStats", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void deleteMonsterDataFile (ShipData data) {
      // Build the file name
      string fileName = data.shipName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "ShipStats", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void duplicateXMLData (ShipData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "ShipStats");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.shipName += "_copy";
      string fileName = data.shipName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "ShipStats", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void loadXMLData () {
      _shipDataList = new Dictionary<string, ShipData>();
      // Build the path to the folder containing the Ship data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "ShipStats");

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
            ShipData shipData = ToolsUtil.xmlLoad<ShipData>(filePath);

            // Save the Ship data in the memory cache
            _shipDataList.Add(shipData.shipName, shipData);
         }
         if (fileNames.Length > 0) {
            shipDataScene.loadShipData(_shipDataList);
         }
      }
   }

   #region Private Variables

   // Holds the list of ship data
   private Dictionary<string, ShipData> _shipDataList = new Dictionary<string, ShipData>();

   #endregion
}
