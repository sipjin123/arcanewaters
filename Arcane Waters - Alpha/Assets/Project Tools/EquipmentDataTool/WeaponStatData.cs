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
   public Weapon.Type weaponType = Weapon.Type.None;

   // The Weapon class
   [XmlElement(Namespace = "WeaponClass")]
   public Weapon.Class weaponClass = Weapon.Class.Any;
   
   // The damage of the weapon
   public int weaponBaseDamage;
}