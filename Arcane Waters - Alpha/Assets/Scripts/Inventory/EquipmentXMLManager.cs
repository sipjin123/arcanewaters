using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.IO;

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

   #endregion

   private void Awake () {
      self = this;
      initializeDataCache();
   }

   public WeaponStatData getWeaponData (Weapon.Type weaponType) {
      if (_weaponStatList.ContainsKey(weaponType)) {
         return _weaponStatList[weaponType];
      }
      return null;
   }

   public ArmorStatData getArmorData (Armor.Type armorType) {
      if (_armorStatList.ContainsKey(armorType)) {
         return _armorStatList[armorType];
      }
      return null;
   }

   private void initializeDataCache () {
      _weaponStatList = new Dictionary<Weapon.Type, WeaponStatData>();
      _armorStatList = new Dictionary<Armor.Type, ArmorStatData>();
      _helmStatList = new Dictionary<Helm.Type, HelmStatData>();

      // Iterate over the files
      foreach (TextAsset textAsset in weaponTextAssets) {
         // Read and deserialize the file
         WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(textAsset);
         Weapon.Type uniqueID = rawData.weaponType;

         // Save the data in the memory cache
         if (!_weaponStatList.ContainsKey(uniqueID)) {
            _weaponStatList.Add(uniqueID, rawData);
         }
      }

      // Iterate over the files
      foreach (TextAsset textAsset in armorTextAssets) {
         // Read and deserialize the file
         ArmorStatData rawData = Util.xmlLoad<ArmorStatData>(textAsset);
         Armor.Type uniqueID = rawData.armorType;

         // Save the data in the memory cache
         if (!_armorStatList.ContainsKey(uniqueID)) {
            _armorStatList.Add(uniqueID, rawData);
         }
      }

      // Iterate over the files
      foreach (TextAsset textAsset in helmTextAssets) {
         // Read and deserialize the file
         HelmStatData rawData = Util.xmlLoad<HelmStatData>(textAsset);
         Helm.Type uniqueID = rawData.helmType;

         // Save the data in the memory cache
         if (!_helmStatList.ContainsKey(uniqueID)) {
            _helmStatList.Add(uniqueID, rawData);
         }
      }
   }
   
   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadWeaponXMLData();
      loadArmorXMLData();
      loadHelmXMLData();
   }

   private void loadWeaponXMLData () {
      weaponTextAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "EquipmentStats/Weapons");

      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      } else {
         // Get the list of XML files in the folder
         string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

         // Iterate over the files
         foreach (string fileName in fileNames) {
            // Build the path to a single file
            string filePath = Path.Combine(directoryPath, fileName);

            // Read and deserialize the file
            TextAsset textAsset = (TextAsset) UnityEditor.AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset));
            weaponTextAssets.Add(textAsset);
         }
      }
   }

   private void loadArmorXMLData () {
      armorTextAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "EquipmentStats/Armors");

      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      } else {
         // Get the list of XML files in the folder
         string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

         // Iterate over the files
         foreach (string fileName in fileNames) {
            // Build the path to a single file
            string filePath = Path.Combine(directoryPath, fileName);

            // Read and deserialize the file
            TextAsset textAsset = (TextAsset) UnityEditor.AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset));
            armorTextAssets.Add(textAsset);
         }
      }
   }

   private void loadHelmXMLData () {
      helmTextAssets = new List<TextAsset>();

      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine("Assets", "Data", "EquipmentStats/Helms");

      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      } else {
         // Get the list of XML files in the folder
         string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

         // Iterate over the files
         foreach (string fileName in fileNames) {
            // Build the path to a single file
            string filePath = Path.Combine(directoryPath, fileName);

            // Read and deserialize the file
            TextAsset textAsset = (TextAsset) UnityEditor.AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset));
            helmTextAssets.Add(textAsset);
         }
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
