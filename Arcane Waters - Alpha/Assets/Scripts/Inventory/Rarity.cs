using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Rarity : MonoBehaviour {
   #region Public Variables

   // The Type of Rarity
   public enum Type {  None = 0, Common = 1, Uncommon = 2, Rare = 3, Epic = 4, Legendary = 5 }

   // The paths to the rarity star images
   public static string BACKGROUND_STAR_PATH = "Sprites/GUI/Shop/star_base_brown";
   public static string BRONZE_STAR_PATH = "Sprites/GUI/Shop/star_bronze_brown";
   public static string SILVER_STAR_PATH = "Sprites/GUI/Shop/star_silver_brown";
   public static string DIAMOND_STAR_PATH = "Sprites/GUI/Shop/star_diamond_brown";

   #endregion

   public static Color getColor (Type rarityType) {
      switch (rarityType) {
         case Type.Common:
            return Util.getColor(140, 140, 140);
         case Type.Uncommon:
            return Color.green;
         case Type.Rare:
            return Color.cyan;
         case Type.Epic:
            return Util.getColor(255, 159, 6);
         case Type.Legendary:
            return Color.magenta;
      }

      return Color.white;
   }

   public static Type getRandom () {
      // Decide what the rarity chances should be
      List<WeightedItem<Type>> rarities = new List<WeightedItem<Type>>() {
         WeightedItem.Create(.47f, Type.Common),
         WeightedItem.Create(.32f, Type.Uncommon),
         WeightedItem.Create(.16f, Type.Rare),
         WeightedItem.Create(.04f, Type.Epic),
         WeightedItem.Create(.01f, Type.Legendary),
      };

      return rarities.ChooseByRandom();
   }

   public static float getItemShopPriceModifier (Type rarityType) {
      switch (rarityType) {
         case Type.Uncommon:
            return Util.getBellCurveFloat(1.5f, .1f, .50f, 2.0f);
         case Type.Rare:
            return Util.getBellCurveFloat(3.0f, .2f, .50f, 5.0f);
         case Type.Epic:
            return Util.getBellCurveFloat(10.0f, 1.0f, 2f, 20f);
         case Type.Legendary:
            return Util.getBellCurveFloat(50.0f, 5.0f, 10f, 100f);
         default:
            return Util.getBellCurveFloat(1.0f, .1f, .50f, 1.50f);
      }
   }

   public static float getCropSellPriceModifier (Type rarityType) {
      float returnPrice = 0;

      // Represents percantage multiplier relative to the base price set in web tool, legendary items will display the max value set in web tool
      switch (rarityType) {
         case Type.Uncommon:
            returnPrice = .5f;
            break;
         case Type.Rare:
            returnPrice = .75f;
            break;
         case Type.Epic:
            returnPrice = .9f;
            break;
         case Type.Legendary:
            returnPrice = 1;
            break;
         default:
            returnPrice = .35f;
            break;
      }

      return returnPrice;
   }

   public static float getXPModifier (Type rarityType) {
      switch (rarityType) {
         case Type.Uncommon:
            return Util.getBellCurveFloat(1.5f, .1f, .50f, 2.0f);
         case Type.Rare:
            return Util.getBellCurveFloat(3.0f, .2f, .50f, 5.0f);
         case Type.Epic:
            return Util.getBellCurveFloat(10.0f, 1.0f, 2f, 20f);
         case Type.Legendary:
            return Util.getBellCurveFloat(50.0f, 5.0f, 10f, 100f);
         default:
            return Util.getBellCurveFloat(1.0f, .1f, .50f, 1.50f);
      }
   }

   public static int getRandomItemStockCount (Type rarityType) {
      switch (rarityType) {
         case Type.Common:
            return Util.getBellCurveInt(50, 10, 1, 100);
         case Type.Uncommon:
            return Util.getBellCurveInt(20, 2, 1, 100);
         case Type.Rare:
            return Util.getBellCurveInt(10, 1, 1, 20);
         case Type.Epic:
            return Util.getBellCurveInt(6, 1, 1, 10);
         case Type.Legendary:
            return Util.getBellCurveInt(3, 1, 1, 5);
         default:
            return 1;
      }
   }

   public static float getIncreasingModifier (Type rarity) {
      // Set our base mean and standard deviation
      float mean = 1.00f;
      float stdDev = .05f;
      
      // Adjust the mean for the different rarities
      switch (rarity) {
         case Type.Uncommon:
            mean = 1.10f;
            break;
         case Type.Rare:
            mean = 1.15f;
            break;
         case Type.Epic:
            mean = 1.30f;
            break;
         case Type.Legendary:
            mean = 1.50f;
            break;
      }

      float min = mean - stdDev * 6;
      float max = mean + stdDev * 6;

      return Util.getBellCurveFloat(mean, stdDev, min, max);
   }

   public static float getDecreasingModifier (Type rarity) {
      // Set our base mean and standard deviation
      float mean = 1.00f;
      float stdDev = .05f;

      // Adjust the mean for the different rarities
      switch (rarity) {
         case Type.Uncommon:
            mean = .90f;
            break;
         case Type.Rare:
            mean = .80f;
            break;
         case Type.Epic:
            mean = .70f;
            break;
         case Type.Legendary:
            mean = .50f;
            break;
      }

      float min = mean - stdDev * 6;
      float max = mean + stdDev * 6;

      return Util.getBellCurveFloat(mean, stdDev, min, max);
   }

   public static Sprite[] getRarityStars (Type rarity) {
      // Load the sprites if not already done
      if (_backgroundStar == null) {
         _backgroundStar = ImageManager.getSprite(BACKGROUND_STAR_PATH);
         _bronzeStar = ImageManager.getSprite(BRONZE_STAR_PATH);
         _silverStar = ImageManager.getSprite(SILVER_STAR_PATH);
         _diamondStar = ImageManager.getSprite(DIAMOND_STAR_PATH);
      }

      switch (rarity) {
         case Rarity.Type.None:
            return new Sprite[] { _backgroundStar, _backgroundStar, _backgroundStar };
         case Rarity.Type.Common:
            return new Sprite[] { _bronzeStar, _backgroundStar, _backgroundStar };
         case Rarity.Type.Uncommon:
            return new Sprite[] { _bronzeStar, _bronzeStar, _backgroundStar };
         case Rarity.Type.Rare:
            return new Sprite[] { _bronzeStar, _bronzeStar, _bronzeStar };
         case Rarity.Type.Epic:
            return new Sprite[] { _silverStar, _silverStar, _silverStar };
         case Rarity.Type.Legendary:
            return new Sprite[] { _diamondStar, _diamondStar, _diamondStar };
         default:
            return new Sprite[] { _backgroundStar, _backgroundStar, _backgroundStar };
      }
   }

   #region Private Variables

   // The references to the rarity star sprites
   private static Sprite _backgroundStar = null;
   private static Sprite _bronzeStar = null;
   private static Sprite _silverStar = null;
   private static Sprite _diamondStar = null;

   #endregion
}
