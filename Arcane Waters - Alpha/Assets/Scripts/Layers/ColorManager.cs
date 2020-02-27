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
      defineWeaponColors();

      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         defineArmorColors();
      });
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
         foreach (ArmorStatData armorData in EquipmentXMLManager.self.armorStatList) {
            ColorKey colorKey = new ColorKey(gender, "armor_" + armorData.equipmentID);
            HashSet<ColorType> primaries = new HashSet<ColorType>();
            HashSet<ColorType> secondaries = new HashSet<ColorType>();

            // Define the list of colors for each type
            if (armorData.setAllColors) {
               primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.Green, ColorType.Yellow, ColorType.Blue, ColorType.Red,
                     ColorType.LightRed, ColorType.Pink, ColorType.LightPink, ColorType.Teal,
                     ColorType.Orange, ColorType.DarkGreen, ColorType.LightPurple, ColorType.DarkPurple,
                     ColorType.White, ColorType.Black
                  };
            } else {
               primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.White
                  };
            }

            _primaries[colorKey] = primaries;
            _secondaries[colorKey] = secondaries;
         }
      }
   }

   public void defineWeaponColors () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         foreach (WeaponStatData weaponData in EquipmentXMLManager.self.weaponStatList) {
            ColorKey colorKey = new ColorKey(gender, "weapon_" + weaponData.equipmentID);
            HashSet<ColorType> primaries = new HashSet<ColorType>();
            HashSet<ColorType> secondaries = new HashSet<ColorType>();

            // Define the list of colors for each type
            if (weaponData.setAllColors) {
               primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.SteelBlue, ColorType.LightRed, ColorType.DarkRed, ColorType.LightPink, ColorType.DarkPurple,
                     ColorType.LightBlue, ColorType.DarkBlue, ColorType.LightGreen, ColorType.DarkGreen, ColorType.LightYellow,
                     ColorType.DarkYellow, ColorType.Teal,
                  };
            } else {
               primaries = secondaries = new HashSet<ColorType>() {
                     ColorType.White
                  };
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
