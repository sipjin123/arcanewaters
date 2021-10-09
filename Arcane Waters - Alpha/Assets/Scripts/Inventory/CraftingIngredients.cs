using UnityEngine;
using System;
using System.Collections.Generic;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

#endif

[Serializable]
public class CraftingIngredients : RecipeItem
{
   #region Public Variables

   // The Type
   public enum Type
   {
      None = 0, Gold_Ore = 1, Silver_Ore = 2, Iron_Ore = 3, Coal = 4, Leather = 5, Wood = 6, Heavy_Cloth = 7, Light_Cloth = 8, Silk = 9, Fur = 10, Onyx = 11,
      Lizard_Claw = 12, Green_Scale = 13, Molted_Skin = 14, Broken_Fang = 15, Green_Blood_Droplet = 16,
      Chitin = 17, Mandible = 18, Carapace = 19, Egg_Sac = 20, Bug_Juice = 21,
      Ectoplasm = 22, Brimstone = 23, Spectral_Ash = 24, Luminous_Powder = 25, Eldritch_Aura = 26,
      Bark = 27, Thorn = 28, Wood_Louse = 29, Polypore = 30, Sap = 31,
      Spores = 32, Toadstool_Cap = 33, Fungal_Chunk = 34, Mycelium_Fiber = 35, Grey_Slime = 36, Rib_Bone = 37, Grave_Dirt = 38
   }

   // The type
   public Type type;

   #endregion Public Variables

   public CraftingIngredients () {
      this.type = Type.None;
   }

#if IS_SERVER_BUILD

   public CraftingIngredients (MySqlDataReader dataReader) {
      this.type = (CraftingIngredients.Type) DataUtil.getInt(dataReader, "itmType");
      this.id = DataUtil.getInt(dataReader, "itmId");
      this.category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.data = DataUtil.getString(dataReader, "itmData");

      // Defaults
      this.paletteNames = DataUtil.getString(dataReader, "itmPalettes");

      foreach (string kvp in this.data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }
      }
   }

#endif

   public CraftingIngredients (int id, CraftingIngredients.Type recipeType, string newPalettes) {
      this.category = Category.CraftingIngredients;
      this.id = id;
      this.type = recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.paletteNames = newPalettes;
      this.data = "";
   }

   public CraftingIngredients (int id, int itemTypeId, string newPalettes, string data, int count = 1) {
      this.category = Category.CraftingIngredients;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.type = (Type) itemTypeId;
      this.paletteNames = newPalettes;
      this.data = data;
   }

   public override string getDescription () {
      switch (type) {
         // Lizard Drops
         case Type.Green_Scale:
            return "A Green Scale";

         case Type.Lizard_Claw:
            return "A Claw good for crafting";

         case Type.Broken_Fang:
            return "A Strange Fang";

         case Type.Green_Blood_Droplet:
            return "A Rare blood droplet";

         case Type.Molted_Skin:
            return "Molted Skin ";

         // Wood Guy Drops
         case Type.Bark:
            return "Strong bark good for ships";

         case Type.Thorn:
            return "A Pointy Thorn";

         case Type.Wood_Louse:
            return "A Wood Louse";

         case Type.Sap:
            return "Sap good for crafting";

         case Type.Polypore:
            return "A Polypore";

         // Insect Drops
         case Type.Chitin:
            return "Insect Chitin";

         case Type.Mandible:
            return "Mandible of a giant insect";

         case Type.Carapace:
            return "Tough carapace good for armor";

         case Type.Egg_Sac:
            return "An Egg Sac, very rare";

         case Type.Bug_Juice:
            return "Yummy bug juice";

         // Wisp Drops
         case Type.Ectoplasm:
            return "A rare ectoplasm";

         case Type.Brimstone:
            return "Shiny Brimstone";

         case Type.Spectral_Ash:
            return "Ash used for healing";

         case Type.Luminous_Powder:
            return "Powder with magical powers";

         case Type.Eldritch_Aura:
            return "A very rare essence, good for upgrading";

         // Musroom Drops
         case Type.Spores:
            return "Sprores dropped by Mushroom";

         case Type.Toadstool_Cap:
            return "Cap of toadstool";

         case Type.Fungal_Chunk:
            return "A Fungal Chunk";

         case Type.Mycelium_Fiber:
            return "A Mycelium Fiber";

         case Type.Grey_Slime:
            return "Slimy material";

         // Mineable Loots
         case Type.Coal:
            return "Coal good for cooking";

         case Type.Iron_Ore:
            return "An Iron Ore";

         case Type.Silver_Ore:
            return "A Silver Ore";

         case Type.Gold_Ore:
            return "A Golden Ore";

         // General Loots
         case Type.Fur:
            return "Some Fur";

         case Type.Heavy_Cloth:
            return "A Heavy Cloth";

         case Type.Leather:
            return "A Leather Cloth";

         case Type.Light_Cloth:
            return "A Light Cloth";

         case Type.Onyx:
            return "An Onyx Gem";

         case Type.Silk:
            return "A Silk Cloth";

         case Type.Wood:
            return "A piece of Wood";

         // Skeleton Loot
         case Type.Rib_Bone:
            return "A skeleton's rib bone.";

         case Type.Grave_Dirt:
            return "Dirt from the cemetery.";

         default:
            return "";
      }
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      string palettes = Item.trimItmPalette(paletteNames);

      return string.Format("<color={0}>{1}</color> (" + palettes + ")\n\n{2}",
         "#" + colorHex, getName(), getDescription());
   }

   public override string getName () {
      return getName(type);
   }

   public static string getName (CraftingIngredients.Type recipeType) {
      switch (recipeType) {
         // Lizard Drops
         case Type.Green_Scale:
            return "Green Scale";

         case Type.Lizard_Claw:
            return "Lizard Claw";

         case Type.Broken_Fang:
            return "Broken Fang";

         case Type.Green_Blood_Droplet:
            return "Green Blood Droplet";

         case Type.Molted_Skin:
            return "Molted Skin ";

         // Wood Guy Drops
         case Type.Bark:
            return "Bark";

         case Type.Thorn:
            return "Thorn";

         case Type.Wood_Louse:
            return "Wood Louse";

         case Type.Sap:
            return "Sap";

         case Type.Polypore:
            return "Polypore";

         // Insect Drops
         case Type.Chitin:
            return "Chitin";

         case Type.Mandible:
            return "Mandible";

         case Type.Carapace:
            return "Carapace";

         case Type.Egg_Sac:
            return "Egg Sac";

         case Type.Bug_Juice:
            return "Bug Juice";

         // Wisp Drops
         case Type.Ectoplasm:
            return "Ectoplasm";

         case Type.Brimstone:
            return "Brimstone";

         case Type.Spectral_Ash:
            return "Spectral Ash";

         case Type.Luminous_Powder:
            return "Glow Powder";

         case Type.Eldritch_Aura:
            return "Eldritch Aura";

         // Musroom Drops
         case Type.Spores:
            return "Sprores";

         case Type.Toadstool_Cap:
            return "Toadstool Cap";

         case Type.Fungal_Chunk:
            return "Fungal Chunk";

         case Type.Mycelium_Fiber:
            return "Mycelium Fiber";

         case Type.Grey_Slime:
            return "Grey Slime";

         // Mineable Loots
         case Type.Coal:
            return "Coal";

         case Type.Iron_Ore:
            return "Iron Ore";

         case Type.Silver_Ore:
            return "Silver Ore";

         case Type.Gold_Ore:
            return "Golden Ore";

         // General Loots
         case Type.Fur:
            return "Fur";

         case Type.Heavy_Cloth:
            return "Heavy Cloth";

         case Type.Leather:
            return "Leather Cloth";

         case Type.Light_Cloth:
            return "Light Cloth";

         case Type.Onyx:
            return "Onyx";

         case Type.Silk:
            return "Silk Cloth";

         case Type.Wood:
            return "Wood";

         // Skeleton Loot
         case Type.Rib_Bone:
            return "Rib Bone";

         case Type.Grave_Dirt:
            return "Grave Dirt";

         default:
            return "";
      }
   }

   public static CraftingIngredients getEmpty () {
      return new CraftingIngredients(0, CraftingIngredients.Type.None, "");
   }

   public override bool canBeTrashed () {
      switch (this.type) {
         default:
            return base.canBeTrashed();
      }
   }

   public override string getIconPath () {
      return "Icons/CraftingIngredients/ingredient_" + this.type;
   }

   public static string getIconPath (CraftingIngredients.Type craftingIngredientType) {
      return "Icons/CraftingIngredients/ingredient_" + craftingIngredientType;
   }

   public override string getBorderlessIconPath () {
      return getBorderlessIconPath(this.type);
   }

   public static string getBorderlessIconPath (CraftingIngredients.Type craftingIngredientType) {
      return "Icons/NoBorderCraftingIngredients/ingredient_" + craftingIngredientType;
   }
}