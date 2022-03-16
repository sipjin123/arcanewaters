using UnityEngine;
using System.Collections.Generic;

public class WorldMapWaypointsManager : MonoBehaviour
{
   #region Public Variables

   // Prefab used to create waypoints in the scene
   public GameObject waypointPrefab;

   // Self
   public static WorldMapWaypointsManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   private void Update () {
      if (_waypoints.Count >= _waypointSpots.Count) {
         return;
      }

      foreach (WorldMapSpot spot in _waypointSpots) {
         if (Global.player == null) {
            continue;
         }

         if (!Util.isEmpty(spot.subAreaKey) && spot.subAreaKey != Global.player.areaKey) {
            continue;
         }

         instantiateWaypointAt(spot);
      }
   }

   public void clearWaypoints () {
      // Destroy all the instanced waypoints
      foreach (WorldMapWaypoint waypoint in _waypoints) {
         Destroy(waypoint);
      }

      // Clear registries
      _waypoints.Clear();
      _waypointSpots.Clear();
   }

   private void instantiateWaypointAt (WorldMapSpot spot) {
      if (Global.player == null) {
         return;
      }

      Area area = AreaManager.self.getArea(Global.player.areaKey);

      if (area == null) {
         return;
      }

      // Instantiate the new waypoint in the world
      GameObject waypointGO = Instantiate(waypointPrefab, area.transform);
      WorldMapWaypoint waypoint = waypointGO.GetComponent<WorldMapWaypoint>();

      // Store a reference to the instanced waypoint
      _waypoints.Add(waypoint);

      // Set the details on the new waypoint
      waypoint.transform.localPosition = WorldMapManager.self.getPositionFromSpot(Global.player.areaKey, spot);
      waypoint.spot = spot;
      waypoint.displayName = $"Waypoint {_waypoints.Count}";

      // Add a minimap icon for the waypoint
      Minimap.self.addWaypointIcon(area, waypoint);
   }

   public void addWaypoint (WorldMapSpot spot) {
      _waypointSpots.Add(spot);
   }

   public void removeWaypoint (WorldMapSpot spot) {
      foreach (WorldMapWaypoint waypoint in _waypoints.ToArray()) {
         if (waypoint.spot == spot) {
            // Destroy the instanced waypoint scene object
            Destroy(waypoint.gameObject);

            // Remove the waypooint from storage
            _waypoints.Remove(waypoint);

            // Delete the waypoint from the minimap
            Minimap.self.deleteWaypointIcon(waypoint);
         }
      }

      _waypointSpots.Remove(spot);
   }

   public List<WorldMapWaypoint> getWaypoints () {
      return _waypoints;
   }

   public List<WorldMapSpot> getWaypointSpots () {
      return _waypointSpots;
   }

   #region Private Variables

   // Waypoints registry
   private List<WorldMapWaypoint> _waypoints = new List<WorldMapWaypoint>();

   // Spots registry
   private List<WorldMapSpot> _waypointSpots = new List<WorldMapSpot>();

   #endregion
}
