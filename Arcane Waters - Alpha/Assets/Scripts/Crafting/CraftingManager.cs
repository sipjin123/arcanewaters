using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftingManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static CraftingManager self;

   // The files containing the crafting data
   public TextAsset[] craftingDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   #endregion

   public void Awake () {
      self = this;

#if IS_SERVER_BUILD 
      initializeCraftCache();
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

   private void initializeCraftCache () {
      if (!hasInitialized) {
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in craftingDataAssets) {
            // Read and deserialize the file
            CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(textAsset);

            // Save the Crafting data in the memory cache
            _craftingData.Add(craftingData.resultItem.category == Item.Category.None ? "Undefined" : craftingData.resultItem.getCastItem().getName(), craftingData);
         }

         RewardManager.self.craftableDataList = getAllCraftableData();
      }
   }

   #region Private Variables

   // The cached crafting data 
   private Dictionary<string, CraftableItemRequirements> _craftingData = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
