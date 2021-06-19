using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Mirror.Profiler;
using MapCreationTool.Serialization;

public class AdminManagerSingleton : GenericGameManager
{
   #region Public Variables

   // The dictionary of item ids accessible by in-game item name
   public Dictionary<string, int> weaponNames = new Dictionary<string, int>();
   public Dictionary<string, int> armorNames = new Dictionary<string, int>();
   public Dictionary<string, int> hatNames = new Dictionary<string, int>();
   public Dictionary<string, int> usableNames = new Dictionary<string, int>();
   public Dictionary<string, int> craftingIngredientNames = new Dictionary<string, int>();

   // The dictionary of blueprint names
   public Dictionary<string, int> blueprintNames = new Dictionary<string, int>();

   // The network profiler
   public readonly NetworkProfiler networkProfiler = new NetworkProfiler(60 * 60); // 60 seconds

   // Self
   public static AdminManagerSingleton self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
#if IS_SERVER_BUILD
      networkProfiler.MaxTicks = int.MaxValue;
      networkProfiler.IsRecording = true;
#endif

      InvokeRepeating("summarizeData", 0.0f, 5.0f);

      if (Util.isServerNonHost()) {
         return;
      }

      // Request the list of blueprints from the server
      if (blueprintNames.Count == 0) {
         // TODO: Insert fetch blueprint data here
      }

      StartCoroutine(CO_CreateItemNamesDictionary());
   }

   public void Update () {
      tickTotal += 1;
   }

   private IEnumerator CO_CreateItemNamesDictionary () {
      while (!EquipmentXMLManager.self.loadedAllEquipment) {
         yield return null;
      }

      buildItemNamesDictionary();
   }

   public void buildItemNamesDictionary () {
      // Clear all the dictionaries
      weaponNames.Clear();
      armorNames.Clear();
      hatNames.Clear();
      usableNames.Clear();
      craftingIngredientNames.Clear();

      // Set all the weapon names
      foreach (WeaponStatData weaponData in EquipmentXMLManager.self.weaponStatList) {
         addToItemNameDictionary(weaponNames, Item.Category.Weapon, weaponData.sqlId, weaponData.equipmentName);
      }

      // Set all the armor names
      foreach (ArmorStatData armorData in EquipmentXMLManager.self.armorStatList) {
         addToItemNameDictionary(armorNames, Item.Category.Armor, armorData.sqlId, armorData.equipmentName);
      }

      // Set all the hat names
      foreach (HatStatData hatData in EquipmentXMLManager.self.hatStatList) {
         addToItemNameDictionary(hatNames, Item.Category.Hats, hatData.sqlId, hatData.equipmentName);
      }

      // Set all the usable items names
      foreach (UsableItem.Type usableType in Enum.GetValues(typeof(UsableItem.Type))) {
         addToItemNameDictionary(usableNames, Item.Category.Usable, (int) usableType);
      }

      // Set all the crafting ingredients names
      foreach (CraftingIngredients.Type craftingIngredientsType in Enum.GetValues(typeof(CraftingIngredients.Type))) {
         addToItemNameDictionary(craftingIngredientNames, Item.Category.CraftingIngredients, (int) craftingIngredientsType);
      }
   }

   private void addToItemNameDictionary (Dictionary<string, int> dictionary, Item.Category category, int itemTypeId) {
      // Create a base item
      Item baseItem = new Item(-1, category, itemTypeId, 1, "", "", Item.MAX_DURABILITY);
      baseItem = baseItem.getCastItem();

      addToItemNameDictionary(dictionary, category, itemTypeId, baseItem.getName());
   }

   private void addToItemNameDictionary (Dictionary<string, int> dictionary, Item.Category category, int itemTypeId, string itemName) {
      // Get the item name in lower case
      itemName = itemName.ToLower();

      // Add the new entry in the dictionary
      if (!"undefined".Equals(itemName) && !"usable item".Equals(itemName) && !"undefined design".Equals(itemName) && !itemName.ToLower().Contains("none") && itemName != "") {
         if (!dictionary.ContainsKey(itemName)) {
            dictionary.Add(itemName, itemTypeId);
         } else {
            D.warning(string.Format("The {0} item name ({1}) is duplicated.", category.ToString(), itemName));
         }
      }
   }

   #region Private Variables

   #endregion
}
