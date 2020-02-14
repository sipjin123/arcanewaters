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

   // References to all the helm data
   public List<HelmStatData> helmStatList { get { return _helmStatList.Values.ToList(); } }

   // Display list for editor reviewing
   public List<WeaponStatData> weaponStatData = new List<WeaponStatData>();
   public List<ArmorStatData> armorStatData = new List<ArmorStatData>();
   public List<HelmStatData> helmStatData = new List<HelmStatData>();

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

   public WeaponStatData getWeaponData (Weapon.Type weaponType) {
      if (_weaponStatList == null) {
         return null;
      }
      if (_weaponStatList.ContainsKey(weaponType)) {
         return _weaponStatList[weaponType];
      }
      return null;
   }

   public ArmorStatData getArmorData (int armorType) {
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
         loadedAllEquipment = true;
         if (Global.player != null) { 
            Global.player.admin.buildItemNamesDictionary();
         }
         finishedDataSetup.Invoke();
         equipmentLoadCounter = 0;
      }
   }

   public void initializeDataCache () {
      equipmentLoadCounter = 0;
      _weaponStatList = new Dictionary<Weapon.Type, WeaponStatData>();
      _armorStatList = new Dictionary<int, ArmorStatData>();
      _helmStatList = new Dictionary<Helm.Type, HelmStatData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Weapon);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               WeaponStatData rawData = Util.xmlLoad<WeaponStatData>(newTextAsset);
               Weapon.Type uniqueID = rawData.weaponType;

               // Save the data in the memory cache
               if (!_weaponStatList.ContainsKey(uniqueID) && xmlPair.isEnabled) {
                  _weaponStatList.Add(uniqueID, rawData);
                  weaponStatData.Add(rawData);
               }
            }
            finishedLoading();
         });
      });

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Armor);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ArmorStatData rawData = Util.xmlLoad<ArmorStatData>(newTextAsset);
               int uniqueID = rawData.armorType;

               // Save the data in the memory cache
               if (!_armorStatList.ContainsKey(uniqueID) && xmlPair.isEnabled) {
                  _armorStatList.Add(uniqueID, rawData);
                  armorStatData.Add(rawData);
               }
            }
            finishedLoading();
         });
      });

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getEquipmentXML(EquipmentType.Helm);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               HelmStatData rawData = Util.xmlLoad<HelmStatData>(newTextAsset);
               Helm.Type uniqueID = rawData.helmType;

               // Save the data in the memory cache
               if (!_helmStatList.ContainsKey(uniqueID) && xmlPair.isEnabled) {
                  _helmStatList.Add(uniqueID, rawData);
                  helmStatData.Add(rawData);
               }
            }
            finishedLoading();
         });
      });
   }

   public void receiveWeaponDataFromServer (List<WeaponStatData> statData) {
      foreach (WeaponStatData rawData in statData) {
         Weapon.Type uniqueID = rawData.weaponType;
         // Save the data in the memory cache
         if (!_weaponStatList.ContainsKey(uniqueID)) {
            _weaponStatList.Add(uniqueID, rawData);
            weaponStatData.Add(rawData);
         }
      }
      finishedLoading();
   }

   public void receiveArmorDataFromServer (List<ArmorStatData> statData) {
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

   public void receiveHelmDataFromServer (List<HelmStatData> statData) {
      foreach (HelmStatData rawData in statData) {
         Helm.Type uniqueID = rawData.helmType;
         // Save the data in the memory cache
         if (!_helmStatList.ContainsKey(uniqueID)) {
            _helmStatList.Add(uniqueID, rawData);
            helmStatData.Add(rawData);
         }
      }
      finishedLoading();
   }

   #region Private Variables

   // Stores the list of all weapon data
   private Dictionary<Weapon.Type, WeaponStatData> _weaponStatList = new Dictionary<Weapon.Type, WeaponStatData>();

   // Stores the list of all armor data
   private Dictionary<int, ArmorStatData> _armorStatList = new Dictionary<int, ArmorStatData>();

   // Stores the list of all helm data
   private Dictionary<Helm.Type, HelmStatData> _helmStatList = new Dictionary<Helm.Type, HelmStatData>();

   #endregion
}
