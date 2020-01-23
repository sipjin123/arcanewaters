using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class CraftingManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static CraftingManager self;

   // For editor preview of data
   public List<CraftableItemRequirements> craftingDataList = new List<CraftableItemRequirements>();

   #endregion

   private void Awake () {
      self = this;
   }

   public CraftableItemRequirements getCraftableData(Item.Category itemCategory, int itemTypeId) {
      string key = getKey(itemCategory, itemTypeId);
      if (_craftingData.ContainsKey(key)) {
         return _craftingData[key];
      } else {
         return null;
      }
   }

   public List<CraftableItemRequirements> getAllCraftableData () {
      List<CraftableItemRequirements> craftableList = new List<CraftableItemRequirements>();
      foreach(KeyValuePair<string, CraftableItemRequirements> item in _craftingData) {
         craftableList.Add(item.Value);
      }
      return craftableList;
   }

   public void initializeDataCache () {
      _craftingData = new Dictionary<string, CraftableItemRequirements>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getCraftingXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset); 
               string keyName = getKey(craftingData.resultItem.category, craftingData.resultItem.itemTypeId);
               
               // Save the Crafting data in the memory cache
               if (!_craftingData.ContainsKey(keyName)) {
                  _craftingData.Add(keyName, craftingData);
                  craftingDataList.Add(craftingData);
               } else {
                  D.warning("Key already exists: " + keyName);
               }
            }
         });
      });
   }

   private string getKey (Item.Category category, int itemTypeId) {
      return category == Item.Category.None ? "Undefined" : category + "_" + itemTypeId;
   }

   #region Private Variables

   // The cached crafting data 
   private Dictionary<string, CraftableItemRequirements> _craftingData = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
