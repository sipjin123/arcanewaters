using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class CircleFader : ClientMonoBehaviour {
   #region Public Variables

   // The circle Image
   public CameraFilterPack_FX_Spot spotEffect;

   // Self
   public static CircleFader self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Update () {
      // Note if our camera is currently being drawn like normal
      if (Camera.main.cullingMask == -1) {
         _lastDrawTime = Time.time;
      }

      // If something's gone wrong and our camera stopped drawing, reset it
      if (Time.time - _lastDrawTime > TIMEOUT_DURATION) {
         Debug.Log("Reseting camera draw settings.");
         Camera.main.clearFlags = CameraClearFlags.SolidColor;
         Camera.main.cullingMask = -1;
      }
   }

   public void doCircleFade () {
      _startTime = Time.time;
      _startLocation = Global.player.transform.position;
      _startingCamera = getActiveCamera();

      if (CameraManager.self != null) {
         StartCoroutine(CO_doCircleFade());
      }
   }

   protected IEnumerator CO_doCircleFade () {
      CameraFilterPack_FX_Spot spotEffect = Camera.main.GetComponent<CameraFilterPack_FX_Spot>();

      // Freeze the camera
      Camera.main.clearFlags = CameraClearFlags.Nothing;
      yield return null;
      Camera.main.cullingMask = 0;

      // Start without any effect showing at all
      spotEffect.Radius = 1f;
      spotEffect.center = getEffectCenter(_startLocation);
      spotEffect.enabled = true;
      while (spotEffect.Radius > -.1f) {
         spotEffect.Radius -= EFFECT_SPEED;
         yield return new WaitForSeconds(FRAME_LENGTH);
      }
      spotEffect.Radius = -.1f;

      // Wait until we're in the new area (or enough time passes)
      while (!hasCameraChanged() && Time.time - _startTime < TIMEOUT_DURATION) {
         yield return null;
      }

      // Turn the camera back on
      Camera.main.clearFlags = CameraClearFlags.SolidColor;
      yield return null;
      Camera.main.cullingMask = -1;
      yield return null;

      // Now work our way back to no effect showing
      Vector3 spotCenter = Global.player != null ? Global.player.transform.position : (Vector3) _startLocation;
      spotEffect.center = getEffectCenter(spotCenter);
      while (spotEffect.Radius < 1f) {
         spotEffect.Radius += EFFECT_SPEED;
         yield return new WaitForSeconds(FRAME_LENGTH);
      }
      spotEffect.Radius = 1f;
      spotEffect.enabled = false;
   }

   protected Vector2 getEffectCenter (Vector3 position) {
      Vector3 screenPoint = Camera.main.WorldToScreenPoint(position);
      Vector2 viewportPoint = Camera.main.ScreenToViewportPoint(screenPoint);

      return viewportPoint;
   }

   protected bool hasCameraChanged () {
      if (Global.player == null) {
         return false;
      }

      return _startingCamera != getActiveCamera();
   }

   protected ICinemachineCamera getActiveCamera () {
      if (Camera.main == null) {
         return null;
      }

      return Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;
   }

   #region Private Variables

   // The location we want to start at
   protected Vector2 _startLocation;

   // The time at which the effect start
   protected float _startTime;

   // The time at which the camera was last being drawn
   protected float _lastDrawTime;

   // The camera that was active initially
   protected ICinemachineCamera _startingCamera;

   // How long we give to each frame for the spot effect (smaller number means smoother animation)
   protected static float FRAME_LENGTH = .02f;

   // How quickly we change the effect radius (larger number means faster transition)
   protected static float EFFECT_SPEED = .1f;

   // The amount of time we wait for an area change, before giving up
   protected static float TIMEOUT_DURATION = 5f;

   #endregion
}
