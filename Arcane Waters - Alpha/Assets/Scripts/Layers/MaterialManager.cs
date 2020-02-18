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
      defineWeaponMaterials();

      // Ship materials
      defineShipMaterials();
   }

   private void Start () {
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         defineArmorMaterials();
      });
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
         foreach (ArmorStatData armorData in EquipmentXMLManager.self.armorStatList) {
            // Cloth
            ColorKey cloth = new ColorKey(gender, "armor_" + armorData.armorType);
            _materials[cloth] = translateMaterial(armorData.materialType);
         }
      }
   }

   public void insertArmorMaterial (Gender.Type gender, int armorID, MaterialType materialType) {
      // Cloth
      ColorKey cloth = new ColorKey(gender, "armor_" + armorID);
      _materials[cloth] = translateMaterial(materialType);
   }

   private Material translateMaterial (MaterialType materialType) {
      switch (materialType) {
         case MaterialType.Material_G:
            return material_G;
         case MaterialType.Material_G_B:
            return material_G_B;
         case MaterialType.Material_G_R:
            return material_G_R;
         case MaterialType.Material_R:
            return material_R;
         case MaterialType.Material_R_G:
            return material_R_G;
         case MaterialType.Material_flags:
            return material_flags;
         default:
            return noRecolorMaterial;
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

   public bool hasTwoColors (Gender.Type gender, int armorType) {
      ColorKey colorKey = new ColorKey(gender, "armor_" + armorType.ToString());

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
