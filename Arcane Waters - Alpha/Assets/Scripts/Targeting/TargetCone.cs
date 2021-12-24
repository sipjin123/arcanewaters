using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using System;

public class TargetCone : MonoBehaviour {
   #region Public Variables

   // The radius at which the cone ends
   public float coneOuterRadius;

   // The radius at which the cone begins
   public float coneInnerRadius;

   // Half of the angle of the cone to be drawn
   public float coneHalfAngle;

   // How many degrees of space to leave between the cone center and border
   public float coneBorderSpace;

   // References to  the two dotted lines that represent the edges of the cone
   public DottedLine dottedLineLower, dottedLineUpper;

   // Reference to the sprite that represents the center of the cone
   public SpriteRenderer coneCenter;

   #endregion

   private void Awake () {
      _coneMat = coneCenter.material;
   }

   public void updateCone (bool updateInputs) {
      if (updateInputs) {
         _toMouse = Util.getMousePos(transform.position) - transform.position;
         _toMouse = _toMouse.normalized;
      }

      float mouseAngle = Util.angle(_toMouse);

      Vector2 rotatePos = ExtensionsUtil.Rotate(_toMouse, coneHalfAngle);
      Vector2 rotateNeg = ExtensionsUtil.Rotate(_toMouse, -coneHalfAngle);

      dottedLineLower.lineStart.position = transform.position + (rotateNeg * coneInnerRadius).ToVector3();
      dottedLineLower.lineEnd.position = transform.position + (rotateNeg * coneOuterRadius).ToVector3();

      dottedLineUpper.lineStart.position = transform.position + (rotatePos * coneInnerRadius).ToVector3();
      dottedLineUpper.lineEnd.position = transform.position + (rotatePos * coneOuterRadius).ToVector3();

      updateMaterial(mouseAngle);

      dottedLineLower.updateLine();
      dottedLineUpper.updateLine();
   }

   public void setFillColor (Color newColor) {
      _coneMat.SetColor("_Color", newColor);
   }

   private void updateMaterial (float mouseAngle) {
      Vector3 pos = transform.position;
      pos.z = 0.0f;
      _coneMat.SetVector("_Position", pos);
      _coneMat.SetFloat("_Radius", coneOuterRadius);
      _coneMat.SetFloat("_InnerRadius", coneInnerRadius);
      _coneMat.SetFloat("_HalfAngle", coneHalfAngle - coneBorderSpace);
      _coneMat.SetFloat("_MiddleAngle", mouseAngle);
   }

   // Provides visual feedback to indicate that this target has been confirmed
   public void targetingConfirmed (Action onTargetingComplete) {
      StartCoroutine(CO_OnTargetingConfirmed(onTargetingComplete));
   }

   private IEnumerator CO_OnTargetingConfirmed (Action onTargetingComplete) {
      // Change color to red
      DOTween.To(() => coneCenter.material.color, x => setConeColor(x), Color.red, 0.4f);
      DOTween.To(() => coneBorderSpace, x => coneBorderSpace = x, 0.0f, 0.4f).SetEase(Ease.OutCubic);

      float timer = 0.4f;

      while (timer > 0.0f) {
         timer -= Time.deltaTime;
         updateCone(false);
         yield return null;
      }

      // Fade out
      DOTween.To(() => coneCenter.material.color, x => setConeColor(x), Color.clear, 0.3f);
      yield return new WaitForSeconds(0.3f);

      // Disable self, and reset variables
      gameObject.SetActive(false);
      setConeColor(Color.white);
      coneBorderSpace = 1.5f;
      onTargetingComplete?.Invoke();
   }

   public void setConeColor (Color newColor) {
      coneCenter.material.color = newColor;
      dottedLineLower.setLineColor(newColor);
      dottedLineUpper.setLineColor(newColor);
   }

   #region Private Variables

   // Reference to the material on the cone center's sprite renderer
   private Material _coneMat;

   // Stores the vector from the player to their mouse position
   private Vector2 _toMouse;

   #endregion
}
