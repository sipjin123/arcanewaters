using UnityEngine;
using UnityEngine.UI;

public class StoreHatBox : StoreItemBox
{
   #region Public Variables

   // The hat item
   public HatStatData hat;

   #endregion

   public void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.Hats;

      if (this.imageIcon == null || Util.isBatch() || hat == null) {
         return;
      }

      // Set the details
      itemName = hat.equipmentName;
      itemDescription = hat.equipmentDescription;

      assignHatSpriteToImage(imageIcon);
   }

   private bool assignHatSpriteToImage (Image image) {
      Sprite sprite = ImageManager.getSprite(hat.equipmentIconPath);

      if (sprite == null) {
         return false;
      }

      image.sprite = sprite;
      image.material = new Material(this.imageIcon.material);
      return true;
   }

   #region Private Variables

   #endregion
}
