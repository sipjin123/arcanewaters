using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaTargetPointer : MonoBehaviour {

   #region Public Variables

   // The arrows pointing in different directions
   public GameObject northArrow;
   public GameObject southArrow;
   public GameObject eastArrow;
   public GameObject westArrow;

   // We don't want to use the full size of the screen in order to avoid other UI elements, such us the minimap or the bottom menu bar
   public Vector2 screenSizeOffset = new Vector2(0.25f, 0.5f);

   #endregion

   public void Start () {
      // Cache a reference to the main camera if it doesn't exist
      if (_mainCamera == null) {
         _mainCamera = Camera.main;
      }
   }

   public void Update () {
      hideArrows();

      _targetShip = SelectionManager.self.selectedEntity;

      if (Global.player == null || _targetShip == null || _mainCamera == null) {
         return;
      }
      
      // Calculate the screen size
      Vector2 screenSize = new Vector2(
         _mainCamera.orthographicSize * 2f * ((float) Screen.width / Screen.height),
         _mainCamera.orthographicSize * 2f);

      // We place the arrow a little away from the screen border. This allows to avoid going behind permanent UI panels.
      screenSize -= screenSizeOffset;

      Rect cameraRect = new Rect((Vector2) _mainCamera.transform.position - screenSize / 2, screenSize);

      // Clamp the target to the camera bounds
      Vector2 clampedTarget = new Vector2(
         Mathf.Clamp(_targetShip.transform.position.x, cameraRect.xMin, cameraRect.xMax),
         Mathf.Clamp(_targetShip.transform.position.y, cameraRect.yMin, cameraRect.yMax));

      // If the current arrow position is too far away, teleport it
      if (Vector2.Distance(transform.position, clampedTarget) > 2f) {
         Util.setXY(transform, clampedTarget);
      } else {
         // Progressively move the arrow to the target, to avoid stuttering due to the camera movement
         Util.setXY(transform, Vector2.SmoothDamp(transform.position, clampedTarget, ref _velocity,
            SMOOTH_TIME, float.MaxValue, Time.deltaTime));
      }

      // When the target is visible, switch to an arrow that points down on it
      if (!cameraRect.Contains(_targetShip.transform.position)) {
         // Show the correct arrow sprite
         if (_targetShip.transform.position.x > _mainCamera.transform.position.x + screenSize.x / 2) {
            eastArrow.SetActive(true);
         } else if (_targetShip.transform.position.x < _mainCamera.transform.position.x - screenSize.x / 2) {
            westArrow.SetActive(true);
         } else if (_targetShip.transform.position.y > _mainCamera.transform.position.y + screenSize.y / 2) {
            northArrow.SetActive(true);
         } else {
            southArrow.SetActive(true);
         }
      }
   }

   public void hideArrows () {
      if (northArrow.activeSelf) {
         northArrow.SetActive(false);
      }

      if (southArrow.activeSelf) {
         southArrow.SetActive(false);
      }

      if (eastArrow.activeSelf) {
         eastArrow.SetActive(false);
      }

      if (westArrow.activeSelf) {
         westArrow.SetActive(false);
      }
   }

   #region Private Variables

   // The currently targetted warp
   private SeaEntity _targetShip = null;

   // A cached reference to the main camera
   private Camera _mainCamera = null;

   // A velocity parameter used for smooth movement
   private Vector2 _velocity;

   // The parameter for smooth movement (smaller is faster)
   private const float SMOOTH_TIME = 0.05f;

   #endregion
}
