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

      if (this.weaponImage == null || Util.isBatch() || dye == null) {
         return;
      }

      this.palette = PaletteSwapManager.self.getPalette(dye.paletteId);

      if (this.palette == null) {
         return;
      }

      setupWeaponLayer(front: true, weaponImage, palette.paletteName);
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
      weaponImage.GetComponent<RecoloredSprite>().recolor(weaponPalette);
      return true;
   }

   #region Private Variables

   #endregion
}

