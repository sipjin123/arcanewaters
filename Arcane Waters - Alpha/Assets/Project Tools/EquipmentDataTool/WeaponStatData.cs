using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;

[Serializable]
public class WeaponStatData : EquipmentStatData
{
   // Weapon Type
   [XmlElement(Namespace = "WeaponType")]
   public int weaponType = 0;

   // The Weapon class
   [XmlElement(Namespace = "WeaponClass")]
   public Weapon.Class weaponClass = Weapon.Class.Any;

   [XmlElement(Namespace = "ProjectileSprite")]
   public string projectileSprite = "";

   // Directory of the action sfx
   [XmlElement(Namespace = "ActionSFX")]
   public string actionSfxDirectory = "";

   // SFX category for FMOD implementation
   public SoundEffectManager.WeaponType sfxType;

   // The damage of the weapon
   public int weaponBaseDamage = 0;

   // The elemental damage of the weapons
   public int weaponDamageFire;
   public int weaponDamageWater;
   public int weaponDamageAir;
   public int weaponDamageEarth;

   // The type of action that comes with the weapon
   public Weapon.ActionType actionType = Weapon.ActionType.None;

   // The generic value of the action type
   public int actionTypeValue = 0;

   // Sound Id, used for SFX
   [XmlElement(Namespace = "SoundId")]
   public int soundId = 0;

   public static Weapon translateDataToWeapon (WeaponStatData weaponData) {
      Weapon newWeapon = new Weapon {
         id = weaponData.sqlId,
         itemTypeId = weaponData.sqlId,
         itemName = weaponData.equipmentName,
         itemDescription = weaponData.equipmentDescription,
         category = Item.Category.Weapon,
         iconPath = weaponData.equipmentIconPath,
         actionTypeValue = weaponData.actionTypeValue,
         paletteNames = weaponData.palettes,
         data = serializeWeaponStatData(weaponData)
      };
      return newWeapon;
   }

   public static string serializeWeaponStatData (WeaponStatData data) {
      XmlSerializer weaponSerializer = new XmlSerializer(data.GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         weaponSerializer.Serialize(writer, data);
      }
      string weaponDataXML = sb.ToString();
      return weaponDataXML;
   }

   public static WeaponStatData getStatData (string data, int itemTypeId) {
      TextAsset newTextAsset = new TextAsset(data);

      if (data == "") {
         return null;
      }

      if (data.Length < 1) {
         D.debug("Invalid xml data for weapon type: " + itemTypeId + " " + data);
         return null;
      }

      if (!data.Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
         D.debug("Invalid xml data format for weapon type: " + itemTypeId + " " + data);
         return null;
      }

      try {
         WeaponStatData castedData = Util.xmlLoad<WeaponStatData>(newTextAsset);
         return castedData;
      } catch {
         Debug.LogWarning("There is no Weapon Data for: " + itemTypeId);
         return null;
      }
   }

   public static WeaponStatData getDefaultData () {
      return new WeaponStatData { 
         weaponType = 0,
         palettes = "",
         actionType = Weapon.ActionType.None,
      };
   }
}