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
   [XmlIgnore]
   public int itemSqlID;
}