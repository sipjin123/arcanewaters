﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WorldMapPanelWaypointsContainer : MonoBehaviour
{
   #region Public Variables

   // Reference to the prefab used to create indicators
   public GameObject mapWaypointPrefab;

   // The sprite used to represent waypoints
   public Sprite waypointSprite;

   #endregion

   public void clearWaypoints () {
      foreach (WorldMapPanelWaypoint waypoint in _waypoints) {
         Destroy(waypoint.gameObject);
      }

      _waypoints.Clear();
   }

   public void addWaypoints (IEnumerable<WorldMapSpot> spots) {
      foreach (WorldMapSpot spot in spots) {
         WorldMapPanelWaypoint newWaypoint = createWaypoint(spot);
         positionWaypoint(newWaypoint);
         textureWaypoint(newWaypoint);
      }
   }

   private WorldMapPanelWaypoint createWaypoint (WorldMapSpot spot) {
      GameObject o = Instantiate(mapWaypointPrefab);
      WorldMapPanelWaypoint waypoint = o.GetComponent<WorldMapPanelWaypoint>();
      waypoint.spot = spot;
      _waypoints.Add(waypoint);
      return waypoint;
   }

   public void removeWaypoint (WorldMapSpot spot) {
      WorldMapPanelWaypoint waypoint = _waypoints.FirstOrDefault(_ => _.spot == spot);

      if (waypoint != null) {
         Destroy(waypoint.gameObject);
         _waypoints.Remove(waypoint);
      }
   }

   private void positionWaypoint (WorldMapPanelWaypoint waypoint) {
      waypoint.transform.SetParent(transform);

      Vector2Int cellSize = WorldMapPanel.self.cellSize;
      Vector2Int mapDimensions = WorldMapPanel.self.mapDimensions;

      float computedX = waypoint.spot.worldX * cellSize.x + waypoint.spot.areaX / waypoint.spot.areaWidth * cellSize.x;
      float computedY = (mapDimensions.y - 1 - waypoint.spot.worldY) * cellSize.y - waypoint.spot.areaY / waypoint.spot.areaHeight * cellSize.y;

      waypoint.transform.localPosition = new Vector3(computedX, -computedY);
   }

   private void textureWaypoint (WorldMapPanelWaypoint waypoint) {
      waypoint.setSprite(waypointSprite);
   }

   public List<WorldMapPanelWaypoint> getWaypointsWithinArea (WorldMapPanelAreaCoords mapPanelAreaCoords) {
      List<WorldMapPanelWaypoint> waypoints = new List<WorldMapPanelWaypoint>();

      foreach (WorldMapPanelWaypoint waypoint in _waypoints) {
         WorldMapAreaCoords waypointAreaCoords = new WorldMapAreaCoords(waypoint.spot.worldX, waypoint.spot.worldY);
         WorldMapPanelAreaCoords waypointPanelAreaCoords = WorldMapPanel.self.transformCoords(waypointAreaCoords);

         if (mapPanelAreaCoords == waypointPanelAreaCoords) {
            waypoints.Add(waypoint);
         }
      }

      return waypoints;
   }

   public void highlightWaypoint (WorldMapSpot spot, bool show) {
      WorldMapPanelWaypoint waypoint = _waypoints.FirstOrDefault(_ => _.spot == spot);

      if (waypoint != null && waypoint.rect != null) {
         waypoint.rect.localScale += show ? Vector3.one * 0.5f : -Vector3.one * 0.5f;
      }
   }

   #region Private Variables

   // Waypoints registry
   private List<WorldMapPanelWaypoint> _waypoints = new List<WorldMapPanelWaypoint>();

   #endregion
}