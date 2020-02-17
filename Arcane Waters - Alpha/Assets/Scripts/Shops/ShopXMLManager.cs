using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

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
            List<string> rawXMLData = DB_Main.getShopXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (string rawText in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(rawText);
                  ShopData shopData = Util.xmlLoad<ShopData>(newTextAsset);

                  if (!_shopData.ContainsKey(shopData.shopName)) {
                     _shopData.Add(shopData.shopName, shopData);
                     shopDataList.Add(shopData);
                  }
               }
               hasInitialized = true;
               finishedDataSetup.Invoke();
            });
         });
      }
   }

   public void receiveDataFromServer (ShopData[] dataCollection) {
      if (!hasInitialized) {
         shopDataList = new List<ShopData>();
         foreach (ShopData data in dataCollection) {
            if (!_shopData.ContainsKey(data.shopName)) {
               _shopData.Add(data.shopName, data);
               shopDataList.Add(data);
            }
         }
         hasInitialized = true;
         finishedDataSetup.Invoke();
      }
   }

   public ShopData getShopDataByName (string name) {
      return _shopData[name];
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
   private Dictionary<string, ShopData> _shopData = new Dictionary<string, ShopData>();

   #endregion
}
