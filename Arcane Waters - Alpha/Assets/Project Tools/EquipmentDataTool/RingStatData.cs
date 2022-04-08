using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System;

[Serializable]
public class RingStatData : EquipmentStatData {
   // Gear Type
   [XmlElement(Namespace = "RingType")]
   public int ringType = 0;

   // The defense of the ring
   public int ringBaseDefense;

   // The elemental defense of the gear
   public int fireResist;
   public int waterResist;
   public int airResist;
   public int earthResist;
   public int physicalResist;

   // The various stats for this gear
   public int strength;
   public int precision;
   public int vitality;
   public int intelligence;
   public int spirit;
   public int luck;

   public static Ring translateDataToRing (RingStatData data) {
      Ring newRing = new Ring {
         id = data.sqlId,
         itemTypeId = data.sqlId,
         itemName = data.equipmentName,
         itemDescription = data.equipmentDescription,
         category = Item.Category.Ring,
         iconPath = data.equipmentIconPath,
         paletteNames = data.palettes,
         data = serializeRingStatData(data)
      };
      return newRing;
   }

   public static string serializeRingStatData (RingStatData data) {
      XmlSerializer dataSerializer = new XmlSerializer(data.GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         dataSerializer.Serialize(writer, data);
      }
      string dataXML = sb.ToString();
      return dataXML;
   }

   public static RingStatData getStatData (string data, int itemTypeId) {
      TextAsset newTextAsset = new TextAsset(data);

      if (data == "") {
         return null;
      }

      try {
         RingStatData castedData = Util.xmlLoad<RingStatData>(newTextAsset);
         return castedData;
      } catch {
         Debug.LogWarning("There is no Ring Data for: " + itemTypeId);
         return null;
      }
   }

   public static RingStatData getDefaultData () {
      return new RingStatData {
         ringType = 0,
         palettes = ""
      };
   }
}