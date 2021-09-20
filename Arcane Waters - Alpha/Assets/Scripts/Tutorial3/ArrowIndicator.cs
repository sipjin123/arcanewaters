using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ArrowIndicator : MonoBehaviour {
   #region Public Variables

   // The parameter for smooth movement (smaller is faster)
   public static float SMOOTH_TIME = 0.05f;

   // The arrows pointing in different directions
   public GameObject northArrow;
   public GameObject southArrow;
   public GameObject eastArrow;
   public GameObject westArrow;

   // The arrow pointing down, placed above the target, when it is visible
   public GameObject downArrow;

   // If the target is on screen
   public bool isOnScreen;

   // The screen point in world view
   public Vector3 screenPoint;

   // If the arrow is horrizontal
   public bool isHorizontal;

   // If this arrow is active
   public bool isActive = true;

   #endregion

   protected virtual void Start () {
      _mainCamera = Camera.main;
   }

   protected virtual void Update () {
      hideArrows();

      if (Global.player == null || _target == null || _mainCamera == null || !isActive) {
         return;
      }

      screenPoint = _mainCamera.WorldToViewportPoint(_target.transform.position, Camera.MonoOrStereoscopicEye.Mono);
      isOnScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

      // Calculate the screen size
      Vector2 screenSize = new Vector2(
         _mainCamera.orthographicSize * 2f * ((float) Screen.width / Screen.height),
         _mainCamera.orthographicSize * 2f);

      // We place the arrow a little away from the screen border. This allows to avoid going behind permanent UI panels.
      screenSize -= new Vector2(1f, 1.8f);

      Rect cameraRect = new Rect((Vector2) _mainCamera.transform.position - screenSize / 2, screenSize);

      // Clamp the target to the camera bounds
      Vector2 clampedTarget = new Vector2(
         Mathf.Clamp(_target.transform.position.x, cameraRect.xMin, cameraRect.xMax),
         Mathf.Clamp(_target.transform.position.y, cameraRect.yMin, cameraRect.yMax));

      // If the current arrow position is too far away, teleport it
      if (Vector2.Distance(transform.position, clampedTarget) > 2f) {
         Util.setXY(transform, clampedTarget);
      } else {
         // Progressively move the arrow to the target, to avoid stuttering due to the camera movement
         Util.setXY(transform, Vector2.SmoothDamp(transform.position, clampedTarget, ref _velocity,
            SMOOTH_TIME, float.MaxValue, Time.deltaTime));
      }

      // When the target is visible, switch to an arrow that points down on it
      if (cameraRect.Contains(_target.transform.position) || isOnScreen) {
         downArrow.SetActive(true);
      } else {
         // Show the correct arrow sprite
         if (_target.transform.position.x > _mainCamera.transform.position.x + screenSize.x / 2) {
            eastArrow.SetActive(true);
            isHorizontal = true;
         } else if (_target.transform.position.x < _mainCamera.transform.position.x - screenSize.x / 2) {
            westArrow.SetActive(true);
            isHorizontal = true;
         } else if (_target.transform.position.y > _mainCamera.transform.position.y + screenSize.y / 2) {
            northArrow.SetActive(true);
            isHorizontal = false;
         } else {
            southArrow.SetActive(true);
            isHorizontal = false;
         }
      }
   }

   public void deactivate () {
      _target = null;
      gameObject.SetActive(false);
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

      if (downArrow.activeSelf) {
         downArrow.SetActive(false);
      }
   }

   public void setTarget (GameObject obj) {
      _target = obj;
   }

   #region Private Variables

   // The currently targetted object
   [SerializeField]
   protected GameObject _target = null;

   // A cached reference to the main camera
   [SerializeField]
   protected Camera _mainCamera = null;

   // A velocity parameter used for smooth movement
   protected Vector2 _velocity;

   #endregion
}
