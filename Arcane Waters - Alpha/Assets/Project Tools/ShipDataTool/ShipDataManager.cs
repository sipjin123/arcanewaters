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
   
   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<ShipData> shipDataList;

   #endregion

   public void Awake () {
      self = this;
   }

   private void Start () {
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
      loadXMLData(ShipDataToolManager.FOLDER_PATH);
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
