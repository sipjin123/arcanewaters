using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using TMPro;
using System.Linq;

public class WorldMapPanel : Panel
{
   #region Public Variables

   // The size of each cell in the map
   public Vector2Int cellSize = new Vector2Int(64, 64);

   // The resolution of the grid
   public Vector2Int mapDimensions = new Vector2Int(15, 9);

   // Reference to the Sprite resolver
   public WorldMapPanelCloudSpriteResolver cloudSpriteResolver;

   // Reference to the Clouds Container
   public WorldMapPanelCloudsContainer cloudsContainer;

   // Reference to the Pins Container
   public WorldMapPanelPinsContainer pinsContainer;

   // Reference to the Progress Indicator
   public WorldMapPanelProgressIndicator progressIndicator;

   // Reference to the container holding the selector that highlights the area the player is in
   public WorldMapPanelCurrentAreaSelectorContainer currentAreaSelectorContainer;

   // Reference to the container holding the selector that highlights the currently hovered area
   public WorldMapPanelHoveredAreaSelectorContainer hoveredAreaSelectorContainer;

   // Reference to the Pins Menu
   public WorldMapPinsMenu pinsMenu;

   // Self
   public static WorldMapPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void displayMap () {
      if (TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.OpenMap) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenMap);
      }

      Global.player.rpc.Cmd_RequestWorldMap();
   }

   public void onWorldMapReceived (List<Vector2Int> visitedWorldMapAreasCoords, List<WorldMapPanelPinInfo> worldMapPins) {
      _visitedWorldMapAreasCoords = visitedWorldMapAreasCoords.Select(adjustAreaCoords).ToList();
      _worldMapPins = worldMapPins;

      // Clouds
      cloudsContainer.fill();
      cloudsContainer.hideCloudsAtCoords(_visitedWorldMapAreasCoords);

      // Update exploration progress
      progressIndicator.updateProgress((float) _visitedWorldMapAreasCoords.Count / (mapDimensions.x * mapDimensions.y));

      // Pins
      pinsContainer.clearPins();
      pinsContainer.addPins(_worldMapPins);

      // Pin Menu
      pinsMenu.toggle(false);

      // Current Area Selector
      currentAreaSelectorContainer.setCurrentArea(Global.player.areaKey);

      // Hovered Area Selector
      hoveredAreaSelectorContainer.fill();
   }

   public Vector2Int adjustAreaCoords (Vector2Int areaCoords) {
      // Adjust the location Y coordinate to match the coords system of the world map panel
      return new Vector2Int(areaCoords.x, mapDimensions.y - areaCoords.y - 1);
   }

   public List<Vector2Int> getVisitedAreasCoords () {
      return _visitedWorldMapAreasCoords;
   }

   public void onMapCellClicked (Vector2Int areaCoords) {
      if (_visitedWorldMapAreasCoords.Contains(areaCoords)) {
         List<WorldMapPanelPin> pins = pinsContainer.getPinsWithinArea(areaCoords);
         pinsMenu.toggle(show: false);

         if (pins.Any(pin => pin.info.pinType == WorldMapPanelPin.PinTypes.Warp)) {
            List<WorldMapPanelPinInfo> pinInfos = pins.Where(pin => pin.info.pinType == WorldMapPanelPin.PinTypes.Warp).Select(_ => _.info).ToList();
            pinsMenu.clear();
            pinsMenu.add(pinInfos);
            pinsMenu.toggle(show: true);
         }
      }
   }

   public void onMenuItemSelected (WorldMapPinsMenuItem menuItem) {
      if (Global.player != null) {
         WorldMapPanelPinInfo pin = menuItem.getPin();

         if (pin != null && pin.pinType == WorldMapPanelPin.PinTypes.Warp) {
            tryWarp(pin.target, pin.spawnTarget);
         }
      }
   }

   public void tryWarp (string areaTarget, string spawnTarget, bool skipPvpCheck = false) {
      if (!skipPvpCheck && VoyageManager.isPvpArenaArea(Global.player.areaKey)) {
         PanelManager.self.showConfirmationPanel("Are you sure you want to leave your current Pvp Game?", () => {
            tryWarp(areaTarget, spawnTarget, skipPvpCheck: true);
         });
         return;
      }

      Global.player.rpc.Cmd_RequestWarpToArea(areaTarget, spawnTarget);

      // Close any opened panel
      PanelManager.self.unlinkPanel();
   }

   public IEnumerable<WorldMapPanelPinInfo> getMapPins () {
      return _worldMapPins;
   }

   #region Private Variables

   // Coordinates of the visited world map areas
   private List<Vector2Int> _visitedWorldMapAreasCoords = new List<Vector2Int>();

   // Local cache of the map pins
   private List<WorldMapPanelPinInfo> _worldMapPins = new List<WorldMapPanelPinInfo>();

   #endregion
}