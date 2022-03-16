using UnityEngine;
using TMPro;

public class WorldMapPanelCoordsIndicator : MonoBehaviour
{
   #region Public Variables

   // Reference to the rect transform
   public RectTransform rect;

   // Reference to the rect transform of the text container
   public RectTransform txtRect;

   // Reference to the canvas group
   public CanvasGroup canvasGroup;

   // Reference to the control displaying the X coordinate
   public TextMeshProUGUI txtX;

   // Reference to the control displaying the Y coordinate
   public TextMeshProUGUI txtY;

   #endregion

   public void setCoords (string x, string y) {
      txtX.text = x;
      txtY.text = y;
   }

   public void toggle (bool show) {
      canvasGroup.alpha = show ? 1 : 0;
   }

   #region Private Variables

   #endregion
}
