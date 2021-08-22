using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class PvpShopManager : MonoBehaviour {
   #region Public Variables

   // Reference to self
   public static PvpShopManager self;

   // List of pvp shop data
   public List<PvpShopData> shopDataList = new List<PvpShopData>();

   // If the data has been initialized
   public bool hasInitialized = false;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeDataCache () {
      ShipDataManager.self.finishedDataSetup.AddListener(() => {
         finalizeDataSetup();
      });
      ShipAbilityManager.self.finishedDataSetup.AddListener(() => {
         finalizeDataSetup();
      });
   }

   public PvpShopData getShopData (int shopId) {
      PvpShopData fetchedData = shopDataList.Find(_ => _.shopId == shopId);
      if (fetchedData == null) {
         return shopDataList[0];
      }
      return fetchedData;
   }

   private void finalizeDataSetup () {
      if (!hasInitialized && ShipDataManager.self.hasInitialized && ShipAbilityManager.self.hasInitialized) {
         hasInitialized = true;
         shopDataList = new List<PvpShopData>();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getPvpShopXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                  PvpShopData pvpShopData = Util.xmlLoad<PvpShopData>(newTextAsset);
                  pvpShopData.shopId = xmlPair.xmlId;
                  if (shopDataList.Find(_ => _.shopId == xmlPair.xmlId) == null) {
                     // Generate rarity randomizer for this shop
                     foreach (PvpShopItem pvpShopItem in pvpShopData.shopItems) {
                        pvpShopItem.rarityType = Rarity.Type.Common;

                        // Inject ship info data to item data variable
                        if (pvpShopItem.shopItemType == PvpShopItem.PvpShopItemType.Ship) {
                           ShipData shipData = ShipDataManager.self.getShipData(pvpShopItem.itemId);
                           ShipInfo newShipData = Ship.generateNewShip(shipData.shipType, pvpShopItem.rarityType);
                           newShipData.shipAbilities = ShipDataManager.self.getShipAbilities(shipData.shipID);

                           // Serialize ability data
                           if (shipData != null) {
                              XmlSerializer ser = new XmlSerializer(newShipData.GetType());
                              StringBuilder sb = new StringBuilder();
                              using (XmlWriter writer = XmlWriter.Create(sb)) {
                                 ser.Serialize(writer, newShipData);
                              }

                              string newXmlData = sb.ToString();
                              pvpShopItem.itemData = newXmlData;
                           }
                        }
                     }

                     pvpShopData.shopItems.Add(PvpShopItem.defaultConsumableItem());
                     shopDataList.Add(pvpShopData);
                  }
               }
            });
         });
      }
   }

   #region Private Variables

   #endregion
}