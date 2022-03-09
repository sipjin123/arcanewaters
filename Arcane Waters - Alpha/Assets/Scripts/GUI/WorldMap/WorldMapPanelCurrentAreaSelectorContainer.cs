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
      if (VoyageManager.isOpenWorld(areaKey)) {
         _areaCoords = WorldMapManager.computeOpenWorldAreaCoords(areaKey);
      } else {
         // See if the current area can be reached through one of the known warps
         IEnumerable<WorldMapPanelPinInfo> pins = WorldMapPanel.self.getMapPins();
         bool isPinTargetArea = pins.Any(_ => _.target == areaKey);

         if (isPinTargetArea) {
            WorldMapPanelPinInfo pin = pins.FirstOrDefault(_ => _.target == areaKey);
            _areaCoords = new Vector2Int(pin.areaX, pin.areaY);
         } else {
            // Fallback area
            _areaCoords = WorldMapManager.computeOpenWorldAreaCoordsForBiome(AreaManager.self.getDefaultBiome(areaKey));
         }
      }

      _areaCoords = WorldMapPanel.self.adjustAreaCoords(_areaCoords);

      if (selector != null) {
         selector.localPosition = new Vector3(_areaCoords.x * WorldMapPanel.self.cellSize.x, -_areaCoords.y * WorldMapPanel.self.cellSize.y);
      }

      _currentAreaKey = areaKey;
   }

   public string getCurrentArea () {
      return _currentAreaKey;
   }

   #region Private Variables

   // The key of the current area
   private string _currentAreaKey;

   // The coords of the current area
   private Vector2Int _areaCoords;

   #endregion
}
