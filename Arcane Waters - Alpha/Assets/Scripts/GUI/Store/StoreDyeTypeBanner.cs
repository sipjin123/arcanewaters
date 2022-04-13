using UnityEngine;
using UnityEngine.UI;

public class StoreDyeTypeBanner : MonoBehaviour
{
   #region Public Variables

   // Reference to the sprite used to represent Primary dyes
   public Sprite primaryDyeSprite;

   // Reference to the sprite used to represent Secondary dyes
   public Sprite secondaryDyeSprite;

   // Reference to the sprite used to represent Accent Dyes
   public Sprite accentDyeSprite;

   // Reference to the image control that shows the dye type sprite
   public Image image;

   #endregion

   #region Sprites

   public void setAsPrimary () {
      setSprite(primaryDyeSprite);
   }

   public void setAsSecondary () {
      setSprite(secondaryDyeSprite);
   }

   public void setAsAccent () {
      setSprite(accentDyeSprite);
   }

   private void setSprite(Sprite sprite) {
      if (image == null) {
         return;
      }

      image.sprite = sprite;
   }

   private Sprite getSprite () {
      if (image == null) {
         return null;
      }
      return image.sprite;
   }

   #endregion

   public void toggle(bool show) {
      gameObject.SetActive(show);
   }

   #region Private Variables

   #endregion
}

