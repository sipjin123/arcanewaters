using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class StoreArmorDyeBox : StoreItemBox
{
   #region Public Variables

   // Reference to the Body layer
   public Image bodyImage;

   // Reference to the Armor layer
   public Image armorImage;

   // Reference to the Recolored sprite for the armor
   public RecoloredSprite armorSprite;

   // The clothing dye information
   public DyeData armorDye;

   // Palette of the current armor
   public Item playerArmor;

   // Palette information
   public PaletteToolData palette;

   #endregion

   public virtual void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.ArmorDyes;

      if (this.armorImage == null || this.bodyImage == null || this.armorSprite == null || Util.isBatch() || armorDye == null) {
         return;
      }

      this.palette = PaletteSwapManager.self.getPalette(armorDye.paletteId);

      if (this.palette == null) {
         return;
      }

      // Body
      setupBody(Global.player.gender);

      // Armor
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(playerArmor.itemTypeId);

      if (armorData == null) {
         armorData = EquipmentXMLManager.self.armorStatList.First();
      }

      if (armorData != null) {
         setupArmor(Global.player.gender, armorData.armorType, palette.paletteName);
      }
   }

   private BodyLayer.Type getRandomBodyTypeOfGender(Gender.Type gender) {
      List<BodyLayer.Type> genderTypes = new List<BodyLayer.Type>();
      Array bodyTypes = Enum.GetValues(typeof(BodyLayer.Type));
      
      foreach (var item in bodyTypes) {
         var bodyType = (BodyLayer.Type) item;

         if ((gender == Gender.Type.Male && bodyType.ToString().ToLower().StartsWith("male")) || (gender == Gender.Type.Female && bodyType.ToString().ToLower().StartsWith("female"))) {
            genderTypes.Add(bodyType);
         }
      }

      int index = Mathf.FloorToInt(UnityEngine.Random.Range(0, genderTypes.Count - 1));
      return genderTypes[index];
   }

   private bool setupBody (Gender.Type gender) {
      BodyLayer.Type randomBodyType = getRandomBodyTypeOfGender(gender);
      Sprite bodySprite = ImageManager.getBodySprite(gender, randomBodyType, frame: 8);

      if (bodySprite == null) {
         return false;
      }

      bodyImage.sprite = bodySprite;
      bodyImage.material = new Material(this.bodyImage.material);

      return true;
   }

   private bool setupArmor (Gender.Type gender, int armorType, string armorPalette) {
      Sprite armorSprite = ImageManager.getArmorSprite(gender, armorType, frame: 8);

      if (armorSprite == null) {
         return false;
      }

      armorImage.sprite = armorSprite;
      armorImage.material = new Material(this.armorImage.material);

      string mergedPalette = Item.parseItmPalette(Item.overridePalette(armorPalette, playerArmor.paletteNames));

      // Recolor armor
      recolorArmor(mergedPalette);

      return true;
   }

   private void recolorArmor(string palette) {
      armorSprite.recolor(palette);
   }

   #region Private Variables

   #endregion
}

