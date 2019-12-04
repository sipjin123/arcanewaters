using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class CraftingManager : XmlManager {
   #region Public Variables

   // Self
   public static CraftingManager self;

   #endregion

   public void Awake () {
      self = this;

#if IS_SERVER_BUILD 
      translateXMLData();
#endif
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

   private void translateXMLData () {
      _craftingData = new Dictionary<string, CraftableItemRequirements>();
      // Iterate over the files
      foreach (TextAsset textAsset in textAssets) {
         // Read and deserialize the file
         CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(textAsset);

         // Save the Crafting data in the memory cache
         string keyName = craftingData.resultItem.category == Item.Category.None ? "Undefined" : craftingData.resultItem.getCastItem().getName();
         if (_craftingData.ContainsKey(keyName)) {
            _craftingData.Add(keyName, craftingData);
         } else {
            D.warning("Key already exists: " + keyName);
         }
      }

      RewardManager.self.craftableDataList = getAllCraftableData();
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(CraftingToolManager.FOLDER_PATH);
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #region Private Variables

   // The cached crafting data 
   private Dictionary<string, CraftableItemRequirements> _craftingData = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
