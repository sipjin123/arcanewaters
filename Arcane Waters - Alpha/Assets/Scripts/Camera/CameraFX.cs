using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CameraFX : MonoBehaviour {
   #region Public Variables

   // How long it takes to transition to the desired intensity
   public static float TRANSITION_TIME = 3f;

   // The minimum intensity of this effect
   public float minIntensity = 0f;

   // The maximum intensity of this effect
   public float maxIntensity = 1f;

   #endregion

   public void setTargetIntensity (float targetIntensity, float totalTransitionTime) {
      StopAllCoroutines();
      StartCoroutine(CO_SetTargetIntensity(targetIntensity, totalTransitionTime));
   }

   public void setMinIntensity (float totalTransitionTime) {
      StopAllCoroutines();
      StartCoroutine(CO_SetTargetIntensity(minIntensity, totalTransitionTime));
   }

   public void setMaxIntensity (float totalTransitionTime) {
      StopAllCoroutines();
      StartCoroutine(CO_SetTargetIntensity(maxIntensity, totalTransitionTime));
   }

   protected IEnumerator CO_SetTargetIntensity (float targetIntensity, float totalTransitionTime) {
      float startTime = Time.time;
      float startIntensity = _currentIntensity;

      // Keep waiting a frame until we've waited for the specified delay
      while (Time.time - startTime < totalTransitionTime) {
         float timePassed = Time.time - startTime;
         float lerpTime = (timePassed / totalTransitionTime);

         // Smoothly interpolate towards the target intensity
         _currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, lerpTime);

         yield return null;
      }

      // Now make sure our intensity has reached the specified amount
      _currentIntensity = targetIntensity;
   }

   #region Private Variables

   // The current intensity
   protected float _currentIntensity;

   // The target intensity
   protected float _targetIntensity;

   #endregion
}
