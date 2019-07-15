using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class MaterialManager : MonoBehaviour {
   #region Public Variables

   // The various Materials that we use to recolor sprite layers
   public Material material_G;
   public Material material_G_B;
   public Material material_G_R;
   public Material material_R;
   public Material material_R_G;
   public Material material_flags;
   public Material noRecolorMaterial;

   // Self
   public static MaterialManager self;

   #endregion

   public void Awake () {
      self = this;

      // Body materials
      defineBodyMaterials();
      defineArmorMaterials();
      defineWeaponMaterials();
      defineCraftingIngredientMaterials();
      // Ship materials
      defineShipMaterials();
   }

    public void defineCraftingIngredientMaterials()
    {
        foreach (Gender.Type gender in Gender.getTypes())
        {
            ColorKey lizard_scale = new ColorKey(gender, CraftingIngredients.Type.Lizard_Scale);
            _materials[lizard_scale] = material_G_B;

            ColorKey lizard_claw = new ColorKey(gender, CraftingIngredients.Type.Lizard_Claw);
            _materials[lizard_claw] = material_G_R;

            ColorKey ore = new ColorKey(gender, CraftingIngredients.Type.Ore);
            _materials[ore] = material_G_R;

            ColorKey lumber = new ColorKey(gender, CraftingIngredients.Type.Lumber);
            _materials[lumber] = material_G_R;

            ColorKey flint = new ColorKey(gender, CraftingIngredients.Type.Flint);
            _materials[flint] = material_G_R;
        }
    }
    public void defineBodyMaterials () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         // Eyes
         ColorKey eyes = new ColorKey(gender, Layer.Eyes);
         _materials[eyes] = material_G;

         // Hair
         ColorKey hair = new ColorKey(gender, Layer.Hair);
         _materials[hair] = material_G_R;

         // Body
         ColorKey body = new ColorKey(gender, Layer.Body);
         _materials[body] = material_G;
      }
   }

   public void defineArmorMaterials () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         // Cloth
         ColorKey cloth = new ColorKey(gender, Armor.Type.Cloth);
         _materials[cloth] = material_G_B;

         // Leather
         ColorKey leather = new ColorKey(gender, Armor.Type.Leather);
         _materials[leather] = material_G;

         // Steel
         ColorKey steel = new ColorKey(gender, Armor.Type.Steel);
         _materials[steel] = material_G_R;

         // Sash
         ColorKey sash = new ColorKey(gender, Armor.Type.Sash);
         _materials[sash] = material_G_B;

         // Tunic
         ColorKey tunic = new ColorKey(gender, Armor.Type.Tunic);
         _materials[tunic] = material_G_B;

         // Posh
         ColorKey posh = new ColorKey(gender, Armor.Type.Posh);
         _materials[posh] = material_G_B;

         // Formal
         ColorKey formal = new ColorKey(gender, Armor.Type.Formal);
         _materials[formal] = material_G_B;

         // Casual
         ColorKey casual = new ColorKey(gender, Armor.Type.Casual);
         _materials[casual] = material_G_B;

         // Plate
         ColorKey plate = new ColorKey(gender, Armor.Type.Plate);
         _materials[plate] = material_G_B;

         // Wool
         ColorKey wool = new ColorKey(gender, Armor.Type.Wool);
         _materials[wool] = material_G_B;

         // Strapped
         ColorKey strapped = new ColorKey(gender, Armor.Type.Strapped);
         _materials[strapped] = material_G_B;
      }
   }

   public void defineWeaponMaterials () {
      foreach (Gender.Type gender in Gender.getTypes()) {
         foreach (Weapon.Type weaponType in Enum.GetValues(typeof(Weapon.Type))) {
            Material material = material_G_R;

            // Assign the recolor materials
            switch (weaponType) {
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
               case Weapon.Type.None:
                  material = noRecolorMaterial;
                  break;
               case Weapon.Type.Sword_Rune:
               case Weapon.Type.Lance_Steel:
               case Weapon.Type.Mace_Star:
               case Weapon.Type.Mace_Steel:
               case Weapon.Type.Staff_Mage:
                  material = material_G_R;
                  break;
               case Weapon.Type.Sword_Steel:
                  material = material_G;
                  break;
               default:
                  D.warning("No recolor material defined for: " + weaponType);
                  material = noRecolorMaterial;
                  break;
            }

            ColorKey colorKey = new ColorKey(gender, weaponType);
            _materials[colorKey] = material;
         }
      }
   }

   public void defineShipMaterials () {
      foreach (Ship.Type shipType in Enum.GetValues(typeof(Ship.Type))) {
         // Hulls
         ColorKey hullKey = new ColorKey(shipType, Layer.Hull);
         _materials[hullKey] = material_R;

         // Masts
         ColorKey mastKey = new ColorKey(shipType, Layer.Masts);
         _materials[mastKey] = material_R;

         // Sails
         ColorKey sailKey = new ColorKey(shipType, Layer.Sails);
         _materials[sailKey] = material_G;

         // Flags
         ColorKey flagKey = new ColorKey(shipType, Layer.Flags);
         _materials[flagKey] = material_flags;
      }
   }

   public Material get (ColorKey colorKey) {
      if (_materials.ContainsKey(colorKey)) {
         return _materials[colorKey];
      }

      return null;
   }

   public Material getGUIMaterial (ColorKey colorKey) {
      if (_materials.ContainsKey(colorKey)) {
         Material regularMaterial = _materials[colorKey];
         string materialName = regularMaterial.name.Replace("Material", "GUI");
         return Resources.Load<Material>("Materials/" + materialName);
      }

      return null;
   }

   public int getRecolorCount (ColorKey colorKey) {
      Material material = get(colorKey);

      if (material == noRecolorMaterial || material == null) {
         return 0;
      }

      int underscoreCount = material.name.Count(f => f == '_');

      return underscoreCount;
   }

   public bool hasTwoColors (Gender.Type gender, Armor.Type armorType) {
      ColorKey colorKey = new ColorKey(gender, armorType);

      if (!_materials.ContainsKey(colorKey)) {
         return false;
      }

      Material mat = _materials[colorKey];

      return (mat.name.Split('_').Length > 2);
   }

   public bool hasTwoColors (Gender.Type gender, HairLayer.Type hairType) {
      // Hair layer is kind of bending the rules right now...
      switch (hairType) {
         case HairLayer.Type.Female_Hair_2:
         case HairLayer.Type.Female_Hair_3:
         case HairLayer.Type.Female_Hair_5:
         case HairLayer.Type.Female_Hair_7:
         case HairLayer.Type.Male_Hair_3:
         case HairLayer.Type.Male_Hair_8:
            return true;
         default:
            return false;
      }
   }

   public bool hasTwoColors (Gender.Type gender, Layer layerType) {
      ColorKey colorKey = new ColorKey(gender, layerType);
      Material mat = _materials[colorKey];

      return (mat.name.Split('_').Length > 2);
   }

   #region Private Variables

   // A mapping of our Materials by ColorKey
   protected Dictionary<ColorKey, Material> _materials = new Dictionary<ColorKey, Material>();

   #endregion
}
