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
      initializeDataCache();
   }

   public ShipData getShipData (Ship.Type shipType) {
      ShipData returnData = shipDataList.Find(_ => _.shipType == shipType);
      if (returnData == null) {
         return new ShipData();
      }
      return returnData;
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         shipDataList = new List<ShipData>();
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
