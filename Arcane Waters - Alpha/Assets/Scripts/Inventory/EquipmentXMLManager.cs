using UnityEngine;
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

   // Display list for editor reviewing
   public List<WeaponStatData> weaponStatData = new List<WeaponStatData>();
   public List<ArmorStatData> armorStatData = new List<ArmorStatData>();
   public List<HatStatData> hatStatData = new List<HatStatData>();

   // Determines how many equipment set has been loaded
   public int equipmentLoadCounter = 0;

   // Determines if all equipment is loaded
   public bool loadedAllEquipment;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this;
   }

   public WeaponStatData getWeaponData (int equipmentID) {
      if (_weaponStatList.ContainsKey(equipmentID)) {
         return _weaponStatList[equipmentID];
      }
      return null;
   }

   public ArmorStatData getArmorData (int armorType) {
      if (_armorStatList == null) {
         Debug.LogWarning("Does not exist: " + armorType);
         return null;
      }
      if (_armorStatList.ContainsKey(armorType)) {
         return _armorStatList[armorType];
      }
      return null;
   }

   public HatStatData getHatData (int hatType) {
      if (_hatStatList == null) {
         return null;
      }
      HatStatData hatDataFetched = _hatStatList.Values.ToList().Find(_ => _.equipmentID == hatType);
      if (hatDataFetched != null) {
         return hatDataFetched;
      }
      return null;
   }

   private void finishedLoading () {
      loadedAllEquipment = true;
      if (Global.player != null) { 
         Global.player.admin.buildItemNamesDictionary();
      }
      finishedDataSetup.Invoke();
   }

   public void initializeDataCache () {
      equipmentLoadCounter = 0;
      _weaponStatList = new Dictionary<int, WeaponStatData>();
      _armorStatList = new Dictionary<int, ArmorStatData>();
      _hatStatList = new Dictionary<int, HatStatData>();

      List<XMLPair> rawWeaponXml = DB_Main.getEquipmentXML(EquipmentType.Weapon);
      foreach (XMLPair xmlPair in rawWeaponXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(newTextAsset);
            rawData.equipmentID = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!_weaponStatList.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
               _weaponStatList.Add(xmlPair.xmlId, rawData);
               weaponStatData.Add(rawData);
            }
         }
      }

      List<XMLPair> rawArmorXml = DB_Main.getEquipmentXML(EquipmentType.Armor);
      foreach (XMLPair xmlPair in rawArmorXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            ArmorStatData rawData = Util.xmlLoad<ArmorStatData>(newTextAsset);
            rawData.equipmentID = xmlPair.xmlId;

            // Save the data in the memory cache
            if (!_armorStatList.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
               _armorStatList.Add(xmlPair.xmlId, rawData);
               armorStatData.Add(rawData);
            }
         }
      }

      List<XMLPair> rawHatXml = DB_Main.getEquipmentXML(EquipmentType.Hat);
      foreach (XMLPair xmlPair in rawHatXml) {
         if (xmlPair.isEnabled) {
            TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
            HatStatData rawData = Util.xmlLoad<HatStatData>(newTextAsset);
            int uniqueID = rawData.hatType;

            // Save the data in the memory cache
            if (!_hatStatList.ContainsKey(uniqueID) && xmlPair.isEnabled) {
               _hatStatList.Add(uniqueID, rawData);
               hatStatData.Add(rawData);
            }
         }
      }

      finishedLoading();
   }

   public void receiveWeaponDataFromZipData (List<WeaponStatData> statData) {
      foreach (WeaponStatData rawData in statData) {
         int uniqueID = rawData.weaponType;
         // Save the data in the memory cache
         if (!_weaponStatList.ContainsKey(uniqueID)) {
            _weaponStatList.Add(uniqueID, rawData);
            weaponStatData.Add(rawData);
         }
      }
      finishedLoading();
   }

   public void receiveArmorDataFromZipData (List<ArmorStatData> statData) {
      foreach (ArmorStatData rawData in statData) {
         int uniqueID = rawData.armorType;
         // Save the data in the memory cache
         if (!_armorStatList.ContainsKey(uniqueID)) {
            _armorStatList.Add(uniqueID, rawData);
            armorStatData.Add(rawData);
         }
      }
      finishedLoading();
   }

   public void receiveHatFromZipData (List<HatStatData> statData) {
      _hatStatList = new Dictionary<int, HatStatData>();
      foreach (HatStatData rawData in statData) {
         int uniqueID = rawData.hatType;
         // Save the data in the memory cache
         if (!_hatStatList.ContainsKey(uniqueID)) {
            _hatStatList.Add(uniqueID, rawData);
            hatStatData.Add(rawData);
         }
      }
      finishedLoading();
   }

   public void resetAllData () {
      _weaponStatList = new Dictionary<int, WeaponStatData>();
      _armorStatList = new Dictionary<int, ArmorStatData>();
      _hatStatList = new Dictionary<int, HatStatData>();

      weaponStatData.Clear();
      armorStatData.Clear();
      hatStatData.Clear();
   }

   public List<WeaponStatData> requestWeaponList (List<int> xmlIDList) {
      List<WeaponStatData> returnWeaponStatList = new List<WeaponStatData>();
      foreach (int index in xmlIDList) {
         WeaponStatData searchData = _weaponStatList.Values.ToList().Find(_ => _.equipmentID == index);
         if (searchData != null) {
            returnWeaponStatList.Add(searchData);
         }
      }

      return returnWeaponStatList;
   }

   public List<ArmorStatData> requestArmorList (List<int> xmlIDList) {
      List<ArmorStatData> returnArmorList = new List<ArmorStatData>();
      foreach (int index in xmlIDList) {
         ArmorStatData searchData = _armorStatList.Values.ToList().Find(_ => _.equipmentID == index);
         if (searchData != null) {
            returnArmorList.Add(searchData);
         }
      }

      return returnArmorList;
   }

   public List<HatStatData> requestHatList (List<int> xmlIDList) {
      List<HatStatData> returnHatStatList = new List<HatStatData>();
      foreach (int index in xmlIDList) {
         HatStatData searchData = _hatStatList.Values.ToList().Find(_ => _.equipmentID == index);
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
               ArmorStatData armorStatData = self.getArmorData(dataItem.itemTypeId);
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

   #region Private Variables

   // Stores the list of all weapon data
   private Dictionary<int, WeaponStatData> _weaponStatList = new Dictionary<int, WeaponStatData>();

   // Stores the list of all armor data
   private Dictionary<int, ArmorStatData> _armorStatList = new Dictionary<int, ArmorStatData>();

   // Stores the list of all hat data
   private Dictionary<int, HatStatData> _hatStatList = new Dictionary<int, HatStatData>();

   #endregion
}
