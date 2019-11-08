using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class EquipmentToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the equipment data
   public EquipmentToolScene equipmentDataScene;

   public enum EquipmentType
   {
      None = 0,
      Weapon =  1,
      Armor = 2,
      Helm = 3,
   }

   #endregion

   private void Start () {
      loadXMLData();
   }

   #region Save 

   public void saveWeapon (WeaponStatData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Weapons");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      } 

      // Build the file name
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Weapons", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void saveArmor (ArmorStatData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Armors");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Armors", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void saveHelm (HelmStatData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Helms");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Helms", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #endregion

   #region Delete

   public void deleteWeapon (WeaponStatData data) {
      // Build the file name
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Weapons", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void deleteArmor (ArmorStatData data) {
      // Build the file name
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Armors", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public void deleteHelm (HelmStatData data) {
      // Build the file name
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Helms", fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   #endregion

   #region Duplicate

   public void duplicateWeapon (WeaponStatData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Weapons");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.equipmentName += "_copy";
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Weapons", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void duplicateArmor (ArmorStatData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Armors");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.equipmentName += "_copy";
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Armors", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   public void duplicateHelm (HelmStatData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Helms");
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.equipmentName += "_copy";
      string fileName = data.equipmentName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Helms", fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #endregion

   #region Loading

   public void loadXMLData () {
      loadWeapons();
      loadArmors();
      loadHelms();
   }

   private void loadWeapons () {
      _weaponStatData = new Dictionary<string, WeaponStatData>();
      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Weapons");

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
            WeaponStatData weaponData = ToolsUtil.xmlLoad<WeaponStatData>(filePath);

            // Save the Data in the memory cache
            _weaponStatData.Add(weaponData.equipmentName, weaponData);
         }
         if (fileNames.Length > 0) {
            equipmentDataScene.loadWeaponData(_weaponStatData);
         }
      }
   }

   private void loadArmors () {
      _armorStatData = new Dictionary<string, ArmorStatData>();
      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Armors");

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
            ArmorStatData armorData = ToolsUtil.xmlLoad<ArmorStatData>(filePath);

            // Save the Data in the memory cache
            _armorStatData.Add(armorData.equipmentName, armorData);
         }
         if (fileNames.Length > 0) {
            equipmentDataScene.loadArmorData(_armorStatData);
         }
      }
   }

   private void loadHelms () {
      _helmStatData = new Dictionary<string, HelmStatData>();
      // Build the path to the folder containing the data XML files
      string directoryPath = Path.Combine(Application.dataPath, "Data", "EquipmentStats/Helms");

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
            HelmStatData helmData = ToolsUtil.xmlLoad<HelmStatData>(filePath);

            // Save the Data in the memory cache
            _helmStatData.Add(helmData.equipmentName, helmData);
         }
         if (fileNames.Length > 0) {
            equipmentDataScene.loadHelmData(_helmStatData);
         }
      }
   }

   #endregion

   #region Private Variables

   // Holds the list of loaded weapon data
   private Dictionary<string, WeaponStatData> _weaponStatData = new Dictionary<string, WeaponStatData>();

   // Holds the list of loaded armor data
   private Dictionary<string, ArmorStatData> _armorStatData = new Dictionary<string, ArmorStatData>();

   // Holds the list of loaded helm data
   private Dictionary<string, HelmStatData> _helmStatData = new Dictionary<string, HelmStatData>();

   #endregion
}
