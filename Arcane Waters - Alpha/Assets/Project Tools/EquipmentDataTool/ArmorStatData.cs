using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System;

[Serializable]
public class ArmorStatData : EquipmentStatData
{
   // Armor Type
   [XmlElement(Namespace = "ArmorType")]
   public int armorType = 0;
   
   // The defense of the armor
   public int armorBaseDefense;

   // The elemental defense of the armor
   public int fireResist;
   public int waterResist;
   public int airResist;
   public int earthResist;

   // Item sql id assigned from the database
   [XmlIgnore]
   public int itemSqlID;
}