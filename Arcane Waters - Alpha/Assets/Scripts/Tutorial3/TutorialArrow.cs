﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialArrow : MonoBehaviour
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

      if (Global.player == null || _target == null || _mainCamera == null) {
         return;
      }

      if (!TutorialManager3.self.isActive()) {
         deactivate();
         return;
      }

      // Calculate the screen size
      Vector2 screenSize = new Vector2(
         _mainCamera.orthographicSize * 2f * ((float) Screen.width / Screen.height),
         _mainCamera.orthographicSize * 2f);

      // We place the arrow a little away from the screen border. This allows to avoid going behind permanent UI panels.
      screenSize -= new Vector2(1f, 1.8f);

      Rect cameraRect = new Rect((Vector2)_mainCamera.transform.position - screenSize / 2, screenSize);

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
      if (cameraRect.Contains(_target.transform.position)) {
         downArrow.SetActive(true);
      } else {
         // Show the correct arrow sprite
         if (_target.transform.position.x > _mainCamera.transform.position.x + screenSize.x / 2) {
            eastArrow.SetActive(true);
         } else if (_target.transform.position.x < _mainCamera.transform.position.x - screenSize.x / 2) {
            westArrow.SetActive(true);
         } else if (_target.transform.position.y > _mainCamera.transform.position.y + screenSize.y / 2) {
            northArrow.SetActive(true);
         } else {
            southArrow.SetActive(true);
         }
      }
   }

   public void setTarget (string targetAreaKey) {
      if (!string.IsNullOrEmpty(targetAreaKey) ||
         TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLobby ||
         TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLeagueNotLobby) {
         gameObject.SetActive(true);
         _target = null;
         StopAllCoroutines();
         StartCoroutine(CO_PointTo(targetAreaKey));
      } else {
         deactivate();
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

   private IEnumerator CO_PointTo (string targetAreaKey) {
      // Wait until we have finished instantiating the area
      while (Global.player == null || AreaManager.self.getArea(Global.player.areaKey) == null) {
         yield return 0;
      }

      // Search for the correct warp (entrance to target)
      foreach (Warp warp in AreaManager.self.getArea(Global.player.areaKey).getWarps()) {
         if (string.Equals(warp.targetInfo.name, targetAreaKey)) {
            _target = warp.gameObject;
            break;
         }
      }

      // Special case for league maps
      if (TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLobby ||
         TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.SpawnInLeagueNotLobby) {
         foreach (GenericActionTrigger genericTrigger in AreaManager.self.getArea(Global.player.areaKey).GetComponentsInChildren<GenericActionTrigger>()) {
            if (genericTrigger.actionName == GenericActionTrigger.WARP_TO_LEAGUE_ACTION) {
               _target = genericTrigger.gameObject;
               break;
            }
         }
      }

      if (_target == null) {
         deactivate();
         yield break;
      }
   }

   #region Private Variables

   // The currently targetted object
   private GameObject _target = null;

   // A cached reference to the main camera
   private Camera _mainCamera = null;

   // A velocity parameter used for smooth movement
   private Vector2 _velocity;

   #endregion
}
