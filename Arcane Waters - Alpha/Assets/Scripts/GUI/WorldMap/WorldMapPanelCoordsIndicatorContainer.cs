using UnityEngine;

public class WorldMapPanelCoordsIndicatorContainer : MonoBehaviour
{
   #region Public Variables

   // Reference to the coords indicator
   public WorldMapPanelCoordsIndicator coordsIndicator;

   #endregion

   public void positionIndicator (WorldMapPanelAreaCoords areaCoords) {
      if (coordsIndicator) {
         // Compute the geo coords
         WorldMapAreaCoords coords = WorldMapPanel.self.transformCoords(areaCoords);
         WorldMapGeoCoords geoCoords = WorldMapManager.self.getGeoCoordsFromWorldMapAreaCoords(coords);

         // Set the new coords
         coordsIndicator.setCoords(geoCoords.worldY.ToString(), geoCoords.worldX.ToString());

         // Reposition the indicator
         coordsIndicator.transform.localPosition = new Vector3(areaCoords.x * WorldMapPanel.self.cellSize.x, -areaCoords.y * WorldMapPanel.self.cellSize.y);
      }
   }

   public void toggleIndicator (bool show) {
      if (coordsIndicator) {
         coordsIndicator.toggle(show);
      }
   }

   #region Private Variables

   #endregion
}
