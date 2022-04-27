using UnityEngine;
using UnityEngine.UI;

public class StoreWeaponDyeBox : StoreDyeBox
{
   #region Public Variables

   // The hair layers
   public Image weaponImage;

   // Reference to the player's weapon
   public Item playerWeapon;

   #endregion

   public void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.WeaponDyes;

      if (this.weaponImage == null || Util.isBatch()) {
         return;
      }

      setupWeaponLayer(front: true, weaponImage, palette.paletteName);

      setupBanner();
   }

   private bool setupWeaponLayer (bool front, Image weaponImage, string weaponPalette) {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(this.playerWeapon.itemTypeId);

      if (weaponData == null) {
         return false;
      }

      Sprite sprite = ImageManager.getSprite(weaponData.equipmentIconPath);

      if (sprite == null) {
         return false;
      }

      weaponImage.sprite = sprite;
      weaponImage.material = new Material(this.weaponImage.material);

      string mergedPalette = Item.parseItmPalette(Item.overridePalette(this.playerWeapon.paletteNames, weaponData.palettes));
      mergedPalette = Item.parseItmPalette(Item.overridePalette(weaponPalette, mergedPalette));
      weaponImage.GetComponent<RecoloredSprite>().recolor(mergedPalette);
      return true;
   }

   #region Private Variables

   #endregion
}

