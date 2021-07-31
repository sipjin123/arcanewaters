﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.IO;
using static EquipmentToolManager;
using UnityEngine.Events;

public class EquipmentXMLManager : MonoBehaviour {
   #region Public Variables

   // A convenient self reference
   public static EquipmentXMLManager self;

   // References to all the weapon data
   public List<WeaponStatData> weaponStatList { get { return _weaponStatList.Values.ToList(); } }

   // References to all the armor data
   public List<ArmorStatData> armorStatList { get { return _armorStatList.Values.ToList(); } }

   // References to all the hat data
   public List<HatStatData> hatStatList { get { return _hatStatList.Values.ToList(); } }

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

   #endregion

   private void Awake () {
      _weaponStatList = new Dictionary<int, WeaponStatData>();
      _armorStatList = new Dictionary<int, ArmorStatData>();
      _hatStatList = new Dictionary<int, HatStatData>();
      self = this;
   }

   public WeaponStatData getWeaponData (int equipmentID) {
      if (_weaponStatList.ContainsKey(equipmentID)) {
         return _weaponStatList[equipmentID];
      }
      return null;
   }

   public ArmorStatData getArmorDataBySqlId (int sqlId) {
      if (_armorStatList.ContainsKey(sqlId)) {
         return _armorStatList[sqlId];
      }
      if (sqlId > 0) {
         D.debug("Armor Does not exist: " + sqlId);
      }
      return null;
   }

   public HatStatData getHatData (int hatType) {
      HatStatData hatDataFetched = _hatStatList.Values.ToList().Find(_ => _.sqlId == hatType);
      if (hatDataFetched != null) {
         return hatDataFetched;
      }
      return null;
   }

   private void finishedLoading () {
      equipmentLoadCounter++;
      if (equipmentLoadCounter >= TOTAL_EQUIPMENT_TYPES) {
         loadedAllEquipment = true;
         finishedDataSetup.Invoke();
      }
   }

   public void initializeDataCache () {
      equipmentLoadCounter = 0;
      _weaponStatList = new Dictionary<int, WeaponStatData>();
      _armorStatList = new Dictionary<int, ArmorStatData>();
      _hatStatList = new Dictionary<int, HatStatData>();
      _hatStatData = new List<HatStatData>();

      List<XMLPair> rawWeaponXml = DB_Main.getEquipmentXML(EquipmentType.Weapon);
      foreach (XMLPair xmlPair in rawWeaponXml) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(newTextAsset);
               rawData.sqlId = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_weaponStatList.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _weaponStatList.Add(xmlPair.xmlId, rawData);
                  _weaponStatData.Add(rawData);
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
            if (!_armorStatList.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
               _armorStatList.Add(xmlPair.xmlId, rawData);
               _armorStatData.Add(rawData);
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
            if (!_hatStatList.ContainsKey(uniqueID) && xmlPair.isEnabled) {
               _hatStatList.Add(uniqueID, rawData);
               _hatStatData.Add(rawData);
            }
         }
      }
      equipmentLoadCounter = TOTAL_EQUIPMENT_TYPES;
      finishedLoading();
   }

   public void receiveWeaponDataFromZipData (List<WeaponStatData> statData) {
      foreach (WeaponStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_weaponStatList.ContainsKey(uniqueID)) {
            _weaponStatList.Add(uniqueID, rawData);
            _weaponStatData.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _weaponStatData.Count + "} weapon data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveArmorDataFromZipData (List<ArmorStatData> statData) {
      foreach (ArmorStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_armorStatList.ContainsKey(uniqueID)) {
            _armorStatList.Add(uniqueID, rawData);
            _armorStatData.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _armorStatData.Count + "} armor data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void receiveHatFromZipData (List<HatStatData> statData) {
      foreach (HatStatData rawData in statData) {
         int uniqueID = rawData.sqlId;

         // Save the data in the memory cache
         if (!_hatStatList.ContainsKey(uniqueID)) {
            _hatStatList.Add(uniqueID, rawData);
            _hatStatData.Add(rawData);
         }
      }
      D.adminLog("EquipmentXML :: Received a total of {" + _hatStatData.Count + "} hat data from {" + statData.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void resetAllData () {
      _weaponStatList = new Dictionary<int, WeaponStatData>();
      _armorStatList = new Dictionary<int, ArmorStatData>();
      _hatStatList = new Dictionary<int, HatStatData>();

      _weaponStatData.Clear();
      _armorStatData.Clear();
      _hatStatData.Clear();
   }

   public List<WeaponStatData> requestWeaponList (List<int> xmlIDList) {
      List<WeaponStatData> returnWeaponStatList = new List<WeaponStatData>();
      foreach (int index in xmlIDList) {
         WeaponStatData searchData = _weaponStatList.Values.ToList().Find(_ => _.sqlId == index);
         if (searchData != null) {
            returnWeaponStatList.Add(searchData);
         }
      }

      return returnWeaponStatList;
   }

   public List<ArmorStatData> requestArmorList (List<int> xmlIDList) {
      List<ArmorStatData> returnArmorList = new List<ArmorStatData>();
      foreach (int index in xmlIDList) {
         ArmorStatData searchData = _armorStatList.Values.ToList().Find(_ => _.sqlId == index);
         if (searchData != null) {
            returnArmorList.Add(searchData);
         }
      }

      return returnArmorList;
   }

   public List<HatStatData> requestHatList (List<int> xmlIDList) {
      List<HatStatData> returnHatStatList = new List<HatStatData>();
      foreach (int index in xmlIDList) {
         HatStatData searchData = _hatStatList.Values.ToList().Find(_ => _.sqlId == index);
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

   public string getItemName (Item item) {
      string newName = "";
      
      switch (item.category) {
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
         case Item.Category.Hats:
            HatStatData hatData = getHatData(item.itemTypeId);
            newDescription = hatData.equipmentDescription;
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = getArmorDataBySqlId(item.itemTypeId);
            newDescription = armorData.equipmentDescription;
            break;
         case Item.Category.Weapon:
            WeaponStatData weaponData = getWeaponData(item.itemTypeId);
            newDescription = weaponData.equipmentDescription;
            break;
         case Item.Category.CraftingIngredients:
            CraftingIngredients newItem = new CraftingIngredients();
            newItem.category = item.category;
            newItem.itemTypeId = item.itemTypeId;
            newDescription = newItem.getCastItem().getDescription();
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
            break;
      }

      return newDescription;
   }

   public string getItemIconPath (Item item) {
      string iconPath = "";

      switch (item.category) {
         case Item.Category.Hats:
            HatStatData hatData = getHatData(item.itemTypeId);
            iconPath = hatData.equipmentIconPath;
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = getArmorDataBySqlId(item.itemTypeId);
            iconPath = armorData.equipmentIconPath;
            break;
         case Item.Category.Weapon:
            WeaponStatData weaponData = getWeaponData(item.itemTypeId);
            iconPath = weaponData.equipmentIconPath;
            break;
         case Item.Category.CraftingIngredients:
            CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
            iconPath = CraftingIngredients.getIconPath(ingredientType);
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
            break;
      }

      return iconPath;
   }

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
      }

      CraftableItemRequirements craftingItem = CraftingManager.self.getCraftableData(blueprintCategory, item.itemTypeId);
      return craftingItem;
   }

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
               D.debug("Cannot process loaded armor data for armorType: {" + armorList[i].itemTypeId + "}");
            }
         }
      }
      return newArmorList;
   }

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<WeaponStatData> _weaponStatData = new List<WeaponStatData>();
   [SerializeField]
   private List<ArmorStatData> _armorStatData = new List<ArmorStatData>();
   [SerializeField]
   private List<HatStatData> _hatStatData = new List<HatStatData>();

   // Stores the list of all weapon data
   private Dictionary<int, WeaponStatData> _weaponStatList = new Dictionary<int, WeaponStatData>();

   // Stores the list of all armor data
   private Dictionary<int, ArmorStatData> _armorStatList = new Dictionary<int, ArmorStatData>();

   // Stores the list of all hat data
   private Dictionary<int, HatStatData> _hatStatList = new Dictionary<int, HatStatData>();

   #endregion
}
