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

   // The starting ship id
   public const int STARTING_SHIP_ID = 118;

   // The xml id of the starting ships from caravel to buss in the web tool
   public static int[] ALL_STARTING_SHIP_IDS = new int[] {118, 119, 120, 121, 122, 123, 124, 125 };

   #endregion

   public void Awake () {
      self = this;
   }

   public ShipData getShipData (int shipXmlId) {
      if (!_shipData.Values.ToList().Exists(_=>_.shipID == shipXmlId)) {
         D.debug("Failed to fetch ship data using Xml Id: {" + shipXmlId + "}");
         return _shipData.Values.ToList()[0];
      }
      ShipData returnData = _shipData.Values.ToList().Find(_=>_.shipID == shipXmlId);
      return returnData;
   }

   public ShipData getShipData (Ship.Type shipType, bool returnDefault = true) {
      if (!_shipData.Values.ToList().Exists(_=> _.shipType == shipType) && (int) shipType != -1 && returnDefault) {
         D.debug("Failed to fetch ship data: {" + shipType + " : " + (int) shipType + "}");
         return _shipData.Values.ToList()[0];
      }

      // Always get the first entry of ship type, the next entries are the variants
      List<ShipData> allShipData = _shipData.Values.ToList().FindAll(_ => _.shipType == shipType);
      return allShipData[0];
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
                     shipData.shipID = xmlPair.xmlId;
                     int uniqueID = shipData.shipID;
                     // Save the ship data in the memory cache
                     if (!_shipData.ContainsKey(uniqueID) && xmlPair.isEnabled) {
                        _shipData.Add(uniqueID, shipData);
                        shipDataList.Add(shipData);
                     }
                  } catch {
                     D.debug("Failed to load ship xml data for: " + xmlPair.xmlId);
                  }
               }
               hasInitialized = true;
               finishedDataSetup.Invoke();
            });
         });
      }
   }

   public ShipAbilityInfo getShipAbilities (int shipId) {
      ShipData shipData = getShipData(shipId);
      if (shipData == null) {
         return new ShipAbilityInfo();
      }

      List<int> abilityIdList = new List<int>();
      foreach (ShipAbilityPair shipAbilities in shipData.shipAbilities) {
         abilityIdList.Add(shipAbilities.abilityId);
      }

      ShipAbilityInfo newShipAbilityInfo = new ShipAbilityInfo {
         ShipAbilities = abilityIdList.ToArray()
      };

      return newShipAbilityInfo;
   }

   public void receiveShipDataFromZipData (Dictionary<int, ShipData> shipDataList) {
      foreach (KeyValuePair<int, ShipData> shipData in shipDataList) {
         if (!_shipData.ContainsKey(shipData.Key)) {
            _shipData.Add(shipData.Key, shipData.Value);
            this.shipDataList.Add(shipData.Value);
         }
      }
      hasInitialized = true;
      finishedDataSetup.Invoke();
   }

   #region Private Variables

   // The cached ship data 
   private Dictionary<int, ShipData> _shipData = new Dictionary<int, ShipData>();

   #endregion
}
