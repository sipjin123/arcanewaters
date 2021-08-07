using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   public PvpShopData getShopData (int shopId) {
      PvpShopData fetchedData = shopDataList.Find(_ => _.shopId == shopId);
      if (fetchedData == null) {
         return shopDataList[0];
      }
      return fetchedData;
   }

   public void initializDataCache () {
      if (!hasInitialized) {
         shopDataList = new List<PvpShopData>();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getPvpShopXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                  PvpShopData pvpShopData = Util.xmlLoad<PvpShopData>(newTextAsset);
                  pvpShopData.shopId = xmlPair.xmlId;
                  if (shopDataList.Find(_=> _.shopId == xmlPair.xmlId) == null) {
                     // Generate rarity randomizer for this shop
                     foreach (PvpShopItem pvpShopItem in pvpShopData.shopItems) {
                        pvpShopItem.rarityType = Rarity.getRandom();
                     }

                     shopDataList.Add(pvpShopData);
                  }
               }
               hasInitialized = true;
            });
         });
      }
   }

   #region Private Variables

   #endregion
}