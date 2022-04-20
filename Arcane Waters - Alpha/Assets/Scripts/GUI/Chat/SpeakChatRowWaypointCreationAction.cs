using System.Linq;

public class SpeakChatRowWaypointCreationAction : SpeakChatRowAction
{
   #region Public Variables

   #endregion

   public override void execute () {
      WorldMapSpot newSpot = WorldMapManager.self.decodeSpot(chatRow.chatLine.chatInfo.extra);

      if (newSpot == null) {
         return;
      }

      if (WorldMapWaypointsManager.self.getWaypointSpots().Any(spot => WorldMapManager.self.areSpotsInTheSamePosition(spot, newSpot))) {
         PanelManager.self.noticeScreen.show("Waypoint already placed!");
         return;
      }

      WorldMapWaypointsManager.self.createWaypoint(newSpot);
      PanelManager.self.noticeScreen.show("Waypoint placed!");
   }

   public override void refresh () {
      // Check visibility
      WorldMapSpot spot = WorldMapManager.self.decodeSpot(chatRow.chatLine.chatInfo.extra);
      this.toggle(spot != null);

      // Set the tooltip
      tooltip.message = "Click to create a waypoint";
   }

   #region Private Variables

   #endregion
}
