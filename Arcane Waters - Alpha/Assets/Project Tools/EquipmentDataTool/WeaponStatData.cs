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

   // The damage of the weapon
   public int weaponBaseDamage = 0;

   // The elemental damage of the weapons
   public int weaponDamageFire;
   public int weaponDamageWater;
   public int weaponDamageAir;
   public int weaponDamageEarth;

   // The type of action that comes with the weapon
   public Weapon.ActionType actionType = Weapon.ActionType.None;

   // Item sql id assigned from the database
   public int itemSqlId = 0;

   public static Weapon translateDataToWeapon (WeaponStatData weaponData) {
      Weapon newWeapon = new Weapon {
         id = weaponData.itemSqlId,
         itemTypeId = weaponData.weaponType,
         itemName = weaponData.equipmentName,
         itemDescription = weaponData.equipmentDescription,
         category = Item.Category.Weapon,
         iconPath = weaponData.equipmentIconPath,
         materialType = weaponData.materialType,
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
         color1 = ColorType.None,
         color2 = ColorType.None,
         actionType = Weapon.ActionType.None,
      };
   }
}