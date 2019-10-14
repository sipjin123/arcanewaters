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

   #endregion

   public void Awake () {
      self = this;
   }

   private void Start () {
      initializeQuestCache();
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

   public void initializeQuestCache () {
      // Iterate over the files
      foreach (TextAsset textAsset in craftingDataAssets) {
         // Read and deserialize the file
         CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(textAsset);

         // Save the Crafting data in the memory cache
         _craftingData.Add(craftingData.resultItem.category == Item.Category.None ? "Undefined" : craftingData.resultItem.getCastItem().getName(), craftingData);
      }
   }

   #region Private Variables

   // The cached crafting data 
   private Dictionary<string, CraftableItemRequirements> _craftingData = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
