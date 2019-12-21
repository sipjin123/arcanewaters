using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.IO;
using static EquipmentToolManager;

public class EquipmentXMLManager : XmlManager {
   #region Public Variables

   // A convenient self reference
   public static EquipmentXMLManager self;

   // References to all the weapon data
   public List<WeaponStatData> weaponStatList { get { return _weaponStatList.Values.ToList(); } }

   // References to all the armor data
   public List<ArmorStatData> armorStatList { get { return _armorStatList.Values.ToList(); } }

   // Holds the xml raw data
   public List<TextAsset> weaponTextAssets, armorTextAssets, helmTextAssets;

   // Display list for editor reviewing
   public List<WeaponStatData> weaponStatData = new List<WeaponStatData>();
   public List<ArmorStatData> armorStatData = new List<ArmorStatData>();
   public List<HelmStatData> helmStatData = new List<HelmStatData>();

   // Determines how many equipment set has been loaded
   public int equipmentLoadCounter = 0;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      initializeDataCache();
   }

   public WeaponStatData getWeaponData (Weapon.Type weaponType) {
      if (_weaponStatList == null) {
         return null;
      }
      if (_weaponStatList.ContainsKey(weaponType)) {
         return _weaponStatList[weaponType];
      }
      return null;
   }

   public ArmorStatData getArmorData (Armor.Type armorType) {
      if (_armorStatList == null) {
         return null;
      }
      if (_armorStatList.ContainsKey(armorType)) {
         return _armorStatList[armorType];
      }
      return null;
   }

   private void finishedLoading () {
      equipmentLoadCounter ++;
      if (equipmentLoadCounter == 3) {
         ShopManager.self.initializeRandomGeneratedItems();
         equipmentLoadCounter = 0;
      }
   }

   private void initializeDataCache () {
      equipmentLoadCounter = 0;
      _weaponStatList = new Dictionary<Weapon.Type, WeaponStatData>();
      _armorStatList = new Dictionary<Armor.Type, ArmorStatData>();
      _helmStatList = new Dictionary<Helm.Type, HelmStatData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(newTextAsset);
               Weapon.Type uniqueID = rawData.weaponType;

               // Save the data in the memory cache
               if (!_weaponStatList.ContainsKey(uniqueID)) {
                  _weaponStatList.Add(uniqueID, rawData);
                  weaponStatData.Add(rawData);
               }
            }
            finishedLoading();
         });
      });

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               ArmorStatData rawData = Util.xmlLoad<ArmorStatData>(newTextAsset);
               Armor.Type uniqueID = rawData.armorType;

               // Save the data in the memory cache
               if (!_armorStatList.ContainsKey(uniqueID)) {
                  _armorStatList.Add(uniqueID, rawData);
                  armorStatData.Add(rawData);
               }
            }
            finishedLoading();
         });
      });

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               HelmStatData rawData = Util.xmlLoad<HelmStatData>(newTextAsset);
               Helm.Type uniqueID = rawData.helmType;

               // Save the data in the memory cache
               if (!_helmStatList.ContainsKey(uniqueID)) {
                  _helmStatList.Add(uniqueID, rawData);
                  helmStatData.Add(rawData);
               }
            }
            finishedLoading();
         });
      });
   }
   
   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(EquipmentType.Weapon, FOLDER_PATH_WEAPON);
      loadXMLData(EquipmentType.Armor, FOLDER_PATH_ARMOR);
      loadXMLData(EquipmentType.Helm, FOLDER_PATH_HELM);
   }

   private void loadXMLData (EquipmentType equipmentType, string folderName) {
      switch (equipmentType) {
         case EquipmentType.Weapon: {
               TextAsset[] textArray = Resources.LoadAll("Data/" + folderName, typeof(TextAsset)).Cast<TextAsset>().ToArray();
               weaponTextAssets = new List<TextAsset>(textArray);
            }
            break;
         case EquipmentType.Armor: {
               TextAsset[] textArray = Resources.LoadAll("Data/" + folderName, typeof(TextAsset)).Cast<TextAsset>().ToArray();
               armorTextAssets = new List<TextAsset>(textArray);
            }
            break;
         case EquipmentType.Helm: {
               TextAsset[] textArray = Resources.LoadAll("Data/" + folderName, typeof(TextAsset)).Cast<TextAsset>().ToArray();
               helmTextAssets = new List<TextAsset>(textArray);
            }
            break;
      }
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      weaponTextAssets = new List<TextAsset>();
      armorTextAssets = new List<TextAsset>();
      helmTextAssets = new List<TextAsset>();
   }

   #region Private Variables

   // Stores the list of all weapon data
   private Dictionary<Weapon.Type, WeaponStatData> _weaponStatList;

   // Stores the list of all armor data
   private Dictionary<Armor.Type, ArmorStatData> _armorStatList;

   // Stores the list of all helm data
   private Dictionary<Helm.Type, HelmStatData> _helmStatList;

   #endregion
}
