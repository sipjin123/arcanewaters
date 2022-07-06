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

   public static Armor translateDataToArmor (ArmorStatData armorData) {
      Armor newArmor = new Armor {
         id = armorData.sqlId,
         itemTypeId = armorData.sqlId,
         itemName = armorData.equipmentName,
         itemDescription = armorData.equipmentDescription,
         category = Item.Category.Armor,
         iconPath = armorData.equipmentIconPath,
         paletteNames = PaletteSwapManager.extractPalettes(armorData.defaultPalettes),
         data = serializeArmorStatData(armorData)
      };
      return newArmor;
   }

   public static string serializeArmorStatData (ArmorStatData data) {
      XmlSerializer armorSerializer = new XmlSerializer(data.GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         armorSerializer.Serialize(writer, data);
      }
      string armorDataXML = sb.ToString();
      return armorDataXML;
   }

   public static ArmorStatData getStatData (string data, int itemTypeId) {
      TextAsset newTextAsset = new TextAsset(data);

      if (data == "") {
         return null;
      }

      try {
         ArmorStatData castedData = Util.xmlLoad<ArmorStatData>(newTextAsset);
         return castedData;
      } catch {
         Debug.LogWarning("There is no Armor Data for: " + itemTypeId);
         return null;
      }
   }

   public static ArmorStatData getDefaultData () {
      return new ArmorStatData {
         armorType = 0,
         //palettes = ""
      };
   }
}