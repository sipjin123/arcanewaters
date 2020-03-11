using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Linq;
using UnityEngine.Events;

public class ShipDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShipDataManager self;
   
   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the ship data
   public List<ShipData> shipDataList = new List<ShipData>();

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent(); 

   #endregion

   public void Awake () {
      self = this;
   }

   public ShipData getShipData (Ship.Type shipType) {
      if (!_shipData.ContainsKey(shipType)) {
         D.debug("Failed to fetch ship data: " + shipType);
         return new ShipData();
      }
      ShipData returnData = _shipData[shipType];
      return returnData;
   }

   public void initializeDataCache () {
      if (!hasInitialized) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getShipXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in rawXMLData) {
                  try {
                     TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                     ShipData shipData = Util.xmlLoad<ShipData>(newTextAsset);
                     Ship.Type uniqueID = shipData.shipType;

                     // Save the ship data in the memory cache
                     if (!_shipData.ContainsKey(uniqueID) && xmlPair.isEnabled) {
                        _shipData.Add(uniqueID, shipData);
                        shipDataList.Add(shipData);
                     }
                  } catch {
                     D.warning("Failed to load ship xml data for: " + xmlPair.xmlId);
                  }
               }
               hasInitialized = true;
               finishedDataSetup.Invoke();
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
      hasInitialized = true;
      finishedDataSetup.Invoke();
   }

   #region Private Variables

   // The cached ship data 
   private Dictionary<Ship.Type, ShipData> _shipData = new Dictionary<Ship.Type, ShipData>();

   #endregion
}
