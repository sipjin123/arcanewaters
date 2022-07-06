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
   public WorldMapPanelCellsContainer cellsContainer;

   // Reference to the container holding the waypoints
   public WorldMapPanelWaypointsContainer waypointsContainer;

   // Reference to the container holding the Coords indicator
   public WorldMapPanelCoordsIndicatorContainer coordsIndicatorContainer;

   // Reference to the container holding the Player Pins
   public WorldMapPanelPinsContainer playerPinsContainer;

   // Reference to the Sites Menu
   public WorldMapPanelMenu menu;

   // Self
   public static WorldMapPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public override void Update () {
      if (menu.isShowing()) {
         // While navigating the tabs in the panel's menu the selected cells might lose focus and become unselected
         // To prevent this, force the cell that triggered the menu to stay selected 
         cellsContainer.focus();
      }
   }

   public void displayMap () {
      if (TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.OpenMap) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenMap);
      }

      // Make sure the panel is showing
      PanelManager.self.showPanel(Type.WorldMap);

      List<WorldMapPanelAreaCoords> visitedWorldMapPanelAreasCoords = WorldMapManager.self.getVisitedAreasCoordsList().Select(transformCoords).ToList();

      // Clouds
      cloudsContainer.fill();
      cloudsContainer.hideClouds(visitedWorldMapPanelAreasCoords);

      // Update exploration progress
      int visitedWorldMapAreasCount = visitedWorldMapPanelAreasCoords.Count;
      Vector2Int worldMapDimensions = WorldMapManager.self.getMapSize();
      int worldMapAreasTotalCount = worldMapDimensions.x * worldMapDimensions.y;
      progressIndicator.updateProgress((float) visitedWorldMapAreasCount / worldMapAreasTotalCount);

      // Pins
      pinsContainer.clearPins();
      pinsContainer.addPins(WorldMapManager.self.getSpots());

      // Menu
      menu.hide();

      // Current Area Selector
      currentAreaSelectorContainer.setCurrentArea(Global.player.areaKey);

      // Hovered Area Selector
      cellsContainer.fill();

      // Waypoints
      waypointsContainer.clearWaypoints();
      waypointsContainer.addWaypoints(WorldMapWaypointsManager.self.getWaypointSpots());

      // For now hide the coords indicator
      coordsIndicatorContainer.toggleIndicator(false);

      // Request the position of the team members
      requestGroupMembersLocations();

      show();
   }

   public WorldMapPanelAreaCoords transformCoords (WorldMapAreaCoords mapAreaCoords) {
      WorldMapPanelAreaCoords panelAreaCoords = new WorldMapPanelAreaCoords {
         x = mapAreaCoords.x,
         y = mapDimensions.y - mapAreaCoords.y - 1
      };
      return panelAreaCoords;
   }

   public WorldMapAreaCoords transformCoords (WorldMapPanelAreaCoords areaCoords) {
      WorldMapAreaCoords panelAreaCoords = new WorldMapAreaCoords {
         x = areaCoords.x,
         y = mapDimensions.y - areaCoords.y - 1
      };
      return panelAreaCoords;
   }

   public void onAreaPressed (WorldMapPanelAreaCoords areaCoords) {
      // Prepare to show the menu with the warps and waypoints of the selected area
      List<WorldMapPanelAreaCoords> adjustedAreaCoords = WorldMapManager.self.getVisitedAreasCoordsList().Select(transformCoords).ToList();

      // The player hasn't been to this area of the map yet
      if (!adjustedAreaCoords.Contains(areaCoords)) {
         return;
      }

      showMenu(areaCoords);
   }

   public void onAreaHovered (WorldMapPanelAreaCoords areaCoords) {
      coordsIndicatorContainer.toggleIndicator(true);
      coordsIndicatorContainer.positionIndicator(areaCoords);
   }

   private void showMenu (WorldMapPanelAreaCoords areaCoords, int menuTabIndex = 0) {
      // Temporarily hide the menu
      menu.hide();

      // Register the pins with the menu
      menu.clearMenuItems();

      IEnumerable<WorldMapPanelPin> pins = pinsContainer.getPinsWithinArea(areaCoords);
      menu.addMenuItems(pins.Select(_ => _.spot));

      // Register the waypoints with the menu
      IEnumerable<WorldMapPanelWaypoint> waypoints = waypointsContainer.getWaypointsWithinArea(areaCoords);
      menu.addMenuItems(waypoints.Select(_ => _.spot));

      // Register the player pins with the menu
      IEnumerable<WorldMapPanelPin> playerPins = playerPinsContainer.getPinsWithinArea(areaCoords);
      menu.addMenuItems(playerPins.Select(_ => _.spot));

      // Show the menu and optionally specify the tab to display
      menu.show(menuTabIndex);

      // To ensure that the currently selected area is not covered by the menu
      menu.shift(toLeftSide: areaCoords.x >= (mapDimensions.x / 2));
   }

   public void onMenuItemPressed (WorldMapPanelMenuItem menuItem) {
      WorldMapSpot spot = menuItem.spot;

      if (menuItem.isDestination()) {
         // Ensure the spot is a warp spot
         if (spot != null && spot.type == WorldMapSpot.SpotType.Warp) {
            tryWarp(spot.target, spot.spawnTarget);
         }
      } else if (menuItem.isWaypoint()) {
         // Delete the waypoint at spot from the scene
         WorldMapWaypointsManager.self.destroyWaypoint(spot);

         // Delete the waypoint at spot from the map panel
         waypointsContainer.removeWaypoint(spot);

         // Redisplay the menu
         WorldMapAreaCoords mapAreaCoords = WorldMapManager.self.getWorldMapAreaCoordsFromGeoCoords(WorldMapManager.self.getGeoCoordsFromSpot(menuItem.spot));
         showMenu(transformCoords(mapAreaCoords), menu.getCurrentTab());
      }
   }

   public void onMenuItemPointerEnter (WorldMapPanelMenuItem menuItem) {
      WorldMapSpot spot = menuItem.spot;

      if (menuItem.isWaypoint()) {
         waypointsContainer.highlightWaypoint(spot, show: true);
      } else if (menuItem.isDestination()) {
         pinsContainer.highlightPin(spot, show: true);
      }
   }

   public void onMenuItemPointerExit (WorldMapPanelMenuItem menuItem) {
      WorldMapSpot spot = menuItem.spot;

      if (menuItem.isWaypoint()) {
         waypointsContainer.highlightWaypoint(spot, show: false);
      } else if (menuItem.isDestination()) {
         pinsContainer.highlightPin(spot, show: false);
      }
   }

   public void onMenuDismissed () {
      // When the player closes the menu, makes sure to unselect all cells
      cellsContainer.blur();
   }

   public void tryWarp (string areaTarget, string spawnTarget, bool skipPvpCheck = false) {
      if (Global.player == null) {
         return;
      }

      if (!skipPvpCheck && GroupInstanceManager.isPvpArenaArea(Global.player.areaKey)) {
         string message = "Are you sure you want to leave your current Pvp Game?";
         if (WorldMapManager.isWorldMapArea(Global.player.areaKey)) {
            message = "Are you sure you want to leave the open seas?";
         }
         PanelManager.self.showConfirmationPanel(message, () => {
            tryWarp(areaTarget, spawnTarget, skipPvpCheck: true);
         });
         return;
      }

      Global.player.rpc.Cmd_RequestWarpToArea(areaTarget, spawnTarget);

      // Close any opened panel
      PanelManager.self.hideCurrentPanel();
   }

   public void requestGroupMembersLocations () {
      if (Global.player == null) {
         return;
      }

      Global.player.rpc.Cmd_RequestGroupMemberLocations();
   }

   public void onReceiveGroupMemberLocations(WorldMapSpot[] groupMemberLocations) {
      if (groupMemberLocations == null || !groupMemberLocations.Any()) {
         return;
      }

      // Refresh player pins
      playerPinsContainer.clearPins();
      playerPinsContainer.addPins(groupMemberLocations);
   }

   #region Private Variables

   #endregion
}