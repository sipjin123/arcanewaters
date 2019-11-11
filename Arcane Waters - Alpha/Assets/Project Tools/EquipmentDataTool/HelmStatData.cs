﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;

public class HelmStatData : EquipmentStatData
{
   // Armor Type
   [XmlElement(Namespace = "HelmType")]
   public Helm.Type helmType = Helm.Type.None;

   // The defense of the helm
   public int helmBaseDefense;
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