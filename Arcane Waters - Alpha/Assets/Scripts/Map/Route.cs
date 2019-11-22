using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Route : MonoBehaviour {
   #region Public Variables

   // The Type of Route this is
   public enum Type { None = 0, Patrol = 1, Trade = 2 }

   // The Type of Route this is
   public Type routeType;

   // The Area this route is in
   [HideInInspector]
   public string areaKey;

   #endregion

   void Awake () {
      // Check which area we're in
      this.areaKey = GetComponentInParent<Area>().areaKey;

      // Get our list of waypoints
      _waypoints = new List<Waypoint>(GetComponentsInChildren<Waypoint>());
   }

   public List<Waypoint> getWaypoints () {
      return _waypoints;
   }

   public Waypoint getClosest (Vector2 pos) {
      Waypoint closest = _waypoints[0];
      float minDistance = float.MaxValue;

      foreach (Waypoint waypoint in _waypoints) {
         float thisDistance = Vector2.Distance(waypoint.transform.position, pos);
         if (thisDistance < minDistance) {
            closest = waypoint;
            minDistance = thisDistance;
         }
      }

      return closest;
   }

   #region Private Variables

   // Our waypoints
   protected List<Waypoint> _waypoints = new List<Waypoint>();

   #endregion
}
