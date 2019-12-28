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

   public CraftableItemRequirements getItem(Item.Category itemCategory, int itemType) {
      return _craftingData[itemCategory.ToString()];
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
               string keyName = craftingData.resultItem.category == Item.Category.None ? "Undefined" : craftingData.resultItem.category + "_" + craftingData.resultItem.itemTypeId;
               
               // Save the Crafting data in the memory cache
               if (!_craftingData.ContainsKey(keyName)) {
                  _craftingData.Add(keyName, craftingData);
                  craftingDataList.Add(craftingData);
               } else {
                  D.warning("Key already exists: " + keyName);
               }

               RewardManager.self.craftableDataList = getAllCraftableData();
            }
         });
      });
   }

   #region Private Variables

   // The cached crafting data 
   private Dictionary<string, CraftableItemRequirements> _craftingData = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
