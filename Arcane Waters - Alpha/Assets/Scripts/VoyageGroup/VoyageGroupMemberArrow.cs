using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.InputSystem;

public class VoyageGroupMemberArrow : MonoBehaviour
{
   #region Public Variables

   // The parameter for smooth movement (smaller is faster)
   private static float SMOOTH_TIME = 0.05f;

   // The arrows pointing in different directions
   public GameObject northArrow;
   public GameObject southArrow;
   public GameObject eastArrow;
   public GameObject westArrow;

   // The arrow pointing down, placed above the target, when it is visible
   public GameObject downArrow;

   // The arrow labels
   public TextMeshPro[] arrowLabels;

   #endregion

   public void Start () {
      deactivate();

      // Cache a reference to the main camera if it doesn't exist
      if (_mainCamera == null) {
         _mainCamera = Camera.main;
      }
   }

   public void Update () {
      hideArrows();

      if (Global.player == null || !VoyageGroupManager.isInGroup(Global.player) || _mainCamera == null) {
         return;
      }

      if (!Keyboard.current.leftAltKey.isPressed && !Keyboard.current.rightAltKey.isPressed && !_cell.isMouseOver()) {
         return;
      }

      NetEntity targetEntity = EntityManager.self.getEntity(_targetUserId);
      if (targetEntity == null) {
         return;
      }

      // Calculate the screen size
      Vector2 screenSize = new Vector2(
         _mainCamera.orthographicSize * 2f * ((float) Screen.width / Screen.height),
         _mainCamera.orthographicSize * 2f);

      // We place the arrow a little away from the screen border. This allows to avoid going behind permanent UI panels.
      screenSize -= new Vector2(1.3f, 1f);

      Rect cameraRect = new Rect((Vector2) _mainCamera.transform.position - screenSize / 2, screenSize);

      // Clamp the target to the camera bounds
      Vector2 clampedTarget = new Vector2(
         Mathf.Clamp(targetEntity.transform.position.x, cameraRect.xMin, cameraRect.xMax),
         Mathf.Clamp(targetEntity.transform.position.y, cameraRect.yMin, cameraRect.yMax));

      // If the current arrow position is too far away, teleport it
      if (Vector2.Distance(transform.position, clampedTarget) > 1f) {
         Util.setXY(transform, clampedTarget);
      } else {
         // Progressively move the arrow to the target, to avoid stuttering due to the camera movement
         Util.setXY(transform, Vector2.SmoothDamp(transform.position, clampedTarget, ref _velocity,
            SMOOTH_TIME, float.MaxValue, Time.deltaTime));
      }

      if (cameraRect.Contains(targetEntity.transform.position)) {
         // When the target is visible, display the down arrow above it only if the member cell is hovered
         if (_cell.isMouseOver()) {
            downArrow.SetActive(true);
         }
      } else {
         // Show the correct arrow sprite
         if (targetEntity.transform.position.x > _mainCamera.transform.position.x + screenSize.x / 2) {
            eastArrow.SetActive(true);
         } else if (targetEntity.transform.position.x < _mainCamera.transform.position.x - screenSize.x / 2) {
            westArrow.SetActive(true);
         } else if (targetEntity.transform.position.y > _mainCamera.transform.position.y + screenSize.y / 2) {
            northArrow.SetActive(true);
         } else {
            southArrow.SetActive(true);
         }
      }
   }

   public void setTarget (VoyageGroupMemberCell cell, int userId, string userName) {
      _cell = cell;
      _targetUserId = userId;
      foreach (TextMeshPro label in arrowLabels) {
         label.text = userName;
      }
   }

   public void activate () {
      if (!gameObject.activeSelf) {
         gameObject.SetActive(true);
      }
   }

   public void deactivate () {
      if (gameObject.activeSelf) {
         gameObject.SetActive(false);
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

      if (downArrow.activeSelf) {
         downArrow.SetActive(false);
      }
   }

   #region Private Variables

   // The currently targetted user
   private int _targetUserId = -1;

   // The associated group member cell
   private VoyageGroupMemberCell _cell = null;

   // A cached reference to the main camera
   private Camera _mainCamera = null;

   // A velocity parameter used for smooth movement
   private Vector2 _velocity;

   #endregion
}
