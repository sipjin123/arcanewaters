using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class EquipmentXMLManager : MonoBehaviour {
   #region Public Variables

   // A convenient self reference
   public static EquipmentXMLManager self;

   // Holds the xml data for all data
   public TextAsset[] weaponDataAssets;
   public TextAsset[] armorDataAssets;
   public TextAsset[] helmDataAssets;

   // References to all the weapon data
   public List<WeaponStatData> weaponStatList { get { return _weaponStatList.Values.ToList(); } }

   // References to all the armor data
   public List<ArmorStatData> armorStatList { get { return _armorStatList.Values.ToList(); } }

   #endregion

   private void Awake () {
      self = this;
      initXML();
   }

   private void initXML () {
      foreach (TextAsset textAsset in weaponDataAssets) {
         // Read and deserialize the file
         WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(textAsset);
         _weaponStatList.Add(weaponData.weaponType, weaponData);
      }
      foreach (TextAsset textAsset in armorDataAssets) {
         // Read and deserialize the file
         ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(textAsset);
         _armorStatList.Add(armorData.armorType, armorData);
      }
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

   #region Private Variables

   // Stores the list of all weapon data
   private Dictionary<Weapon.Type, WeaponStatData> _weaponStatList = new Dictionary<Weapon.Type, WeaponStatData>();

   // Stores the list of all armor data
   private Dictionary<Armor.Type, ArmorStatData> _armorStatList = new Dictionary<Armor.Type, ArmorStatData>();

   #endregion
}
