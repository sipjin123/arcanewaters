using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StoreHatDyeBox : StoreDyeBox
{
   #region Public Variables

   // Reference to the Body layer
   public Image bodyImage;

   // Reference to the Hat layer
   public Image hatImage;

   // Reference to the Recolored sprite for the hat
   public RecoloredSprite hatSprite;

   // The hat of the current player
   public Item playerHat;

   #endregion

   public virtual void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.HatDyes;

      if (this.hatImage == null || this.bodyImage == null || this.hatSprite == null || Util.isBatch() || dye == null) {
         return;
      }

      this.palette = PaletteSwapManager.self.getPalette(dye.paletteId);

      if (this.palette == null) {
         return;
      }

      // Body
      setupBody(Global.player.gender);

      // Hat
      HatStatData hatData = EquipmentXMLManager.self.getHatData(playerHat.itemTypeId);

      if (hatData == null) {
         hatData = EquipmentXMLManager.self.hatStatList.First();
      }

      if (hatData != null) {
         setupHat(Global.player.gender, hatData.hatType, palette.paletteName);
      }
   }

   private bool setupBody (Gender.Type gender) {
      BodyLayer.Type randomBodyType = BodyLayer.getRandomBodyTypeOfGender(gender);
      Sprite bodySprite = ImageManager.getBodySprite(gender, randomBodyType, frame: 8);

      if (bodySprite == null) {
         return false;
      }

      bodyImage.sprite = bodySprite;
      bodyImage.material = new Material(this.bodyImage.material);

      return true;
   }

   private bool setupHat (Gender.Type gender, int hatType, string hatPalette) {
      Sprite hatSprite = ImageManager.getHatSprite(hatType, frame: 8);

      if (hatSprite == null) {
         return false;
      }

      hatImage.sprite = hatSprite;
      hatImage.material = new Material(this.hatImage.material);

      string mergedPalette = Item.parseItmPalette(Item.overridePalette(hatPalette, playerHat.paletteNames));

      recolorHat(mergedPalette);

      return true;
   }

   private void recolorHat (string palette) {
      hatSprite.recolor(palette);
   }

   public bool isDyeApplicable () {
      // Checks whether the dye can actually be applied to the hat
      PaletteToolData palette = PaletteSwapManager.self.getPalette(this.dye.paletteId);
      HatDyeSupportInfo info = getHatDyeSupportInfo();

      if (palette.isPrimary() && info.supportsPrimaryDyes ||
         palette.isSecondary() && info.supportsSecondaryDyes ||
         palette.isAccent() && info.supportsAccentDyes ) {
         return true;
      }

      return false;
   }

   public HatDyeSupportInfo getHatDyeSupportInfo () {
      if (_hatDyeSupportInfoCache.ContainsKey(this.playerHat.itemTypeId)) {
         return _hatDyeSupportInfoCache[this.playerHat.itemTypeId];
      }

      var palettes = PaletteSwapManager.self.getPaletteList();
      PaletteToolData primaryPalette = null;
      PaletteToolData secondaryPalette = null;
      PaletteToolData accentPalette = null;
      bool canUsePrimaryDyes = false;
      bool canUseSecondaryDyes = false;
      bool canUseAccentDyes = false;

      foreach (PaletteToolData palette in palettes) {
         if (DyeXMLManager.self.getAdjustedPaletteType(palette) == PaletteToolManager.PaletteImageType.Hat) {
            if (palette.isPrimary() && primaryPalette == null) {
               primaryPalette = palette;
            }

            if (palette.isSecondary() && secondaryPalette == null) {
               secondaryPalette = palette;
            }

            if (palette.isAccent() && accentPalette == null) {
               accentPalette = palette;
            }
         }
      }

      if (primaryPalette != null) {
         foreach (Color colorRGB in primaryPalette.srcColor.Select(PaletteToolManager.convertHexToRGB)) {
            if (!canUsePrimaryDyes) {
               canUsePrimaryDyes = textureContainsColor(hatImage.sprite.texture, colorRGB, hatImage.sprite);
            }
         }
      }

      if (secondaryPalette != null) {
         foreach (Color colorRGB in secondaryPalette.srcColor.Select(PaletteToolManager.convertHexToRGB)) {
            if (!canUseSecondaryDyes) {
               canUseSecondaryDyes = textureContainsColor(hatImage.sprite.texture, colorRGB, hatImage.sprite);
            }
         }
      }

      if (accentPalette != null) {
         foreach (Color colorRGB in accentPalette.srcColor.Select(PaletteToolManager.convertHexToRGB)) {
            if (!canUseAccentDyes) {
               canUseAccentDyes = textureContainsColor(hatImage.sprite.texture, colorRGB, hatImage.sprite);
            }
         }
      }

      HatDyeSupportInfo info = new HatDyeSupportInfo {
         supportsPrimaryDyes = canUsePrimaryDyes,
         supportsSecondaryDyes = canUseSecondaryDyes,
         supportsAccentDyes = canUseAccentDyes
      };

      _hatDyeSupportInfoCache[this.playerHat.itemTypeId] = info;
      return info;
   }

   public bool textureContainsColor (Texture2D texture, Color color, Sprite region = null) {
      if (region == null) {
         return texture.GetPixels().Any(_ => _ == color);
      }

      return texture.GetPixels((int) region.rect.x, (int) region.rect.y, (int) region.rect.width, (int) region.rect.height).Any(_ => _ == color);
   }

   #region Private Variables

   // Stores the information related to the support by each available hat of any of the different types of dye
   private static Dictionary<int, HatDyeSupportInfo> _hatDyeSupportInfoCache = new Dictionary<int, HatDyeSupportInfo>();

   #endregion
}

