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
   // Helm Type
   [XmlElement(Namespace = "HelmType")]
   public int helmType = 0;

   // The defense of the helm
   public int helmBaseDefense;

   // The elemental resistance of the helm
   public int fireResist;
   public int waterResist;
   public int airResist;
   public int earthResist;

   // Item sql id assigned from the database
   public int itemSqlId = 0;

   public static Helm translateDataToHelm (HelmStatData helmStatData) {
      Helm newHeadgear = new Helm {
         id = helmStatData.itemSqlId,
         itemTypeId = helmStatData.helmType,
         itemName = helmStatData.equipmentName,
         itemDescription = helmStatData.equipmentDescription,
         category = Item.Category.Helm,
         iconPath = helmStatData.equipmentIconPath,
         data = serializeHelmStatData(helmStatData)
      };
      return newHeadgear;
   }

   public static string serializeHelmStatData (HelmStatData data) {
      XmlSerializer helmSerializer = new XmlSerializer(data.GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         helmSerializer.Serialize(writer, data);
      }
      string helmDataXML = sb.ToString();
      return helmDataXML;
   }

   public static HelmStatData getStatData (string data, int itemTypeId) {
      TextAsset newTextAsset = new TextAsset(data);

      if (data == "") {
         return null;
      }

      try {
         HelmStatData castedData = Util.xmlLoad<HelmStatData>(newTextAsset);
         return castedData;
      } catch {
         Debug.LogWarning("There is no Helm Data for: " + itemTypeId);
         return null;
      }
   }

   public static HelmStatData getDefaultData () {
      return new HelmStatData {
         helmType = 0,
         palette1 = "",
         palette2 = "",
      };
   }
}