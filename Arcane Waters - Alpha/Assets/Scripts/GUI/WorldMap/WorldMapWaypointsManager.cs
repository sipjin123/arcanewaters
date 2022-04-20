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

   private void FixedUpdate () {
      foreach (WorldMapSpot spot in _waypointSpotsQueue) {
         if (Global.player == null) {
            continue;
         }

         if (!Util.isEmpty(spot.subAreaKey)) {
            if (spot.subAreaKey != Global.player.areaKey) {
               continue;
            }
         } else {
            if (WorldMapManager.self.getAreaKey(new WorldMapAreaCoords(spot.worldX, spot.worldY)) != Global.player.areaKey) {
               continue;
            }
         }

         instantiateSceneWaypoint(spot);
         _waypointSpotsQueueTemp.Add(spot);
      }

      // Remove the processed spots from the queue
      foreach (WorldMapSpot spot in _waypointSpotsQueueTemp) {
         _waypointSpotsQueue.Remove(spot);
      }
      _waypointSpotsQueueTemp.Clear();
   }

   private void instantiateSceneWaypoint (WorldMapSpot spot) {
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
      waypoint.spot = spot;
      waypoint.spot.type = WorldMapSpot.SpotType.Waypoint;
      waypoint.displayName = $"Waypoint {_waypoints.Count}";
      waypoint.transform.localPosition = WorldMapManager.self.getPositionFromSpot(Global.player.areaKey, spot);

      // Add a minimap icon for the waypoint
      Minimap.self.addWaypointIcon(area, waypoint);
   }

   public void createWaypoint (WorldMapSpot spot) {
      if (!_waypointSpots.Contains(spot)) {
         _waypointSpots.Add(spot);
      }

      _waypointSpotsQueue.Add(spot);
   }

   public void destroyWaypoint (WorldMapSpot spot) {
      // Deletes the logical waypoint
      _waypointSpots.Remove(spot);

      WorldMapWaypoint waypoint = _waypoints.Find(_ => _.spot == spot);
      if (waypoint == null) {
         return;
      }

      // Deletes the physical waypoint
      destroySceneWaypoint(waypoint);
   }

   private void destroySceneWaypoint (WorldMapWaypoint waypoint) {
      if (waypoint == null) {
         return;
      }
      
      // Destroy the instanced waypoint scene object
      Destroy(waypoint.gameObject);

      // Removes the waypoint from storage
      _waypoints.Remove(waypoint);

      // Removes the waypoint from the minimap
      Minimap.self.deleteWaypointIcon(waypoint);
   }

   public List<WorldMapWaypoint> getWaypoints () {
      return _waypoints;
   }
   
   public List<WorldMapSpot> getWaypointSpots () {
      return _waypointSpots;
   }

   public void refreshWaypoints () {
      foreach (WorldMapSpot spot in _waypointSpots.ToArray()) {
         destroyWaypoint(spot);
         createWaypoint(spot);
      }
   }

   #region Private Variables

   // The scene waypoints that have been instanced so far
   private List<WorldMapWaypoint> _waypoints = new List<WorldMapWaypoint>();

   // The set of spots that still need to be converted into waypoints
   private List<WorldMapSpot> _waypointSpotsQueue = new List<WorldMapSpot>();
   private List<WorldMapSpot> _waypointSpotsQueueTemp = new List<WorldMapSpot>();
   
   // The set of spots that have been converted into waypoints so far
   private List<WorldMapSpot> _waypointSpots = new List<WorldMapSpot>();
   
   #endregion
}
