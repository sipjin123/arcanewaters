using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.IO;
using static EquipmentToolManager;
using UnityEngine.Events;
using System;

public class EquipmentXMLManager : MonoBehaviour {
   #region Public Variables

   // A convenient self reference
   public static EquipmentXMLManager self;

   // References to all the weapon data
   public List<WeaponStatData> weaponStatList { get { return _weaponStatRegistry.Values.ToList(); } }

   // References to all the armor data
   public List<ArmorStatData> armorStatList { get { return _armorStatRegistry.Values.ToList(); } }

   // References to all the hat data
   public List<HatStatData> hatStatList { get { return _hatStatRegistry.Values.ToList(); } }

   // References to all the gear data
   public List<RingStatData> ringStatList { get { return _ringStatRegistry.Values.ToList(); } }
   public List<NecklaceStatData> necklaceStatList { get { return _necklaceStatRegistry.Values.ToList(); } }
   public List<TrinketStatData> trinketStatList { get { return _trinketStatRegistry.Values.ToList(); } }

   // Reference to all the quest item data
   public List<QuestItem> questItemList { get { return _questItemList.ToList(); } }

   // Determines how many equipment set has been loaded
   public int equipmentLoadCounter = 0;

   // Determines if all equipment is loaded
   public bool loadedAllEquipment;

   // The valid starting format of an xml data
   public const string VALID_XML_FORMAT = "<?xml version=";

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   // The number of equipment types that needs to be set {Weapon/Armor/Hat}
   public const int TOTAL_EQUIPMENT_TYPES = 3;

   // The blank icons if none equipped
   public Sprite blankWeaponIcon, blankArmorIcon, blankHatIcon, blankRingIcon, blankNecklaceIcon, blankTrinketIcon;

   #endregion

   private void Awake () {
      _weaponStatRegistry = new Dictionary<int, WeaponStatData>();
      _armorStatRegistry = new Dictionary<int, ArmorStatData>();
      _hatStatRegistry = new Dictionary<int, HatStatData>();
      _ringStatRegistry = new Dictionary<int, RingStatData>();
      _necklaceStatRegistry = new Dictionary<int, NecklaceStatData>();
      _trinketStatRegistry = new Dictionary<int, TrinketStatData>();
      self = this;
   }

   #region Get Data

   public WeaponStatData getWeaponData (int weaponSqlId) {
      if (_weaponStatRegistry.ContainsKey(weaponSqlId)) {
         WeaponStatData weaponStatData = _weaponStatRegistry[weaponSqlId];
         if (String.IsNullOrEmpty(weaponStatData.equipmentIconPath)) {
            D.debug("Null Icon for Weapon!: {" + weaponStatData.equipmentName + "} {" + weaponStatData.equipmentIconPath + "} {" + weaponStatData.weaponType + "}");
         }
         return weaponStatData;
      }

      return null;
   }

   public WeaponStatData getWeaponDataByItemType (int weaponType) {
      WeaponStatData weaponDataFetched = _weaponStatRegistry.Values.ToList().Find(_ => _.weaponType == weaponType);
      
      if (weaponDataFetched != null) {
         return weaponDataFetched;
      }

      return null;
   }

   public ArmorStatData getArmorDataBySqlId (int armorSqlId) {
      if (_armorStatRegistry.ContainsKey(armorSqlId)) {
         ArmorStatData armorStatData = _armorStatRegistry[armorSqlId];
         if (String.IsNullOrEmpty(armorStatData.equipmentIconPath)) {
            D.debug("Null Icon for Armor!: {" + armorStatData.equipmentName + "} {" + armorStatData.equipmentIconPath + "} {" + armorStatData.armorType + "}");
         }
         return armorStatData;
      }

      return null;
   }

   public HatStatData getHatData (int hatSqlId) {
      if (_hatStatRegistry.ContainsKey(hatSqlId)) {
         HatStatData hatStatData = _hatStatRegistry[hatSqlId];

         if (String.IsNullOrEmpty(hatStatData.equipmentIconPath)) {
            D.debug("Null Icon for Hat!: {" + hatStatData.equipmentName + "} {" + hatStatData.equipmentIconPath + "} {" + hatStatData.hatType + "}");
         }

         return hatStatData;
      }

      return null;
   }

   public RingStatData getRingData (int newSqlId) {
      if (_ringStatRegistry.ContainsKey(newSqlId)) {
         RingStatData newStatData = _ringStatRegistry[newSqlId];

         if (String.IsNullOrEmpty(newStatData.equipmentIconPath)) {
            D.debug("Null Icon for Ring!: {" + newStatData.equipmentName + "} {" + newStatData.equipmentIconPath + "} {" + newStatData.ringType + "}");
         }

         return newStatData;
      }

      return null;
   }

   public NecklaceStatData getNecklaceData (int newSqlId) {
      if (_necklaceStatRegistry.ContainsKey(newSqlId)) {
         NecklaceStatData newStatData = _necklaceStatRegistry[newSqlId];

         if (String.IsNullOrEmpty(newStatData.equipmentIconPath)) {
            D.debug("Null Icon for Necklace: {" + newStatData.equipmentName + "} {" + newStatData.equipmentIconPath + "} {" + newStatData.necklaceType + "}");
         }

         return newStatData;
      }

      return null;
   }

   public TrinketStatData getTrinketData (int newSqlId) {
      if (_trinketStatRegistry.ContainsKey(newSqlId)) {
         TrinketStatData newStatData = _trinketStatRegistry[newSqlId];

         if (String.IsNullOrEmpty(newStatData.equipmentIconPath)) {
            D.debug("Null Icon for Trinket: {" + newStatData.equipmentName + "} {" + newStatData.equipmentIconPath + "} {" + newStatData.trinketType + "}");
         }

         return newStatData;
      }

      return null;
   }

   public HatStatData getHatDataByItemType (int hatType) {
      return _hatStatRegistry.Values.FirstOrDefault(_ => _.hatType == hatType);
   }

   public QuestItem getQuestItemById (int questItemId) {
      QuestItem questItem = questItemList.Find(_ => _.itemTypeId == questItemId);
      if (questItem != null) {

         if (String.IsNullOrEmpty(questItem.iconPath)) {
            D.debug("Null Icon for QuestItem!: {" + questItem.itemName + "} {" + questItem.iconPath + "} {" + questItem.itemTypeId + "}");
         }
         return questItem;
      }

      return null;
   }

   #endregion

   private void finishedLoading () {
      equipmentLoadCounter++;
      if (equipmentLoadCounter >= TOTAL_EQUIPMENT_TYPES) {
         loadedAllEquipment = true;
         finishedDataSetup.Invoke();
      }
   }

   public void initializeDataCache () {
      equipmentLoadCounter = 0;
      _weaponStatRegistry = new Dictionary<int, WeaponStatData>();
      _armorStatRegistry = new Dictionary<int, ArmorStatData>();
      _hatStatRegistry = new Dictionary<int, HatStatData>();
      _ringStatRegistry = new Dictionary<int, RingStatData>();
      _necklaceStatRegistry = new Dictionary<int, NecklaceStatData>();
      _trinketStatRegistry = new Dictionary<int, TrinketStatData>();

      _weaponStatList = new List<WeaponStatData>();
      _armorStatList = new List<ArmorStatData>();
      _hatStatList = new List<HatStatData>();
      _ringStatList = new List<RingStatData>();
      _necklaceStatList = new List<NecklaceStatData>();
      _trinketStatList = new List<TrinketStatData>();

      List<XMLPair> rawQuestItemXml = DB_Main.getQuestItemtXML();
      foreach (XMLPair xmlPair in rawQuestItemXml) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               Item rawData = Util.xmlLoad<Item>(newTextAsset);
               rawData.itemTypeId = xmlPair.xmlId;

               QuestItem newItem = new QuestItem {
                  category = Item.Category.Quest_Item,
                  itemTypeId = rawData.itemTypeId,
                  itemName = rawData.itemName,
                  itemDescription = rawData.itemDescription,
                  paletteNames = rawData.paletteNames,
                  durability = 100,
                  count = rawData.count,
                  data = rawData.data,
                  iconPath = rawData.iconPath,
                  id = -1
               };

               // Save the data in the memory cache
               QuestItem currQuestItem = _questItemList.Find(_ => _.itemTypeId == xmlPair.xmlId);
               if (currQuestItem == null && xmlPair.isEnabled) {
                  _questItemList.Add(newItem);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }

      List<XMLPair> rawWeaponXml = DB_Main.getEquipmentXML(EquipmentType.Weapon);
      foreach (XMLPair xmlPair in rawWeaponXml) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(newTextAsset);
               rawData.sqlId = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_weaponStatRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _weaponStatRegistry.Add(xmlPair.xmlId, rawData);
                  _weaponStatList.Add(rawData);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }

      List<XMLPair> rawArmorXml = DB_Main.getEquipmentXML(EquipmentType.Armor);
      foreach (XMLPair xmlPair in rawArmorXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            ArmorStatData rawData = Util.xmlLoad<ArmorStatData>(newTextAsset);
            rawData.sqlId = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!_armorStatRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
               _armorStatRegistry.Add(xmlPair.xmlId, rawData);
               _armorStatList.Add(rawData);
            }
         }
      }

      List<XMLPair> rawHatXml = DB_Main.getEquipmentXML(EquipmentType.Hat);
      foreach (XMLPair xmlPair in rawHatXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            HatStatData rawData = Util.xmlLoad<HatStatData>(newTextAsset);
            int uniqueID = xmlPair.xmlId;
            rawData.sqlId = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!_hatStatRegistry.ContainsKey(uniqueID) && xmlPair.isEnabled) {
               _hatStatRegistry.Add(uniqueID, rawData);
               _hatStatList.Add(rawData);
            }
         }
      }

      List<XMLPair> rawRingXml = DB_Main.getEquipmentXML(EquipmentType.Ring);
      foreach (XMLPair xmlPair in rawRingXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            RingStatData rawData = Util.xmlLoad<RingStatData>(newTextAsset);
            int uniqueID = xmlPair.xmlId;
            rawData.sqlId = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!_ringStatRegistry.ContainsKey(uniqueID) && xmlPair.isEnabled) {
               _ringStatRegistry.Add(uniqueID, rawData);
               _ringStatList.Add(rawData);
            }
         }
      }
      List<XMLPair> rawNecklaceXml = DB_Main.getEquipmentXML(EquipmentType.Necklace);
      foreach (XMLPair xmlPair in rawNecklaceXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            NecklaceStatData rawData = Util.xmlLoad<NecklaceStatData>(newTextAsset);
            int uniqueID = xmlPair.xmlId;
            rawData.sqlId = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!_necklaceStatRegistry.ContainsKey(uniqueID) && xmlPair.isEnabled) {
               _necklaceStatRegistry.Add(uniqueID, rawData);
               _necklaceStatList.Add(rawData);
            }
         }
      }
      List<XMLPair> rawTrinketXml = DB_Main.getEquipmentXML(EquipmentType.Trinket);
      foreach (XMLPair xmlPair in rawTrinketXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            TrinketStatData rawData = Util.xmlLoad<TrinketStatData>(newTextAsset);
            int uniqueID = xmlPair.xmlId;
            rawData.sqlId = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!_trinketStatRegistry.ContainsKey(uniqueID) && xmlPair.isEnabled) {
               _trinketStatRegistry.Add(uniqueID, rawData);
               _trinketStatList.Add(rawData);
            }
         }
      }
      equipmentLoadCounter = TOTAL_EQUIPMENT_TYPES;
      finishedLoading();
   }

   public void receiveQuestDataFromZipData (List<Item> questData) {
      foreach (Item rawData in questData) {
         // Save the data in the memory cache
         if (!_questItemList.Exists(_=>_.itemTypeId == rawData.itemTypeId)) {
            QuestItem newItem = new QuestItem {
               category = Item.Category.Quest_Item,
               itemTypeId = rawData.itemTypeId,
               itemName = rawData.itemName,
               itemDescription = rawData.itemDescription,
               paletteNames = rawData.paletteNames,
               durability = 100,
               count = rawData.count,
               data = rawData.data,
               iconPath = rawData.iconPath,
               id = -1
            };
            _questItemList.Add(newItem);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _questItemList.Count + "} quest item data from {" + questData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveRingDataFromZipData (List<RingStatData> statData) {
      foreach (RingStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_ringStatRegistry.ContainsKey(uniqueID)) {
            _ringStatRegistry.Add(uniqueID, rawData);
            _ringStatList.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _ringStatRegistry.Count + "} ring data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveNecklaceDataFromZipData (List<NecklaceStatData> statData) {
      foreach (NecklaceStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_necklaceStatRegistry.ContainsKey(uniqueID)) {
            _necklaceStatRegistry.Add(uniqueID, rawData);
            _necklaceStatList.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _necklaceStatRegistry.Count + "} necklace data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveTrinketDataFromZipData (List<TrinketStatData> statData) {
      foreach (TrinketStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_trinketStatRegistry.ContainsKey(uniqueID)) {
            _trinketStatRegistry.Add(uniqueID, rawData);
            _trinketStatList.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _trinketStatRegistry.Count + "} trinket data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveWeaponDataFromZipData (List<WeaponStatData> statData) {
      foreach (WeaponStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_weaponStatRegistry.ContainsKey(uniqueID)) {
            _weaponStatRegistry.Add(uniqueID, rawData);
            _weaponStatList.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _weaponStatRegistry.Count + "} weapon data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveArmorDataFromZipData (List<ArmorStatData> statData) {
      foreach (ArmorStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_armorStatRegistry.ContainsKey(uniqueID)) {
            _armorStatRegistry.Add(uniqueID, rawData);
            _armorStatList.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _armorStatRegistry.Count + "} armor data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveHatFromZipData (List<HatStatData> statData) {
      foreach (HatStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_hatStatRegistry.ContainsKey(uniqueID)) {
            _hatStatRegistry.Add(uniqueID, rawData);
            _hatStatList.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _hatStatRegistry.Count + "} hat data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void resetAllData () {
      _weaponStatRegistry = new Dictionary<int, WeaponStatData>();
      _armorStatRegistry = new Dictionary<int, ArmorStatData>();
      _hatStatRegistry = new Dictionary<int, HatStatData>();
      _ringStatRegistry = new Dictionary<int, RingStatData>();
      _necklaceStatRegistry = new Dictionary<int, NecklaceStatData>();
      _trinketStatRegistry = new Dictionary<int, TrinketStatData>();

      _weaponStatList.Clear();
      _armorStatList.Clear();
      _hatStatList.Clear();
      _ringStatList.Clear();
      _necklaceStatList.Clear();
      _trinketStatList.Clear();
   }

   public List<WeaponStatData> requestWeaponList (List<int> xmlIDList) {
      List<WeaponStatData> returnWeaponStatList = new List<WeaponStatData>();
      foreach (int index in xmlIDList) {
         WeaponStatData searchData = _weaponStatRegistry.Values.ToList().Find(_ => _.sqlId == index);
         if (searchData != null) {
            returnWeaponStatList.Add(searchData);
         }
      }

      return returnWeaponStatList;
   }

   public List<ArmorStatData> requestArmorList (List<int> xmlIDList) {
      List<ArmorStatData> returnArmorList = new List<ArmorStatData>();
      foreach (int index in xmlIDList) {
         ArmorStatData searchData = _armorStatRegistry.Values.ToList().Find(_ => _.sqlId == index);
         if (searchData != null) {
            returnArmorList.Add(searchData);
         }
      }

      return returnArmorList;
   }

   public List<HatStatData> requestHatList (List<int> xmlIDList) {
      List<HatStatData> returnHatStatList = new List<HatStatData>();
      foreach (int index in xmlIDList) {
         HatStatData searchData = _hatStatRegistry.Values.ToList().Find(_ => _.sqlId == index);
         if (searchData != null) {
            returnHatStatList.Add(searchData);
         }
      }

      return returnHatStatList;
   }

   public static List<Item> setEquipmentData (List<Item> itemData) {
      foreach (Item dataItem in itemData) {
         string itemName = "";
         string itemDesc = "";
         string itemPath = "";
         
         switch (dataItem.category) {
            case Item.Category.Armor:
               // Basic Info Setup
               ArmorStatData armorStatData = self.getArmorDataBySqlId(dataItem.itemTypeId);
               itemName = armorStatData.equipmentName;
               itemDesc = armorStatData.equipmentDescription;
               itemPath = armorStatData.equipmentIconPath;

               // Data Setup
               string data = string.Format("armor={0}, rarity={1}", armorStatData.armorBaseDefense, (int) Rarity.Type.Common);
               dataItem.data = data;

               dataItem.setBasicInfo(itemName, itemDesc, itemPath);
               break;
            case Item.Category.Weapon:
               // Basic Info Setup
               WeaponStatData weaponStatData = self.getWeaponData(dataItem.itemTypeId);
               if (weaponStatData != null) {
                  itemName = weaponStatData.equipmentName;
                  itemDesc = weaponStatData.equipmentDescription;
                  itemPath = weaponStatData.equipmentIconPath;

                  // Data Setup
                  data = string.Format("damage={0}, rarity={1}", weaponStatData.weaponBaseDamage, (int) Rarity.Type.Common);
                  dataItem.data = data;

                  dataItem.setBasicInfo(itemName, itemDesc, itemPath);
               } else {
                  Debug.LogError("There is no weapon data for: " + dataItem.id + " - " + dataItem.itemTypeId);
               }
               break;
            case Item.Category.Hats:
               // Basic Info Setup
               HatStatData hatStatData = self.getHatData(dataItem.itemTypeId);
               itemName = hatStatData.equipmentName;
               itemDesc = hatStatData.equipmentDescription;
               itemPath = hatStatData.equipmentIconPath;

               // Data Setup
               data = string.Format("armor={0}, rarity={1}", hatStatData.hatBaseDefense, (int) Rarity.Type.Common);
               dataItem.data = data;

               dataItem.setBasicInfo(itemName, itemDesc, itemPath);
               break;
         }
      }

      return itemData;
   }

   #region Get basic info of item

   public bool isLevelValid (int userLevel, Item item) {
      return userLevel >= equipmentLevelRequirement(item);
   }

   public int equipmentLevelRequirement (Item item) {
      int equipmentLevelRequirement = 0;
      switch (item.category) {
         case Item.Category.Weapon:
            WeaponStatData weaponData = getWeaponData(item.itemTypeId);
            if (weaponData != null) {
               equipmentLevelRequirement = weaponData.levelRequirement;
            }
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = getArmorDataBySqlId(item.itemTypeId);
            if (armorData != null) {
               equipmentLevelRequirement = armorData.levelRequirement;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = getHatData(item.itemTypeId);
            if (hatData != null) {
               equipmentLevelRequirement = hatData.levelRequirement;
            }
            break;
         case Item.Category.Ring:
            RingStatData ringData = getRingData(item.itemTypeId);
            if (ringData != null) {
               equipmentLevelRequirement = ringData.levelRequirement;
            }
            break;
         case Item.Category.Necklace:
            NecklaceStatData necklaceData = getNecklaceData(item.itemTypeId);
            if (necklaceData != null) {
               equipmentLevelRequirement = necklaceData.levelRequirement;
            }
            break;
         case Item.Category.Trinket:
            TrinketStatData trinketData = getTrinketData(item.itemTypeId);
            if (trinketData != null) {
               equipmentLevelRequirement = trinketData.levelRequirement;
            }
            break;
      }

      return equipmentLevelRequirement;
   }

   public bool isJobLevelValid (Jobs jobsData, Item item) {
      if (jobsData == null) {
         D.debug("Jobs data is corrupted!");
         return false;
      }
      
      int equipmentJobLevelRequirement = 0;
      Jobs.Type jobTypeRequirement = Jobs.Type.None;
      switch (item.category) {
         case Item.Category.Weapon:
            WeaponStatData weaponData = getWeaponData(item.itemTypeId);
            if (weaponData != null) {
               equipmentJobLevelRequirement = weaponData.jobLevelRequirement;
               jobTypeRequirement = weaponData.jobRequirement;
            }
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = getArmorDataBySqlId(item.itemTypeId);
            if (armorData != null) {
               equipmentJobLevelRequirement = armorData.jobLevelRequirement;
               jobTypeRequirement = armorData.jobRequirement;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = getHatData(item.itemTypeId);
            if (hatData != null) {
               equipmentJobLevelRequirement = hatData.jobLevelRequirement;
               jobTypeRequirement = hatData.jobRequirement;
            }
            break;
         case Item.Category.Ring:
            RingStatData ringData = getRingData(item.itemTypeId);
            if (ringData != null) {
               equipmentJobLevelRequirement = ringData.jobLevelRequirement;
               jobTypeRequirement = ringData.jobRequirement;
            }
            break;
         case Item.Category.Necklace:
            NecklaceStatData necklaceData = getNecklaceData(item.itemTypeId);
            if (necklaceData != null) {
               equipmentJobLevelRequirement = necklaceData.jobLevelRequirement;
               jobTypeRequirement = necklaceData.jobRequirement;
            }
            break;
         case Item.Category.Trinket:
            TrinketStatData trinketData = getTrinketData(item.itemTypeId);
            if (trinketData != null) {
               equipmentJobLevelRequirement = trinketData.jobLevelRequirement;
               jobTypeRequirement = trinketData.jobRequirement;
            }
            break;
      }

      if (jobTypeRequirement == Jobs.Type.None || equipmentJobLevelRequirement < 1) {
         return true;
      }

      switch (jobTypeRequirement) {
         case Jobs.Type.Farmer:
            return LevelUtil.levelForXp(jobsData.farmerXP) >= equipmentJobLevelRequirement;
         case Jobs.Type.Explorer:
            return LevelUtil.levelForXp(jobsData.explorerXP) >= equipmentJobLevelRequirement;
         case Jobs.Type.Sailor:
            return LevelUtil.levelForXp(jobsData.sailorXP) >= equipmentJobLevelRequirement;
         case Jobs.Type.Trader:
            return LevelUtil.levelForXp(jobsData.traderXP) >= equipmentJobLevelRequirement;
         case Jobs.Type.Crafter:
            return LevelUtil.levelForXp(jobsData.crafterXP) >= equipmentJobLevelRequirement;
         case Jobs.Type.Miner:
            return LevelUtil.levelForXp(jobsData.minerXP) >= equipmentJobLevelRequirement;
         default:
            return false;
      }
   }

   public string getItemName (Item item) {
      string newName = "";
      
      switch (item.category) {
         case Item.Category.Ring:
            RingStatData ringData = getRingData(item.itemTypeId);
            if (ringData != null) {
               newName = ringData.equipmentName;
            }
            break;
         case Item.Category.Necklace:
            NecklaceStatData necklaceData = getNecklaceData(item.itemTypeId);
            if (necklaceData != null) {
               newName = necklaceData.equipmentName;
            }
            break;
         case Item.Category.Trinket:
            TrinketStatData trinketData = getTrinketData(item.itemTypeId);
            if (trinketData != null) {
               newName = trinketData.equipmentName;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = getHatData(item.itemTypeId);
            if (hatData != null) {
               newName = hatData.equipmentName;
            }
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = getArmorDataBySqlId(item.itemTypeId);
            if (armorData != null) {
               newName = armorData.equipmentName;
            }
            break;
         case Item.Category.Weapon:
            WeaponStatData weaponData = getWeaponData(item.itemTypeId);
            if (weaponData != null) {
               newName = weaponData.equipmentName;
            }
            break;
         case Item.Category.CraftingIngredients:
            CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
            newName = CraftingIngredients.getName(ingredientType);
            break;
         case Item.Category.Quest_Item:
            QuestItem questItem = getQuestItemById(item.itemTypeId);
            if (questItem != null) {
               newName = questItem.itemName;
            }
            break;
         case Item.Category.Crop:
            if (CropsDataManager.self.tryGetCropData(item.itemTypeId, out CropsData data)) {
               newName = data.xmlName;
            }
            break;
         case Item.Category.Prop:
            if (ItemDefinitionManager.self.tryGetDefinition(item.itemTypeId, out PropDefinition prop)) {
               newName = prop.name;
            }
            break;
         case Item.Category.Blueprint:
            CraftableItemRequirements craftingItem = getCraftingItem(item);
            if (craftingItem != null) {
               if (craftingItem.resultItem.category == Item.Category.Weapon) {
                  WeaponStatData fetchedData = getWeaponData(craftingItem.resultItem.itemTypeId);
                  if (fetchedData != null) {
                     return fetchedData.equipmentName + " Design";
                  }
               }
               if (craftingItem.resultItem.category == Item.Category.Armor) {
                  ArmorStatData fetchedData = getArmorDataBySqlId(craftingItem.resultItem.itemTypeId);
                  if (fetchedData != null) {
                     return fetchedData.equipmentName + " Design";
                  }
               }
               if (craftingItem.resultItem.category == Item.Category.Hats) {
                  HatStatData fetchedData = getHatData(craftingItem.resultItem.itemTypeId);
                  if (fetchedData != null) {
                     return fetchedData.equipmentName + " Design";
                  }
               }
               if (craftingItem.resultItem.category == Item.Category.Ring) {
                  RingStatData fetchedData = getRingData(craftingItem.resultItem.itemTypeId);
                  if (fetchedData != null) {
                     return fetchedData.equipmentName + " Design";
                  }
               }
               if (craftingItem.resultItem.category == Item.Category.Necklace) {
                  NecklaceStatData fetchedData = getNecklaceData(craftingItem.resultItem.itemTypeId);
                  if (fetchedData != null) {
                     return fetchedData.equipmentName + " Design";
                  }
               }
               if (craftingItem.resultItem.category == Item.Category.Trinket) {
                  TrinketStatData fetchedData = getTrinketData(craftingItem.resultItem.itemTypeId);
                  if (fetchedData != null) {
                     return fetchedData.equipmentName + " Design";
                  }
               }
               if (craftingItem.resultItem.category == Item.Category.CraftingIngredients) {
                  CraftingIngredients referenceItem = new CraftingIngredients(craftingItem.resultItem);
                  if (referenceItem != null) {
                     return referenceItem.getName() + " Design";
                  }
               }
            } else {
               D.debug("The result is null for: " + item.category + " : " + item.itemTypeId + " : " + item.data);
            }
            break;
      }

      return newName;
   }

   public string getItemDescription (Item item) {
      string newDescription = "";

      switch (item.category) {
         case Item.Category.Ring:
            RingStatData ringData = getRingData(item.itemTypeId);
            if (ringData != null) {
               newDescription = ringData.equipmentDescription;
            }
            break;
         case Item.Category.Necklace:
            NecklaceStatData necklaceData = getNecklaceData(item.itemTypeId);
            if (necklaceData != null) {
               newDescription = necklaceData.equipmentDescription;
            }
            break;
         case Item.Category.Trinket:
            TrinketStatData trinketData = getTrinketData(item.itemTypeId);
            if (trinketData != null) {
               newDescription = trinketData.equipmentDescription;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = getHatData(item.itemTypeId);
            if (hatData != null) {
               newDescription = hatData.equipmentDescription;
            }
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = getArmorDataBySqlId(item.itemTypeId);
            if (armorData != null) {
               newDescription = armorData.equipmentDescription;
            }
            break;
         case Item.Category.Weapon:
            WeaponStatData weaponData = getWeaponData(item.itemTypeId);
            if (weaponData != null) {
               newDescription = weaponData.equipmentDescription;
            }
            break;
         case Item.Category.CraftingIngredients:
            CraftingIngredients newItem = new CraftingIngredients();
            newItem.category = item.category;
            newItem.itemTypeId = item.itemTypeId;
            newDescription = newItem.getCastItem().getDescription();
            break;
         case Item.Category.Quest_Item:
            QuestItem questItem = getQuestItemById(item.itemTypeId);
            if (questItem != null) {
               newDescription = questItem.itemDescription;
            }
            break;
         case Item.Category.Blueprint:
            CraftableItemRequirements craftingItem = getCraftingItem(item);
            if (craftingItem.resultItem.category == Item.Category.Weapon) {
               WeaponStatData fetchedData = getWeaponData(craftingItem.resultItem.itemTypeId);
               return fetchedData.equipmentDescription;
            }
            if (craftingItem.resultItem.category == Item.Category.Armor) {
               ArmorStatData fetchedData = getArmorDataBySqlId(craftingItem.resultItem.itemTypeId);
               return fetchedData.equipmentDescription;
            }
            if (craftingItem.resultItem.category == Item.Category.Hats) {
               HatStatData fetchedData = getHatData(craftingItem.resultItem.itemTypeId);
               return fetchedData.equipmentDescription;
            }
            if (craftingItem.resultItem.category == Item.Category.CraftingIngredients) {
               CraftingIngredients referenceItem = new CraftingIngredients(craftingItem.resultItem);
               if (referenceItem != null) {
                  return referenceItem.getDescription();
               }
            }
            break;
      }

      return newDescription;
   }

   public string getItemIconPath (Item item) {
      string iconPath = "";

      switch (item.category) {
         case Item.Category.Ring:
            RingStatData ringData = getRingData(item.itemTypeId);
            if (ringData != null) {
               iconPath = ringData.equipmentIconPath;
            }
            break;
         case Item.Category.Necklace:
            NecklaceStatData necklaceData = getNecklaceData(item.itemTypeId);
            if (necklaceData != null) {
               iconPath = necklaceData.equipmentIconPath;
            }
            break;
         case Item.Category.Trinket:
            TrinketStatData trinketData = getTrinketData(item.itemTypeId);
            if (trinketData != null) {
               iconPath = trinketData.equipmentIconPath;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = getHatData(item.itemTypeId);
            if (hatData != null) {
               iconPath = hatData.equipmentIconPath;
            }
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = getArmorDataBySqlId(item.itemTypeId);
            if (armorData != null) {
               iconPath = armorData.equipmentIconPath;
            }
            break;
         case Item.Category.Weapon:
            WeaponStatData weaponData = getWeaponData(item.itemTypeId);
            if (weaponData != null) {
               iconPath = weaponData.equipmentIconPath;
            }
            break;
         case Item.Category.CraftingIngredients:
            CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
            iconPath = CraftingIngredients.getIconPath(ingredientType);
            break;
         case Item.Category.Quest_Item:
            QuestItem questItem = getQuestItemById(item.itemTypeId);
            if (questItem != null) {
               iconPath = questItem.iconPath;
            }
            break;
         case Item.Category.Blueprint:
            CraftableItemRequirements craftingItem = getCraftingItem(item);

            if (craftingItem == null) {
               D.debug("Null Icon for Blueprint: " + item.category + " : " + item.itemTypeId + " : " + item.data);
               return "";
            }
            if (craftingItem.resultItem.category == Item.Category.Weapon) {
               WeaponStatData fetchedData = getWeaponData(craftingItem.resultItem.itemTypeId);
               return fetchedData.equipmentIconPath;
            }
            if (craftingItem.resultItem.category == Item.Category.Armor) {
               ArmorStatData fetchedData = getArmorDataBySqlId(craftingItem.resultItem.itemTypeId);
               return fetchedData.equipmentIconPath;
            }
            if (craftingItem.resultItem.category == Item.Category.Hats) {
               HatStatData fetchedData = self.getHatData(craftingItem.resultItem.itemTypeId);
               return fetchedData.equipmentIconPath;
            }
            if (craftingItem.resultItem.category == Item.Category.CraftingIngredients) {
               CraftingIngredients referenceItem = new CraftingIngredients(craftingItem.resultItem);
               if (referenceItem != null) {
                  return referenceItem.getBorderlessIconPath();
               }
            }
            break;
      }

      return iconPath;
   }

   #endregion

   private CraftableItemRequirements getCraftingItem (Item item) {
      Item.Category blueprintCategory = Item.Category.None;
      switch (item.data) {
         case Blueprint.WEAPON_DATA_PREFIX:
            blueprintCategory = Item.Category.Weapon;
            break;
         case Blueprint.ARMOR_DATA_PREFIX:
            blueprintCategory = Item.Category.Armor;
            break;
         case Blueprint.HAT_DATA_PREFIX:
            blueprintCategory = Item.Category.Hats;
            break;
         case Blueprint.RING_DATA_PREFIX:
            blueprintCategory = Item.Category.Ring;
            break;
         case Blueprint.NECKLACE_DATA_PREFIX:
            blueprintCategory = Item.Category.Necklace;
            break;
         case Blueprint.TRINKET_DATA_PREFIX:
            blueprintCategory = Item.Category.Trinket;
            break;
         case Blueprint.INGREDIENT_DATA_PREFIX:
            blueprintCategory = Item.Category.CraftingIngredients;
            break;
      }

      CraftableItemRequirements craftingItem = CraftingManager.self.getCraftableData(blueprintCategory, item.itemTypeId);
      return craftingItem;
   }

   #region Translate equipment to items

   public List<Item> translateWeaponItemsToItems (List<Weapon> weaponList) {
      List<Item> newWeaponList = new List<Item>();

      // Assign the appropriate data for the weapons using the weapon type id
      foreach (Weapon weapon in weaponList) {
         if (weapon.itemTypeId > 0) {
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);
            if (weaponData != null) {
               weapon.data = WeaponStatData.serializeWeaponStatData(weaponData);
            } else {
               D.warning("There is no data for Weapon Type: " + weapon.itemTypeId);
            }
         }
         newWeaponList.Add(weapon);
      }
      return newWeaponList;
   }

   public List<Item> translateHatItemsToItems (List<Hat> hatList) {
      List<Item> newHatList = new List<Item>();

      // Assign the appropriate data for the hats using the hat type id
      foreach (Hat hat in hatList) {
         if (hat.itemTypeId > 0) {
            HatStatData hatData = EquipmentXMLManager.self.getHatData(hat.itemTypeId);
            if (hatData != null) {
               hat.data = HatStatData.serializeHatStatData(hatData);
            } else {
               D.warning("There is no data for Hat Type: " + hat.itemTypeId);
            }
         }
         newHatList.Add(hat);
      }
      return newHatList;
   }

   public List<Item> translateArmorItemsToItems (List<Armor> armorList) {
      List<Item> newArmorList = new List<Item>();

      // Assign the appropriate data for the armors using the armor type id
      for (int i = 0; i < armorList.Count; i++) {
         if (armorList[i].itemTypeId != 0) {
            ArmorStatData armorStat = getArmorDataBySqlId(armorList[i].itemTypeId);
            if (armorStat != null) {
               if (armorList[i].data != null) {
                  armorList[i].data = ArmorStatData.serializeArmorStatData(armorStat);
               } else {
                  D.warning("There is no data for Armor Type: " + armorList[i].itemTypeId);
               }
               newArmorList.Add(armorList[i]);
            } else {
               newArmorList.Add(new Armor());
               D.debug("Cannot process loaded armor data for armorType: {" + armorList[i].itemTypeId + "}");
            }
         } else {
            newArmorList.Add(new Armor());
            if (armorList[i].itemTypeId > 0) {
               D.debug("Cannot process loaded armor data for armorType: {" + armorList[i].itemTypeId + "}");
            }
         }
      }
      return newArmorList;
   }
   
   #endregion

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<WeaponStatData> _weaponStatList = new List<WeaponStatData>();
   [SerializeField]
   private List<ArmorStatData> _armorStatList = new List<ArmorStatData>();
   [SerializeField]
   private List<HatStatData> _hatStatList = new List<HatStatData>();
   [SerializeField]
   private List<RingStatData> _ringStatList = new List<RingStatData>();
   [SerializeField]
   private List<NecklaceStatData> _necklaceStatList = new List<NecklaceStatData>();
   [SerializeField]
   private List<TrinketStatData> _trinketStatList = new List<TrinketStatData>();

   // Stores the list of all weapon data
   private Dictionary<int, WeaponStatData> _weaponStatRegistry = new Dictionary<int, WeaponStatData>();

   // Stores the list of all armor data
   private Dictionary<int, ArmorStatData> _armorStatRegistry = new Dictionary<int, ArmorStatData>();

   // Stores the list of all hat data
   private Dictionary<int, HatStatData> _hatStatRegistry = new Dictionary<int, HatStatData>();

   // Stores the list of all ring data
   private Dictionary<int, RingStatData> _ringStatRegistry = new Dictionary<int, RingStatData>();

   // Stores the list of all necklace data
   private Dictionary<int, NecklaceStatData> _necklaceStatRegistry = new Dictionary<int, NecklaceStatData>();

   // Stores the list of all trinked data
   private Dictionary<int, TrinketStatData> _trinketStatRegistry = new Dictionary<int, TrinketStatData>();

   // Stores the list of all quest item data
   public List<QuestItem> _questItemList = new List<QuestItem>();

   #endregion
}
