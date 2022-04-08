using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System;

[Serializable]
public class TrinketStatData : EquipmentStatData {
   // Gear Type
   [XmlElement(Namespace = "TrinketType")]
   public int trinketType = 0;

   // The defense of the trinket
   public int trinketBaseDefense;

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

   public static Trinket translateDataToTrinket (TrinketStatData data) {
      Trinket newTrinket = new Trinket {
         id = data.sqlId,
         itemTypeId = data.sqlId,
         itemName = data.equipmentName,
         itemDescription = data.equipmentDescription,
         category = Item.Category.Trinket,
         iconPath = data.equipmentIconPath,
         paletteNames = data.palettes,
         data = serializeTrinketStatData(data)
      };
      return newTrinket;
   }

   public static string serializeTrinketStatData (TrinketStatData data) {
      XmlSerializer dataSerializer = new XmlSerializer(data.GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         dataSerializer.Serialize(writer, data);
      }
      string dataXML = sb.ToString();
      return dataXML;
   }

   public static TrinketStatData getStatData (string data, int itemTypeId) {
      TextAsset newTextAsset = new TextAsset(data);

      if (data == "") {
         return null;
      }

      try {
         TrinketStatData castedData = Util.xmlLoad<TrinketStatData>(newTextAsset);
         return castedData;
      } catch {
         Debug.LogWarning("There is no Trinket Data for: " + itemTypeId);
         return null;
      }
   }

   public static TrinketStatData getDefaultData () {
      return new TrinketStatData {
         trinketType = 0,
         palettes = ""
      };
   }
}