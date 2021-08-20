using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System;

[Serializable]
public class HatStatData : EquipmentStatData
{
   // Hats Type
   [XmlElement(Namespace = "equipmentType")]
   public int hatType = 0;

   // The defense of the hat
   [XmlElement(Namespace = "DefenseValue")]
   public int hatBaseDefense;

   // The elemental resistance of the hat
   public int fireResist;
   public int waterResist;
   public int airResist;
   public int earthResist;

   public static Hat translateDataToHat (HatStatData hatStatData) {
      Hat newHat = new Hat {
         id = hatStatData.sqlId,
         itemTypeId = hatStatData.sqlId,
         itemName = hatStatData.equipmentName,
         itemDescription = hatStatData.equipmentDescription,
         category = Item.Category.Hats,
         iconPath = hatStatData.equipmentIconPath,
         data = serializeHatStatData(hatStatData)
      };
      return newHat;
   }

   public static string serializeHatStatData (HatStatData data) {
      XmlSerializer hatSerializer = new XmlSerializer(data.GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         hatSerializer.Serialize(writer, data);
      }
      string hatData = sb.ToString();
      return hatData;
   }

   public static HatStatData getStatData (string data, int itemTypeId) {
      TextAsset newTextAsset = new TextAsset(data);

      if (data == "") {
         return null;
      }

      try {
         HatStatData castedData = Util.xmlLoad<HatStatData>(newTextAsset);
         return castedData;
      } catch {
         Debug.LogWarning("There is no Hat Data for: " + itemTypeId);
         return null;
      }
   }

   public static HatStatData getDefaultData () {
      return new HatStatData {
         hatType = 0,
         palettes = ""
      };
   }
}