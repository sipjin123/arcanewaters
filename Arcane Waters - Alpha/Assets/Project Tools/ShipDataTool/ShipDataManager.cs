using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShipDataManager self;

   // The files containing the ship data
   public TextAsset[] shipDataAssets;

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
         foreach (TextAsset textAsset in shipDataAssets) {
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

   #region Private Variables

   // The cached ship data 
   private Dictionary<Ship.Type, ShipData> _shipData = new Dictionary<Ship.Type, ShipData>();

   #endregion
}
