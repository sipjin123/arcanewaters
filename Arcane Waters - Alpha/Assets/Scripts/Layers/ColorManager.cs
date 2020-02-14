using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class ColorManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ColorManager self;

   #endregion

   void Awake () {
      // Keep track of the static color definitions by their string names, for convenience
      storeColorNames();
   }

   void Start () {
      self = this;

      defineHairColors();
      defineEyeColors();
      defineArmorColors();
      defineWeaponColors();
   }

   public void defineHairColors () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         ColorKey colorKey = new ColorKey(gender, Layer.Hair);
         HashSet<ColorType> primaries = new HashSet<ColorType>();
         HashSet<ColorType> secondaries = new HashSet<ColorType>();

         primaries = secondaries = new HashSet<ColorType>() {
            ColorType.Green, ColorType.Yellow, ColorType.Blue, ColorType.Red,
            ColorType.LightRed, ColorType.Pink, ColorType.LightPink, ColorType.Teal,
            ColorType.Orange, ColorType.DarkGreen, ColorType.LightPurple, ColorType.DarkPurple,
            ColorType.White, ColorType.Black
         };

         _primaries[colorKey] = primaries;
         _secondaries[colorKey] = secondaries;
      }
   }

   public void defineEyeColors () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         ColorKey colorKey = new ColorKey(gender, Layer.Eyes);
         HashSet<ColorType> primaries = new HashSet<ColorType>();
         HashSet<ColorType> secondaries = new HashSet<ColorType>();

         primaries = secondaries = new HashSet<ColorType>() {
            ColorType.Green, ColorType.Yellow, ColorType.Blue, ColorType.Red,
            ColorType.LightRed, ColorType.Pink, ColorType.LightPink, ColorType.Teal,
            ColorType.Orange, ColorType.DarkGreen, ColorType.LightPurple, ColorType.DarkPurple,
            ColorType.White, ColorType.Black
         };

         _primaries[colorKey] = primaries;
         _secondaries[colorKey] = secondaries;
      }
   }

   public void defineArmorColors () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         for (int armorType = 0; armorType < Armor.MAX_ARMOR_COUNT; armorType++) { 
            ColorKey colorKey = new ColorKey(gender, armorType);
            HashSet<ColorType> primaries = new HashSet<ColorType>();
            HashSet<ColorType> secondaries = new HashSet<ColorType>();

            // Define the list of colors for each type
            switch (armorType) {
               case 5:
                  primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.Green, ColorType.Yellow, ColorType.Blue, ColorType.Red,
                     ColorType.LightRed, ColorType.Pink, ColorType.LightPink, ColorType.Teal,
                     ColorType.Orange, ColorType.DarkGreen, ColorType.LightPurple, ColorType.DarkPurple,
                     ColorType.White, ColorType.Black
                  };
                  break;
               default:
                  primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.White
                  };
                  break;
            }

            _primaries[colorKey] = primaries;
            _secondaries[colorKey] = secondaries;
         }
      }
   }

   public void defineWeaponColors () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         foreach (Weapon.Type weaponType in Enum.GetValues(typeof(Weapon.Type))) {
            ColorKey colorKey = new ColorKey(gender, weaponType);
            HashSet<ColorType> primaries = new HashSet<ColorType>();
            HashSet<ColorType> secondaries = new HashSet<ColorType>();

            // Define the list of colors for each type
            switch (weaponType) {
               case Weapon.Type.Lance_Steel:
               case Weapon.Type.Mace_Star:
               case Weapon.Type.Mace_Steel:
               case Weapon.Type.Staff_Mage:
               case Weapon.Type.Sword_Rune:
               case Weapon.Type.Sword_Steel:
               case Weapon.Type.Sword_1:
               case Weapon.Type.Sword_2:
               case Weapon.Type.Sword_3:
               case Weapon.Type.Sword_4:
               case Weapon.Type.Sword_5:
               case Weapon.Type.Sword_6:
               case Weapon.Type.Sword_7:
               case Weapon.Type.Sword_8:
               case Weapon.Type.Gun_2:
               case Weapon.Type.Gun_3:
               case Weapon.Type.Gun_6:
               case Weapon.Type.Gun_7:
               case Weapon.Type.Seeds:
               case Weapon.Type.Pitchfork:
               case Weapon.Type.WateringPot:
                  primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.SteelBlue, ColorType.LightRed, ColorType.DarkRed, ColorType.LightPink, ColorType.DarkPurple,
                     ColorType.LightBlue, ColorType.DarkBlue, ColorType.LightGreen, ColorType.DarkGreen, ColorType.LightYellow,
                     ColorType.DarkYellow, ColorType.Teal,
                  };
                  break;
               default:
                  primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.White
                  };
                  break;
            }

            _primaries[colorKey] = primaries;
            _secondaries[colorKey] = secondaries;
         }
      }
   }

   public static ColorType getColor (string colorDefName) {
      return _colors[colorDefName];
   }

   public static ColorType getColor (Color color) {
      foreach (ColorType colorType in _colors.Values) {
         ColorDef colorDef = ColorDef.get(colorType);
         if (colorDef.color == color) {
            return colorType;
         }
      }

      return ColorType.None;
   }

   public HashSet<ColorType> getColors (ColorKey colorKey, bool isPrimary) {
      if (isPrimary) {
         return _primaries[colorKey];
      } else {
         return _secondaries[colorKey];
      }
   }

   protected void storeColorNames () {
      foreach (ColorType colorType in Enum.GetValues(typeof(ColorType))) {
         ColorDef colorDef = ColorDef.get(colorType);
         _colors.Add(colorDef.colorName, colorType);
      }
   }

   #region Private Variables

   // The Mapping of Color Keys to their set of Color Types
   protected Dictionary<ColorKey, HashSet<ColorType>> _primaries = new Dictionary<ColorKey, HashSet<ColorType>>();
   protected Dictionary<ColorKey, HashSet<ColorType>> _secondaries = new Dictionary<ColorKey, HashSet<ColorType>>();

   // A convenient mapping of colors by their string name
   protected static Dictionary<string, ColorType> _colors = new Dictionary<string, ColorType>();

   #endregion
}
