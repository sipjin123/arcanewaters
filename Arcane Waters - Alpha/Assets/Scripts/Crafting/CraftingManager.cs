using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Linq;

public class CraftingManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static CraftingManager self;

   // For editor preview of data
   public List<CraftableItemRequirements> craftingDataList = new List<CraftableItemRequirements>();
   public List<RefinementData> refinementDataList = new List<RefinementData>();

   // The xml id of the bone sword craftable item
   public const int BONE_SWORD_RECIPE = 78;

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

   public RefinementData getRefinementData (int xmlId) {
      if (_refinementData.ContainsKey(xmlId)) {
         return _refinementData[xmlId];
      } else {
         D.debug("No existing Refinement found: " + xmlId);
         return null;
      }
   }

   public CraftableItemRequirements getCraftableData (int xmlId) {
      return _craftingData.Values.ToList().Find(_ => _.xmlId == xmlId);
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
         List<XMLPair> rawXMLData = DB_Main.getCraftingXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlData in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlData.rawXmlData);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset);
               craftingData.xmlId = xmlData.xmlId;
               string keyName = getKey(craftingData.resultItem.category, craftingData.resultItem.itemTypeId);

               if (craftingData.isEnabled) {
                  // Save the Crafting data in the memory cache
                  if (!_craftingData.ContainsKey(keyName)) {
                     CraftableItemRequirements newCraftData = overrideCraftingData(craftingData);
                     _craftingData.Add(keyName, newCraftData);
                     craftingDataList.Add(newCraftData);
                  }
               }
            }
         });
      });
   }

   public CraftableItemRequirements overrideCraftingData (CraftableItemRequirements craftingData) {
      if (craftingData.resultItem.category == Item.Category.Weapon && craftingData.resultItem.data.Length < 1) {
         craftingData.resultItem.data = Blueprint.WEAPON_DATA_PREFIX;
         D.debug("Investigate Weapon {" + craftingData.xmlId + "} crafting data for {" + craftingData.resultItem.itemTypeId + "}");
      }
      if (craftingData.resultItem.category == Item.Category.Armor && craftingData.resultItem.data.Length < 1) {
         craftingData.resultItem.data = Blueprint.ARMOR_DATA_PREFIX;
         D.debug("Investigate Armor {" + craftingData.xmlId + "} crafting data for {" + craftingData.resultItem.itemTypeId + "}");
      }
      if (craftingData.resultItem.category == Item.Category.Hats && craftingData.resultItem.data.Length < 1) {
         craftingData.resultItem.data = Blueprint.HAT_DATA_PREFIX;
         D.debug("Investigate Hat {" + craftingData.xmlId + "} crafting data for {" + craftingData.resultItem.itemTypeId + "}");
      }
      if (craftingData.resultItem.category == Item.Category.Ring && craftingData.resultItem.data.Length < 1) {
         craftingData.resultItem.data = Blueprint.RING_DATA_PREFIX;
         D.debug("Investigate Ring {" + craftingData.xmlId + "} crafting data for {" + craftingData.resultItem.itemTypeId + "}");
      }
      if (craftingData.resultItem.category == Item.Category.Necklace && craftingData.resultItem.data.Length < 1) {
         craftingData.resultItem.data = Blueprint.NECKLACE_DATA_PREFIX;
         D.debug("Investigate Necklace {" + craftingData.xmlId + "} crafting data for {" + craftingData.resultItem.itemTypeId + "}");
      }
      if (craftingData.resultItem.category == Item.Category.Trinket && craftingData.resultItem.data.Length < 1) {
         craftingData.resultItem.data = Blueprint.TRINKET_DATA_PREFIX;
         D.debug("Investigate Trinket {" + craftingData.xmlId + "} crafting data for {" + craftingData.resultItem.itemTypeId + "}");
      }

      return craftingData;
   }

   public void receiveZipData (Dictionary<string, CraftableItemRequirements> zipData) {
      _craftingData = zipData;
      foreach (KeyValuePair<string, CraftableItemRequirements> data in zipData) {
         craftingDataList.Add(data.Value);
      }
   }

   public static string getKey (Item.Category category, int itemTypeId) {
      return category == Item.Category.None ? "Undefined" : category + "_" + itemTypeId;
   }

   #region Private Variables

   // The cached crafting data 
   private Dictionary<string, CraftableItemRequirements> _craftingData = new Dictionary<string, CraftableItemRequirements>();

   // The cached refinement data 
   private Dictionary<int, RefinementData> _refinementData = new Dictionary<int, RefinementData>();

   #endregion
}
