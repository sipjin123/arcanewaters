using System.Linq;

public class SpeakChatRowWaypointCreationAction : SpeakChatRowAction
{
   #region Public Variables

   #endregion

   public override void execute () {
      WorldMapGeoCoords geoCoords = WorldMapManager.self.decodeGeoCoords(chatRow.chatLine.chatInfo.extra);

      if (geoCoords == null) {
         return;
      }

      if (!WorldMapManager.self.areGeoCoordsValid(geoCoords)) {
         PanelManager.self.noticeScreen.show("Invalid Location");
         return;
      }

      WorldMapSpot spot = WorldMapManager.self.getSpotFromGeoCoords(geoCoords);

      if (WorldMapWaypointsManager.self.getWaypointSpots().Any(_ => WorldMapManager.self.areSpotsInTheSamePosition(_, spot))) {
         PanelManager.self.noticeScreen.show("Waypoint already placed!");
         return;
      }

      WorldMapWaypointsManager.self.addWaypoint(spot);
      PanelManager.self.noticeScreen.show("Waypoint placed!");
   }

   public override void refresh () {
      // Check visibility
      WorldMapGeoCoords geoCoords = WorldMapManager.self.decodeGeoCoords(chatRow.chatLine.chatInfo.extra);
      bool showAction = geoCoords != null && WorldMapManager.self.areGeoCoordsValid(geoCoords);
      this.toggle(showAction);

      // Set the tooltip
      tooltip.message = "Click to create a waypoint";
   }

   #region Private Variables

   #endregion
}
