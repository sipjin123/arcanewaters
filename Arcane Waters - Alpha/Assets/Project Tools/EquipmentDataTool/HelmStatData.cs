using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System;

[Serializable]
public class HelmStatData : EquipmentStatData
{
   // Armor Type
   [XmlElement(Namespace = "HelmType")]
   public Helm.Type helmType = Helm.Type.None;

   // The defense of the helm
   public int helmBaseDefense;

   // The elemental resistance of the helm
   public int fireResist;
   public int waterResist;
   public int airResist;
   public int earthResist;
}

public class Helm
{
   public enum Type
   {
      None = 0,
      Berret = 1,
      Cap = 2,
      CowboyHat = 3,
      Hoodie = 4
   }
}