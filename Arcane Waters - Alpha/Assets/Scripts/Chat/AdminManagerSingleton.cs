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

   public int totalMessagesIn;
   public int totalMessagesOut;

   public float totalInSize;
   public float totalOutObservers;
   public float totalOutSize;

   public float greatestNumberOfObserversOut;

   public float largestSizeMessageIn;
   public float largestSizeMessageOut;
   public float largestSizeMessage;
   public float averageMessagesPerTick;
   public int beginningTick;
   public int endingTick;
   public int totalTicks;
   public float beginningTime;
   public float endingTime;
   public int begginingFrame;
   public int endingFrame;
   public int previousBeginningTick;
   public float previousBeginningTime;
   public int previousBegginingFrame;

   public float TESTDURATION = 5.0F;

   #endregion

   IEnumerator CO_RecordEndingTestNumbers () {
      // Store values at the beginging of duration
      beginningTick = networkProfiler.ticks.Count;
      beginningTime = networkProfiler.CurrentTick().time;
      begginingFrame = networkProfiler.CurrentTick().frameCount;

      yield return new WaitForSeconds(TESTDURATION);

      // Store values at the end of duration
      endingTick = networkProfiler.ticks.Count;
      endingTime = networkProfiler.CurrentTick().time;
      endingFrame = networkProfiler.CurrentTick().frameCount;

      // Calculations
      totalTicks = endingTick - beginningTick;
      averageMessagesPerTick = totalMessagesIn + totalMessagesOut / (float) totalTicks;

      // Print to log
      string statHeader = $"************  Message stats for {TESTDURATION} second duration  ************";
      D.adminLog(statHeader, D.ADMIN_LOG_TYPE.NetworkMessages);
      D.adminLog("Total incoming messages = " + totalMessagesIn, D.ADMIN_LOG_TYPE.NetworkMessages);
      D.adminLog("Total out going messages = " + totalMessagesOut, D.ADMIN_LOG_TYPE.NetworkMessages);
      D.adminLog("Total number of ticks = " + totalTicks, D.ADMIN_LOG_TYPE.NetworkMessages);
      if (totalTicks > 0) {
         D.adminLog("Average messages per tick = " + (float)(((float)totalMessagesIn + totalMessagesOut) / totalTicks), D.ADMIN_LOG_TYPE.NetworkMessages);
      }
      if (totalMessagesOut > 0) {
         D.adminLog("Average numbers of observers(clients) per message = " + totalOutObservers / totalMessagesOut, D.ADMIN_LOG_TYPE.NetworkMessages);
      }
      if (totalMessagesIn + totalMessagesOut > 0) {
         D.adminLog("Average size of messages = " + ((totalInSize + totalOutSize) / (totalMessagesIn + totalMessagesOut)) + " bytes", D.ADMIN_LOG_TYPE.NetworkMessages);
      }
      D.adminLog("Largest message in bytes = " + (largestSizeMessage = largestSizeMessageIn > largestSizeMessageOut ? largestSizeMessageIn : largestSizeMessageOut), D.ADMIN_LOG_TYPE.NetworkMessages);

      // Reset the values
      totalMessagesIn = 0;
      totalMessagesOut = 0;
      totalInSize = 0;
      totalOutObservers = 0;
      totalOutSize = 0;
      greatestNumberOfObserversOut = 0;
      largestSizeMessageIn = 0;
      largestSizeMessageOut = 0;
      largestSizeMessage = 0;
   }

   protected override void Awake () {
      base.Awake();
      self = this;

      // Listen to message events
      NetworkDiagnostics.InMessageEvent += NetworkDiagnostics_InMessageEvent;
      NetworkDiagnostics.OutMessageEvent += NetworkDiagnostics_OutMessageEvent;
   }

   private void NetworkDiagnostics_InMessageEvent (NetworkDiagnostics.MessageInfo obj) {
      totalMessagesIn += 1;
      totalInSize += obj.bytes;
      largestSizeMessageIn = obj.bytes > largestSizeMessageIn ? obj.bytes : largestSizeMessageIn;
   }

   private void NetworkDiagnostics_OutMessageEvent (NetworkDiagnostics.MessageInfo obj) {
      totalMessagesOut += 1;
      totalOutSize += obj.bytes;
      largestSizeMessageOut = obj.bytes > largestSizeMessageOut ? obj.bytes : largestSizeMessageOut;
      totalOutObservers += obj.count;
      greatestNumberOfObserversOut = obj.count > greatestNumberOfObserversOut ? obj.count : greatestNumberOfObserversOut;
   }

   public void Start () {
#if IS_SERVER_BUILD
      networkProfiler.MaxTicks = int.MaxValue;
      networkProfiler.IsRecording = true;
#endif
      if (CommandCodes.get(CommandCodes.Type.AUTO_TEST)) {
         StartCoroutine(nameof(CO_RecordEndingTestNumbers));
      }

      if (Util.isServerNonHost()) {
         return;
      }

      // Request the list of blueprints from the server
      if (blueprintNames.Count == 0) {
         // TODO: Insert fetch blueprint data here
      }

      StartCoroutine(CO_CreateItemNamesDictionary());
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
