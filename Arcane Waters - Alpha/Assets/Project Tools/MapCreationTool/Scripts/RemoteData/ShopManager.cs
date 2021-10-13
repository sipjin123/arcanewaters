using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;

namespace MapCreationTool
{
   public class ShopManager : MonoBehaviour
   {
      #region Public Variables

      // Action indicating if loading is done
      public static event System.Action OnLoaded;

      // Self
      public static ShopManager instance { get; private set; }

      // Shop data cache
      public Dictionary<int, ShopData> shopDataCollection { get; private set; }

      // Pvp Shop data cache
      public Dictionary<int, PvpShopData> pvpShopDataCollection { get; private set; }

      // Finished loading
      public bool loaded { get; private set; }

      #endregion

      private void Awake () {
         instance = this;
      }

      private IEnumerator Start () {
         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllShop();
      }

      public SelectOption[] formSelectionOptions () {
         return _shopDataArray.Select(n => new SelectOption(n.shopId.ToString(), n.shopName)).ToArray();
      }

      public SelectOption[] formPvpShopSelectionOptions () {
         return _pvpShopDataArray.Select(n => new SelectOption(n.shopId.ToString(), n.shopName)).ToArray();
      }

      private void loadAllShop () {
         shopDataCollection = new Dictionary<int, ShopData>();
         pvpShopDataCollection = new Dictionary<int, PvpShopData>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getShopXML();
            List<XMLPair> rawPvpXMLData = DB_Main.getPvpShopXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               setShopData(rawXMLData);
               setPvpShopData(rawPvpXMLData);
            });
         });
      }

      private void setShopData (List<XMLPair> xmlPairGroup) {
         try {
            foreach (XMLPair xmlPair in xmlPairGroup) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ShopData shopData = Util.xmlLoad<ShopData>(newTextAsset);
               shopData.shopId = xmlPair.xmlId;
               if (shopData == null) {
                  Utilities.warning($"Failed to load shopData");
                  continue;
               }

               // Save the Shop data in the memory cache
               if (!shopDataCollection.ContainsKey(xmlPair.xmlId)) {
                  shopDataCollection.Add(xmlPair.xmlId, shopData);
               }
            }

            _shopDataArray = shopDataCollection.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         } catch (Exception ex) {
            Utilities.warning("Failed to load shop manager. Exception:\n" + ex);
            UI.messagePanel.displayError("Failed to load shop manager. Exception:\n" + ex);
         }

         loaded = true;
         OnLoaded?.Invoke();
      }

      private void setPvpShopData (List<XMLPair> xmlPairGroup) {
         try {
            foreach (XMLPair xmlPair in xmlPairGroup) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               PvpShopData shopData = Util.xmlLoad<PvpShopData>(newTextAsset);
               shopData.shopId = xmlPair.xmlId;
               if (shopData == null) {
                  Utilities.warning($"Failed to load shopData");
                  continue;
               }

               // Save the Shop data in the memory cache
               if (!pvpShopDataCollection.ContainsKey(xmlPair.xmlId)) {
                  pvpShopDataCollection.Add(xmlPair.xmlId, shopData);
               }
            }

            _pvpShopDataArray = pvpShopDataCollection.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         } catch (Exception ex) {
            Utilities.warning("Failed to load shop manager. Exception:\n" + ex);
            UI.messagePanel.displayError("Failed to load shop manager. Exception:\n" + ex);
         }

         loaded = true;
         OnLoaded?.Invoke();
      }

      public int shopEntryCount
      {
         get { return _shopDataArray.Length; }
      }

      public int pvpShopEntryCount
      {
         get { return _pvpShopDataArray.Length; }
      }

      #region Private Variables

      // Array of shop data loaded
      private ShopData[] _shopDataArray = new ShopData[0];

      // Array of pvp shop data loaded
      private PvpShopData[] _pvpShopDataArray = new PvpShopData[0];

      #endregion
   }
}