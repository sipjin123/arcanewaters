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
      public Dictionary<string, ShopData> shopDataCollection { get; private set; }

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

      public string[] formSelectionOptions () {
         return _shopDataArray.Select(n => n.shopName).ToArray();
      }

      private void loadAllShop () {
         shopDataCollection = new Dictionary<string, ShopData>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<string> rawXMLData = DB_Main.getShopXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               setData(rawXMLData);
            });
         });
      }

      private void setData (List<string> rawXMLData) {
         try {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               ShopData shopData = Util.xmlLoad<ShopData>(newTextAsset);
               if (shopData == null) {
                  Utilities.warning($"Failed to load shopData");
                  continue;
               }

               // Save the Shop data in the memory cache
               if (!shopDataCollection.ContainsKey(shopData.shopName)) {
                  shopDataCollection.Add(shopData.shopName, shopData);
               }
            }

            _shopDataArray = shopDataCollection.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         } catch (Exception ex) {
            Utilities.warning("Failed to load shop manager. Exception:\n" + ex);
            UI.errorDialog.display("Failed to load shop manager. Exception:\n" + ex);
         }

         loaded = true;
         OnLoaded?.Invoke();
      }

      public int shopEntryCount
      {
         get { return _shopDataArray.Length; }
      }

      #region Private Variables

      // Array of shop data loaded
      private ShopData[] _shopDataArray = new ShopData[0];

      #endregion
   }
}