using UnityEngine;
using Cinemachine;

public class MapCameraBounds : MonoBehaviour
{
   #region Public Variables

   // The confiner which forces the camera to stay within the map
   public CinemachineConfiner confiner;

   #endregion

   private void Awake () {
      _boundsCollider = GetComponent<PolygonCollider2D>();
   }

   private void Update () {
      // We want to adjust the camera bounds so bottom of the map is not covered by the UI bar
      if (_previousBottomBarHeight != ChatPanel.bottomBarWorldSpaceHeight) {
         _previousBottomBarHeight = ChatPanel.bottomBarWorldSpaceHeight;

         _boundsCollider.points = new Vector2[] {
            new Vector2(_bounds.min.x, _bounds.min.y - ChatPanel.bottomBarWorldSpaceHeight / transform.localScale.y),
            new Vector2(_bounds.min.x, _bounds.max.y),
            new Vector2(_bounds.max.x, _bounds.max.y),
            new Vector2(_bounds.max.x, _bounds.min.y - ChatPanel.bottomBarWorldSpaceHeight / transform.localScale.y)
         };

         if (confiner != null) {
            confiner.InvalidatePathCache();
         }
      }
   }

   public void setMapBoundingBox (Bounds bounds) {
      _bounds = bounds;

      // Initialize the camera bounds to match map bounds perfectly
      _boundsCollider.points = new Vector2[] {
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.min.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.min.y)
      };

      // Mark bottom bar as changed to force a recalculation
      _previousBottomBarHeight = 0;
   }

   public Collider2D getCameraBoundsShape () {
      return _boundsCollider;
   }

   #region Private Variables

   // The polygon who's bounds we are adjusting
   private PolygonCollider2D _boundsCollider = null;

   // This is the bounding box around the map
   private Bounds _bounds;

   // Previously applied bottom bar height
   private float _previousBottomBarHeight = 0;

   #endregion
}
