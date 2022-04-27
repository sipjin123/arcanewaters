using System;
using UnityEngine;
using static PaletteToolManager;

[Serializable]
public class Dye : Item
{
   #region Public Variables

   #endregion

   public Dye () {

   }

   public Dye (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Dye;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public static Dye createFromData (int paletteTypeId, PaletteToolData data) {
      if (data == null) {
         return null;
      }

      Dye dye = new Dye(-1, paletteTypeId, "", "", 100);
      dye.setBasicInfo(data.paletteDisplayName, data.paletteDescription, string.Empty);
      return dye;
   }

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
      PaletteToolData palette = PaletteSwapManager.self.getPalette(itemTypeId);

      if (palette == null) {
         return null;
      }

      PaletteImageType dyeType = (PaletteImageType)palette.paletteType;

      if (dyeType == PaletteImageType.Armor || dyeType == PaletteImageType.Weapon || dyeType == PaletteImageType.Hair || dyeType == PaletteImageType.Hat) {
         return "Icons/Inventory/DyeIcons";
      }

      return "";
   }

   public override string getName () {
      PaletteToolData palette = PaletteSwapManager.self.getPalette(itemTypeId);

      if (palette == null) {
         return null;
      }

      return palette.paletteDisplayName;
   }

   public override string getDescription () {
      PaletteToolData palette = PaletteSwapManager.self.getPalette(itemTypeId);

      if (palette == null) {
         return null;
      }

      return palette.paletteDescription;
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
