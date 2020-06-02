using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Helm : EquippableItem
{
   #region Public Variables

   #endregion

   public Helm () {
      this.itemTypeId = 0;
   }

#if IS_SERVER_BUILD

   public Helm (MySqlDataReader dataReader) {
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.id = DataUtil.getInt(dataReader, "itmId");
      this.category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.data = DataUtil.getString(dataReader, "itmData");

      // Defaults
      this.paletteName1 = DataUtil.getString(dataReader, "itmPalette1");
      this.paletteName2 = DataUtil.getString(dataReader, "itmPalette2");

      foreach (string kvp in this.data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         /*string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("color1".Equals(key)) {
            this.color1 = (ColorType) Convert.ToInt32(value);
         }*/
      }
   }

#endif

   public Helm (int id, int helmType, string paletteName1, string paletteName2) {
      this.category = Category.Helm;
      this.id = id;
      this.itemTypeId = helmType;
      this.count = 1;
      this.paletteName1 = paletteName1;
      this.paletteName2 = paletteName2;
      this.data = "";
   }

   public Helm (int id, int itemTypeId, string paletteName1, string paletteName2, string data, int count = 1) {
      this.category = Category.Helm;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteName1 = paletteName1;
      this.paletteName2 = paletteName2;
      this.data = data;
   }

   public Helm (int id, int helmType) {
      this.category = Category.Helm;
      this.id = id;
      this.count = 1;
      this.itemTypeId = helmType;
      this.paletteName1 = "";
      this.paletteName2 = "";
      this.data = "";
   }

   public override string getDescription () {
      if (getHelmData() == null) {
         return itemDescription;
      }

      return getHelmData().equipmentDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nHeadgear = <color=red>{5}</color>",
         "#" + colorHex, getName(), paletteName1, paletteName2, getDescription(), getHelmValue());
   }

   public override string getName () {
      return itemName;
   }

   public int getHelmValue () {
      if (getHelmData() != null) {
         return getHelmData().helmBaseDefense;
      }

      return 0;
   }

   public virtual float getDefense (Element element) {
      if (getHelmData() == null) {
         D.warning("Cannot get Defense, Headgear data does not exist! Go to Equipment Editor and make new data: (" + itemTypeId + ")");
         return 5;
      }

      switch (element) {
         case Element.Air:
            return getHelmData().airResist;
         case Element.Fire:
            return getHelmData().fireResist;
         case Element.Water:
            return getHelmData().waterResist;
         case Element.Earth:
            return getHelmData().earthResist;
      }
      return getHelmData().helmBaseDefense;
   }

   public static int getBaseDefense (int helmType) {
      HelmStatData helmData = EquipmentXMLManager.self.getHelmData(helmType);
      if (helmData == null) {
         D.warning("Cannot get Base Helm, Helm data does not exist! Go to Equipment Editor and make new data: (" + helmType + ")");
         return 5;
      }

      return helmData.helmBaseDefense;
   }

   public static float getDefenseModifier (Rarity.Type rarity) {
      float randomModifier = Util.getBellCurveFloat(1.0f, .1f, .90f, 1.10f);

      switch (rarity) {
         case Rarity.Type.Uncommon:
            return randomModifier * 1.2f;
         case Rarity.Type.Rare:
            return randomModifier * 1.5f;
         case Rarity.Type.Epic:
            return randomModifier * 1.5f;
         case Rarity.Type.Legendary:
            return randomModifier * 3f; ;
         default:
            return randomModifier;
      }
   }

   public override bool isEquipped () {
      if (Global.player == null || !(Global.player is BodyEntity)) {
         return false;
      }

      BodyEntity body = (BodyEntity) Global.player;

      return (body.helmManager.equippedHelmId == id);
   }

   public static string getName (int helmType) {
      if (helmType == 0) {
         return "None";
      }

      HelmStatData helmData = EquipmentXMLManager.self.getHelmData(helmType);
      if (helmData == null) {
         D.warning("Cannot get Name, Helm data does not exist! Go to Equipment Editor and make new data :: (" + helmType + ")");
         return "Undefined";
      }

      return helmData.equipmentName;
   }

   public static List<int> getTypes (bool includeNone = false) {
      List<int> list = new List<int>();

      // Cycle over all of the various Helm types
      for (int helmType = 0; helmType < EquipmentXMLManager.self.helmStatList.Count; helmType++) {
         // Only include the "None" type if it was specifically requested
         if (helmType == 0 && !includeNone) {
            continue;
         }

         list.Add(helmType);
      }

      return list;
   }

   public static Helm getEmpty () {
      return new Helm(0, 0, "", "");
   }

   public override string getIconPath () {
      if (getHelmData() == null) {
         return iconPath;
      }

      return getHelmData().equipmentIconPath;
   }

   public static Helm generateRandom (int itemId, int helmType) {
      // Decide what the rarity should be
      Rarity.Type rarity = Rarity.getRandom();

      // Alter the damage based on the rarity
      float baseDefense = getBaseDefense(helmType);
      int defenseValue = (int) (baseDefense * getDefenseModifier(rarity));

      // Alter the price based on the rarity
      int price = (int) (getBaseSellPrice(Category.Helm, (int) helmType) * Rarity.getItemShopPriceModifier(rarity));

      // Let's use nice numbers
      defenseValue = Util.roundToPrettyNumber(defenseValue);
      price = Util.roundToPrettyNumber(price);

      string data = string.Format("helm={0}, rarity={1}, price={2}", defenseValue, (int) rarity, price);
      int stockCount = Rarity.getRandomItemStockCount(rarity);
      Helm headgear = new Helm(itemId, helmType, PaletteDef.Armor.Black, PaletteDef.Armor.White, data, stockCount);

      return headgear;
   }

   public static Helm castItemToHelm (Item item) {
      Helm newHeadgear = new Helm {
         category = Category.Helm,
         itemTypeId = item.itemTypeId,
         id = item.id,
         iconPath = item.iconPath,
         itemDescription = item.itemDescription,
         itemName = item.itemName,
         data = item.data,
         paletteName1 = item.paletteName1,
         paletteName2 = item.paletteName2
      };

      return newHeadgear;
   }

   private HelmStatData getHelmData () {
      if (_helmStatData == null) {
         _helmStatData = HelmStatData.getStatData(data, itemTypeId);
      }

      return _helmStatData;
   }

   #region Private Variables

   // Cached helm data from xml data variable
   [XmlIgnore]
   private HelmStatData _helmStatData;

   #endregion
}