using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System.Linq;
using static ShopDataToolManager;

public class ShopXMLManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShopXMLManager self;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<ShopData> shopDataList = new List<ShopData>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializDataCache () {
      if (!hasInitialized) {
         shopDataList = new List<ShopData>();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getShopXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                  ShopData shopData = Util.xmlLoad<ShopData>(newTextAsset);
                  shopData.shopId = xmlPair.xmlId;
                  if (!_shopData.ContainsKey(xmlPair.xmlId)) {
                     _shopData.Add(xmlPair.xmlId, shopData);
                     shopDataList.Add(shopData);
                  }
               }
               hasInitialized = true;
               finishedDataSetup.Invoke();
            });
         });
      }
   }

   public void receiveDataFromZipData (ShopDataGroup[] dataCollection) {
      if (!hasInitialized) {
         shopDataList = new List<ShopData>();
         foreach (ShopDataGroup shopDataGroup in dataCollection) {
            if (!_shopData.ContainsKey(shopDataGroup.xmlId)) {
               _shopData.Add(shopDataGroup.xmlId, shopDataGroup.shopData);
               shopDataList.Add(shopDataGroup.shopData);
            }
         }
         hasInitialized = true;
         finishedDataSetup.Invoke();
      }
   }

   public ShopData getShopDataByName (string name) {
      return _shopData.Values.ToList().Find(_=>_.shopName == name);
   }

   public ShopData getShopDataById (int shopId) {
      return _shopData[shopId];
   }

   public ShopData getShopDataByArea (string area) {
      ShopData returnData = shopDataList.Find(_ => _.areaAttachment == area);
      if (returnData == null) {
         return new ShopData { shopGreetingText = "Greeting Text Not Set" };
      }
      return returnData;
   }

   #region Private Variables

   // The cached data 
   private Dictionary<int, ShopData> _shopData = new Dictionary<int, ShopData>();

   #endregion
}
