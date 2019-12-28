using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Linq;

public class ShipDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShipDataManager self;
   
   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the ship data
   public List<ShipData> shipDataList = new List<ShipData>();

   #endregion

   public void Awake () {
      self = this;
   }

   public ShipData getShipData (Ship.Type shipType) {
      ShipData returnData = _shipData[shipType];
      if (returnData == null) {
         return new ShipData();
      }
      return returnData;
   }

   public void initializeDataCache () {
      if (!hasInitialized) {
         hasInitialized = true;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<string> rawXMLData = DB_Main.getShipXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (string rawText in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(rawText);
                  ShipData shipData = Util.xmlLoad<ShipData>(newTextAsset);
                  Ship.Type uniqueID = shipData.shipType;

                  // Save the ship data in the memory cache
                  if (!_shipData.ContainsKey(uniqueID)) {
                     _shipData.Add(uniqueID, shipData);
                     shipDataList.Add(shipData);
                  }
               }

               ShopManager.self.initializeRandomGeneratedShips();
            });
         });
      }
   }

   public void receiveShipDataFromServer (List<ShipData> shipDataList) {
      foreach (ShipData shipData in shipDataList) {
         if (!_shipData.ContainsKey(shipData.shipType)) {
            _shipData.Add(shipData.shipType, shipData);
            this.shipDataList.Add(shipData);
         }
      }
   }

   #region Private Variables

   // The cached ship data 
   private Dictionary<Ship.Type, ShipData> _shipData = new Dictionary<Ship.Type, ShipData>();

   #endregion
}
