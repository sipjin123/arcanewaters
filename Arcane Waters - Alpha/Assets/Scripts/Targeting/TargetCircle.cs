using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using System;

public class TargetCircle : MonoBehaviour {
   #region Public Variables

   // Reference to the dotted circle that represents the outline of the circle
   public DottedCircle circleOutline;

   // Reference to the sprite that represents the center of the circle
   public SpriteRenderer circleCenter;

   #endregion

   private void Awake () {
      _outlineInitialRadius = circleOutline.circleRadius;
   }

   public void updateCircle (bool updateInputs) {
      if (updateInputs) {
         transform.position = Util.getMousePos();
      }
      
      if (circleOutline.updateCircle) {
         circleOutline.updateSegments();
      }
   }

   public void scaleCircle (float scale) {
      circleCenter.transform.localScale = Vector3.one * scale;
      circleOutline.circleRadius = _outlineInitialRadius * scale;
   }

   public void setFillColor (Color newColor) {
      circleCenter.color = newColor;
   }

   public void targetingConfirmed (Action onTargetingComplete) {
      StartCoroutine(CO_OnTargetingConfirmed(onTargetingComplete));
   }

   private IEnumerator CO_OnTargetingConfirmed (Action onTargetingComplete) {
      // Change color to red
      DOTween.To(() => circleCenter.material.color, x => setCircleColor(x), Color.red, 0.2f);
      DOTween.To(() => circleOutline.circleRadius, x => circleOutline.circleRadius = x, circleOutline.circleRadius * 0.9f, 0.3f).SetEase(Ease.InOutCubic);

      float timer = 0.3f;

      while (timer > 0.0f) {
         timer -= Time.deltaTime;
         updateCircle(false);
         yield return null;
      }

      // Wait for attack to finish
      yield return new WaitForSeconds(4.25f);

      // Fade out
      DOTween.To(() => circleCenter.material.color, x => setCircleColor(x), Color.clear, 0.3f);

      yield return new WaitForSeconds(0.3f);

      // Disable self, and reset variables
      gameObject.SetActive(false);
      setCircleColor(Color.white);
      circleOutline.circleRadius = _outlineInitialRadius;
      onTargetingComplete?.Invoke();
   }

   public void setCircleColor (Color newColor) {
      circleCenter.material.color = newColor;
      circleOutline.setCircleColor(newColor);
   }

   #region Private Variables

   // Stores the initial radius used by the dotted circle
   private float _outlineInitialRadius;

   // Stores the last position we were set to
   private Vector3 _lastPosition;

   #endregion
}
