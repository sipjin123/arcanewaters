using UnityEngine;
using UnityEngine.UI;

// Represents a map waypoint that is displayed in the context of the World Map Panel
public class WorldMapPanelWaypoint : MonoBehaviour
{
   #region Public Variables

   // The target of the waypoint
   public WorldMapSpot spot;

   // The image control for the waypoint  
   public Image image;

   // The rect transform of the waypoint
   public RectTransform rect;

   #endregion

   public void setSprite (Sprite sprite) {
      if (image == null) {
         return;
      }

      image.sprite = sprite;
   }

   public void toggle (bool show) {
      gameObject.SetActive(show);
   }

   #region Private Variables

   #endregion
}
