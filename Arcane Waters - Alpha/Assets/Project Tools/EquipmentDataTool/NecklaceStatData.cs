using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System;

[Serializable]
public class NecklaceStatData : EquipmentStatData {
   // Gear Type
   [XmlElement(Namespace = "NecklaceType")]
   public int necklaceType = 0;

   // The defense of the necklace
   public int necklaceBaseDefense;

   // The buff value if any
   public float itemBuffValue;

   // The type of buff this gear can offer
   public GearBuffType gearBuffType;

   // Determines if buff value is percentage or raw value
   public bool isBuffPercentage;

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

   public static Necklace translateDataToNecklace (NecklaceStatData data) {
      Necklace newNecklace = new Necklace {
         id = data.sqlId,
         itemTypeId = data.sqlId,
         itemName = data.equipmentName,
         itemDescription = data.equipmentDescription,
         category = Item.Category.Necklace,
         iconPath = data.equipmentIconPath,
         paletteNames = data.palettes,
         data = serializeNecklaceStatData(data)
      };
      return newNecklace;
   }

   public static string serializeNecklaceStatData (NecklaceStatData data) {
      XmlSerializer dataSerializer = new XmlSerializer(data.GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         dataSerializer.Serialize(writer, data);
      }
      string dataXML = sb.ToString();
      return dataXML;
   }

   public static NecklaceStatData getStatData (string data, int itemTypeId) {
      TextAsset newTextAsset = new TextAsset(data);

      if (data == "") {
         return null;
      }

      try {
         NecklaceStatData castedData = Util.xmlLoad<NecklaceStatData>(newTextAsset);
         return castedData;
      } catch {
         Debug.LogWarning("There is no Necklace Data for: " + itemTypeId);
         return null;
      }
   }

   public static NecklaceStatData getDefaultData () {
      return new NecklaceStatData {
         necklaceType = 0,
         palettes = ""
      };
   }
}