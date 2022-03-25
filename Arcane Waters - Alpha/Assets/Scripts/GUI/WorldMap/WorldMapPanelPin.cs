using UnityEngine;
using UnityEngine.UI;

public class WorldMapPanelPin : MonoBehaviour
{
   #region Public Variables

   // Reference to the object that contains the information about the pin
   public WorldMapSpot spot;

   // Reference to the control that displays the image of the pin
   public Image image;

   // Reference to the rect transform of the pin
   public RectTransform rect;

   // Tooltip
   public ToolTipComponent tooltip;

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
   
   public void setTooltip (string displayName) {
      tooltip.message = displayName;
   }   

   #region Private Variables

   #endregion

}
