using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WorldMapPanelCurrentAreaSelectorContainer : MonoBehaviour
{
   #region Public Variables

   // Reference to the control that displays the current area
   public Transform selector;

   #endregion

   public void setCurrentArea (string areaKey) {
      WorldMapAreaCoords mapAreaCoords = tryGetAreaCoords(areaKey);
      _areaCoords = WorldMapPanel.self.transformCoords(mapAreaCoords);

      if (selector != null) {
         selector.localPosition = new Vector3(_areaCoords.x * WorldMapPanel.self.cellSize.x, -_areaCoords.y * WorldMapPanel.self.cellSize.y);
      }

      _currentAreaKey = areaKey;
   }

   private static WorldMapAreaCoords tryGetAreaCoords (string areaKey) {
      if (WorldMapManager.isWorldMapArea(areaKey)) {
         return WorldMapManager.getAreaCoords(areaKey);
      }

      // See if the current area can be reached through one of the known warps
      WorldMapSpot spot = WorldMapManager.self.getSpots().FirstOrDefault(_ => _.target == areaKey);

      if (spot != null) {
         return new WorldMapAreaCoords(spot.worldX, spot.worldY);
      }

      if (Global.player != null) {
         spot = WorldMapManager.self.getSpotFromPosition(Global.player.areaKey, Global.player.transform.localPosition);

         if (spot != null) {
            return new WorldMapAreaCoords(spot.worldX, spot.worldY);
         }
      }

      // Fallback area
      return WorldMapManager.self.getAreaCoordsForBiome(AreaManager.self.getDefaultBiome(areaKey));
   }

   public string getCurrentArea () {
      return _currentAreaKey;
   }

   #region Private Variables

   // The key of the current area
   private string _currentAreaKey;

   // The coords of the current area
   private WorldMapPanelAreaCoords _areaCoords;

   #endregion
}
