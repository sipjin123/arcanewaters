using UnityEngine;
using UnityEngine.UI;

public class WorldMapPanelPin : MonoBehaviour
{
   #region Public Variables

   // Reference to the object that contains the information about the pin
   public WorldMapPanelPinInfo info;

   // Reference to the control that displays the image of the pin
   public Image image;

   // Pin types
   public enum PinTypes
   {
      // None
      None = 0,

      // Warp
      Warp = 1,

      // League
      League = 2,

      // Discovery
      Discovery = 3
   }

   #endregion

   public void setSprite(Sprite sprite) {
      if (image == null) {
         return;
      }

      image.sprite = sprite;
   }

   public void toggle(bool show) {
      gameObject.SetActive(show);
   }

   #region Private Variables

   #endregion
}
