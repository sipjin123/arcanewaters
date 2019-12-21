using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class EquipmentToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the equipment data
   public EquipmentToolScene equipmentDataScene;

   // Holds the path of the folder
   public const string FOLDER_PATH_WEAPON = "EquipmentStats/Weapons";
   public const string FOLDER_PATH_ARMOR = "EquipmentStats/Armors";
   public const string FOLDER_PATH_HELM = "EquipmentStats/Helms";

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
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, (int)data.weaponType, EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void saveArmor (ArmorStatData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, (int) data.armorType, EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void saveHelm (HelmStatData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, (int) data.helmType, EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #endregion

   #region Delete

   public void deleteWeapon (WeaponStatData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteEquipmentXML((int)data.weaponType, EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteArmor (ArmorStatData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteEquipmentXML((int) data.armorType, EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteHelm (HelmStatData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteEquipmentXML((int) data.helmType, EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #endregion

   #region Duplicate

   public void duplicateWeapon (WeaponStatData data) {
      data.weaponType = 0;
      data.equipmentName = "Undefined Weapon";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, (int)data.weaponType, EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateArmor (ArmorStatData data) {
      data.armorType = 0;
      data.equipmentName = "Undefined Armor";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, (int) data.armorType, EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateHelm (HelmStatData data) {
      data.helmType = 0;
      data.equipmentName = "Undefined Helmet";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, (int) data.helmType, EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #endregion

   #region Loading

   public void loadXMLData () {
      loadCounter = 0;
      XmlLoadingPanel.self.startLoading();
      loadWeapons();
      loadArmors();
      loadHelms();
   }

   private void loadWeapons () {
      _weaponStatData = new Dictionary<Weapon.Type, WeaponStatData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(newTextAsset);

               // Save the data in the memory cache
               if (!_weaponStatData.ContainsKey(rawData.weaponType)) {
                  // Save the Data in the memory cache
                  _weaponStatData.Add(rawData.weaponType, rawData);
               }
            }
            equipmentDataScene.loadWeaponData(_weaponStatData);
            finishLoading();
         });
      });
   }

   private void loadArmors () {
      _armorStatData = new Dictionary<Armor.Type, ArmorStatData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               ArmorStatData rawData = Util.xmlLoad<ArmorStatData>(newTextAsset);
               Armor.Type uniqueID = rawData.armorType;

               // Save the data in the memory cache
               if (!_armorStatData.ContainsKey(rawData.armorType)) {
                  _armorStatData.Add(rawData.armorType, rawData);
               }
            }
            equipmentDataScene.loadArmorData(_armorStatData);
            finishLoading();
         });
      });
   }

   private void loadHelms () {
      _helmStatData = new Dictionary<Helm.Type, HelmStatData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               HelmStatData rawData = Util.xmlLoad<HelmStatData>(newTextAsset);
               Helm.Type uniqueID = rawData.helmType;

               // Save the data in the memory cache
               if (!_helmStatData.ContainsKey(rawData.helmType)) {
                  _helmStatData.Add(rawData.helmType, rawData);
               }
            }
            equipmentDataScene.loadHelmData(_helmStatData);
            finishLoading();
         });
      });
   }

   private void finishLoading () {
      loadCounter++;
      if (loadCounter == 3) {
         XmlLoadingPanel.self.finishLoading();
         loadCounter = 0;
      }
   }

   #endregion

   #region Private Variables

   // Holds the list of loaded weapon data
   private Dictionary<Weapon.Type, WeaponStatData> _weaponStatData = new Dictionary<Weapon.Type, WeaponStatData>();

   // Holds the list of loaded armor data
   private Dictionary<Armor.Type, ArmorStatData> _armorStatData = new Dictionary<Armor.Type, ArmorStatData>();

   // Holds the list of loaded helm data
   private Dictionary<Helm.Type, HelmStatData> _helmStatData = new Dictionary<Helm.Type, HelmStatData>();

   // Counts how many equipment set has been loaded
   private float loadCounter = 0;

   #endregion
}
