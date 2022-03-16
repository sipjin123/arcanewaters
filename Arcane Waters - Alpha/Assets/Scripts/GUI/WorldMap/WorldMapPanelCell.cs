using UnityEngine;
using UnityEngine.UI;

public class WorldMapPanelCell : MonoBehaviour
{
   #region Public Variables

   // Button 
   public Button button;

   // Reference to the coords of the area this cell represents
   public WorldMapPanelAreaCoords coords;

   #endregion

   public void onPointerEnter () {
      WorldMapPanel.self.onAreaHovered(coords);
   }

   #region Private Variables

   #endregion
}
