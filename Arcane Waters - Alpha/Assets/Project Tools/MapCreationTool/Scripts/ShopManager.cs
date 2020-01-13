using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

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

      // Array of shop data loaded
      private ShopData[] shopDataArray;

      #endregion

      private void Awake () {
         instance = this;
      }

      private IEnumerator Start () {
         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllShop();
      }

      public string[] formSelectionOptions () {
         return shopDataArray.Select(n => n.shopName).ToArray();
      }

      private void loadAllShop () {
         shopDataCollection = new Dictionary<string, ShopData>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<string> rawXMLData = DB_Main.getShopXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (string rawText in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(rawText);
                  ShopData shopData = Util.xmlLoad<ShopData>(newTextAsset);

                  // Save the Shop data in the memory cache
                  if (!shopDataCollection.ContainsKey(shopData.shopName)) {
                     shopDataCollection.Add(shopData.shopName, shopData);
                  }
               }

               instance.finishLoadingShop();
            });
         });
      }

      public int shopEntryCount
      {
         get { return shopDataArray.Length; }
      }

      private void finishLoadingShop () {
         shopDataArray = shopDataCollection.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         loaded = true;

         OnLoaded?.Invoke();
      }

      #region Private Variables

      #endregion
   }
}