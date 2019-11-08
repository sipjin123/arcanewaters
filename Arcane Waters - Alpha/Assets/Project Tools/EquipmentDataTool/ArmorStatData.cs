using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;

public class ArmorStatData : EquipmentStatData
{
   // Armor Type
   [XmlElement(Namespace = "ArmorType")]
   public Armor.Type armorType = Armor.Type.None;
   
   // The defense of the armor
   public int armorBaseDefense;
}