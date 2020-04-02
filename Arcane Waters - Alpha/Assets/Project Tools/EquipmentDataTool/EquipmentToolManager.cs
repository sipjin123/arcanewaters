using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class EquipmentToolManager : XmlDataToolManager {
   #region Public Variables

   // Holds the main scene for the equipment data
   public EquipmentToolScene equipmentDataScene;

   // Holds the path of the folder
   public const string FOLDER_PATH_WEAPON = "EquipmentStats/Weapons";
   public const string FOLDER_PATH_ARMOR = "EquipmentStats/Armors";
   public const string FOLDER_PATH_HELM = "EquipmentStats/Helms";

   // Holds the collection of user id that created the data entry
   public List<SQLEntryIDClass> weaponUserID = new List<SQLEntryIDClass>();
   public List<SQLEntryIDClass> armorUserID = new List<SQLEntryIDClass>();
   public List<SQLEntryIDClass> helmUserID = new List<SQLEntryIDClass>();

   // Self reference to the specific tool
   public static EquipmentToolManager equipmentToolSelf;

   public enum EquipmentType
   {
      None = 0,
      Weapon =  1,
      Armor = 2,
      Helm = 3,
   }

   #endregion

   private void Start () {
      self = this;
      equipmentToolSelf = this;
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public bool didUserCreateData (int entryID, EquipmentType equipmentType) {
      SQLEntryIDClass sqlEntry = new SQLEntryIDClass();
      switch (equipmentType) {
         case EquipmentType.Weapon:
            sqlEntry = weaponUserID.Find(_ => _.xmlID == entryID);
            break;
         case EquipmentType.Armor:
            sqlEntry = armorUserID.Find(_ => _.xmlID == entryID);
            break;
         case EquipmentType.Helm:
            sqlEntry = helmUserID.Find(_ => _.xmlID == entryID);
            break;
      }

      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      }

      return false;
   }

   #region Save 

   public void saveWeapon (WeaponStatData data, int xml_id, bool isEnabled) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, xml_id, EquipmentType.Weapon, data.equipmentName, isEnabled, data.equipmentID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void saveArmor (ArmorStatData data, int xml_id, bool isEnabled) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, xml_id, EquipmentType.Armor, data.equipmentName, isEnabled, data.equipmentID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void saveHelm (HelmStatData data, int xml_id, bool isEnabled) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, xml_id, EquipmentType.Helm, data.equipmentName, isEnabled, data.equipmentID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #endregion

   #region Delete

   public void deleteWeapon (int xml_id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteEquipmentXML(xml_id, EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteArmor (int xml_id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteEquipmentXML(xml_id, EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteHelm (int xml_id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteEquipmentXML(xml_id, EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #endregion

   #region Duplicate

   public void duplicateWeapon (WeaponStatData data) {
      data.weaponType = 0;
      data.equipmentName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, -1, EquipmentType.Weapon, data.equipmentName, false, data.equipmentID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateArmor (ArmorStatData data) {
      data.armorType = 0;
      data.equipmentName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, -1, EquipmentType.Armor, data.equipmentName, false, data.equipmentID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateHelm (HelmStatData data) {
      data.helmType = 0;
      data.equipmentName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateEquipmentXML(longString, -1, EquipmentType.Helm, data.equipmentName, false, data.equipmentID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #endregion

   #region Loading

   public void loadXMLData () {
      loadCounter = 0;
      userIdData = new List<SQLEntryIDClass>();
      XmlLoadingPanel.self.startLoading();
      loadWeapons();
      loadArmors();
      loadHelms();
   }

   private void loadWeapons () {
      _weaponStatData = new List<WeaponXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Weapon);
         weaponUserID = DB_Main.getSQLDataByID(editorToolType, EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(newTextAsset);

               // Save the data in the memory cache
               if (!_weaponStatData.Exists(_ => _.xml_id == xmlPair.xmlId)) {
                  WeaponXMLContent newXML = new WeaponXMLContent {
                     xml_id = xmlPair.xmlId,
                     weaponStatData = rawData,
                     isEnabled = xmlPair.isEnabled
                  };
                  _weaponStatData.Add(newXML);
               }
            }
            equipmentDataScene.loadWeaponData(_weaponStatData);
            finishLoading();
         });
      });
   }

   private void loadArmors () {
      _armorStatData = new List<ArmorXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Armor);
         armorUserID = DB_Main.getSQLDataByID(editorToolType, EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ArmorStatData rawData = Util.xmlLoad<ArmorStatData>(newTextAsset);

               // Save the data in the memory cache
               if (!_armorStatData.Exists(_ => _.xml_id == xmlPair.xmlId)) {
                  ArmorXMLContent newXML = new ArmorXMLContent {
                     xml_id = xmlPair.xmlId,
                     armorStatData = rawData,
                     isEnabled = xmlPair.isEnabled
                  };
                  _armorStatData.Add(newXML);
               }
            }
            equipmentDataScene.loadArmorData(_armorStatData);
            finishLoading();
         });
      });
   }

   private void loadHelms () {
      _helmStatData = new List<HelmXMLContent>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Helm);
         helmUserID = DB_Main.getSQLDataByID(editorToolType, EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               HelmStatData rawData = Util.xmlLoad<HelmStatData>(newTextAsset);
               rawData.equipmentID = (int) rawData.helmType;

               // Save the data in the memory cache
               if (!_helmStatData.Exists(_=>_.xml_id == xmlPair.xmlId)) {
                  HelmXMLContent newXML = new HelmXMLContent {
                     xml_id = xmlPair.xmlId,
                     helmStatData = rawData,
                     isEnabled = xmlPair.isEnabled
                  };
                  _helmStatData.Add(newXML);
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
         if (EquipmentXMLManager.self != null) {
            // Provide data to generic selection manager
            List<WeaponStatData> weaponStatDataList = new List<WeaponStatData>();
            List<ArmorStatData> armorStatDataList = new List<ArmorStatData>();
            List<HelmStatData> helmStatDataList = new List<HelmStatData>();

            foreach (WeaponXMLContent weaponContent in _weaponStatData) {
               weaponStatDataList.Add(weaponContent.weaponStatData);
            }
            foreach (ArmorXMLContent armorContent in _armorStatData) {
               armorStatDataList.Add(armorContent.armorStatData);
            }
            foreach (HelmXMLContent helmContent in _helmStatData) {
               helmStatDataList.Add(helmContent.helmStatData);
            }

            EquipmentXMLManager.self.resetAllData();
            EquipmentXMLManager.self.receiveWeaponDataFromZipData(weaponStatDataList);
            EquipmentXMLManager.self.receiveArmorDataFromZipData(armorStatDataList);
            EquipmentXMLManager.self.receiveHelmFromZipData(helmStatDataList);
         }

         XmlLoadingPanel.self.finishLoading();
         loadCounter = 0;
      }
   }

   #endregion

   #region Private Variables

   // Holds the list of loaded weapon data
   private List<WeaponXMLContent> _weaponStatData = new List<WeaponXMLContent>();

   // Holds the list of loaded armor data
   private List<ArmorXMLContent> _armorStatData = new List<ArmorXMLContent>();

   // Holds the list of loaded helm data
   private List<HelmXMLContent> _helmStatData = new List<HelmXMLContent>();

   // Counts how many equipment set has been loaded
   private float loadCounter = 0;

   #endregion
}

public class WeaponXMLContent
{
   // Id of the xml entry
   public int xml_id;

   // Data of the weapon content
   public WeaponStatData weaponStatData;

   // Determines if the entry is enabled in the sql database
   public bool isEnabled;
}
public class ArmorXMLContent
{
   // Id of the xml entry
   public int xml_id;

   // Data of the armor content
   public ArmorStatData armorStatData;

   // Determines if the entry is enabled in the sql database
   public bool isEnabled;
}
public class HelmXMLContent
{
   // Id of the xml entry
   public int xml_id;

   // Data of the helm content
   public HelmStatData helmStatData;

   // Determines if the entry is enabled in the sql database
   public bool isEnabled;
}