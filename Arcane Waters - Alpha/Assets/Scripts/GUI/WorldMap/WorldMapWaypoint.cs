using UnityEngine;

// Represents a map waypoint that can be instanced in the scene
public class WorldMapWaypoint : MonoBehaviour
{
   #region Public Variables

   // The target of the waypoint
   public WorldMapSpot spot;

   // The display name of the waypoint
   public string displayName;

   // The clickable area
   public ClickableBox clickableBox;

   #endregion

   private void Start () {
      if (clickableBox) {
         clickableBox.mouseButtonDown += onPressed;
      }
   }

   private void onPressed (MouseButton mouseButton) {
      D.debug($"The waypoint '{displayName}' was pressed.");

      // Confirm that the player wants to delete the waypoint
      if (PanelManager.self) {
         PanelManager.self.confirmScreen.showYesNo("Do you want to delete this waypoint?");
         PanelManager.self.confirmScreen.cancelButton.onClick.RemoveAllListeners();
         PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
         PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
            // Hide the panel
            PanelManager.self.confirmScreen.hide();

            // Remove the waypoint
            if (WorldMapWaypointsManager.self) {
               WorldMapWaypointsManager.self.removeWaypoint(spot);
            }
         });
      }
   }

   private void OnDestroy () {
      if (clickableBox) {
         clickableBox.mouseButtonDown -= onPressed;
      }
   }

   #region Private Variables

   #endregion
}
