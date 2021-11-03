using System;
using UnityEngine;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Haircut : Item {
   #region Public Variables

   #endregion

   public Haircut () {

   }

   public Haircut (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Haircut;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public static Haircut createFromData(HaircutData data) {
      if (data == null) {
         return null;
      }

      Haircut haircut = new Haircut(-1, data.itemID, "", "", 100);
      haircut.setBasicInfo(data.itemName, data.itemDescription, data.itemIconPath);
      return haircut;
   }

   #if IS_SERVER_BUILD

   public static Haircut create (MySqlDataReader reader) {
      Haircut haircutItem = new Haircut();
      haircutItem.category = Category.Haircut;
      haircutItem.id = DataUtil.getInt(reader, "hcId");
      haircutItem.itemName = DataUtil.getString(reader, "hcName");
      haircutItem.itemDescription = DataUtil.getString(reader, "hcDescription");
      haircutItem.itemTypeId = DataUtil.getInt(reader, "hcType");

      // Gender is handled as metadata
      return haircutItem;
   }

   #endif

   public override bool canBeUsed () {
      return true;
   }

   public override bool canBeTrashed () {
      return true;
   }

   public override bool canBeEquipped () {
      return false;
   }

   public override bool canBeStacked () {
      return true;
   }

   public override string getIconPath () {
      return "Icons/Inventory/usables_icons";
   }

   public override string getName () {
      HaircutData haircutData = HaircutXMLManager.self.getHaircutData(itemTypeId);

      if (haircutData == null) {
         return this.itemName;
      }

      return haircutData.itemName;
   }

   public override string getDescription () {
      HaircutData haircutData = HaircutXMLManager.self.getHaircutData(itemTypeId);

      if (haircutData == null) {
         return this.itemDescription;
      }

      return haircutData.itemDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(Rarity.Type.None);
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color>\n\n{2}\n\n",
         "#" + colorHex, getName(), getDescription());
   }

   #region Private Variables

   #endregion
}
