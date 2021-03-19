using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using DG.Tweening;

public class CannonTargeter : MonoBehaviour {
   #region Public Variables

   // A reference to the dotted parabola that shows the projected arc of the cannonball
   public DottedParabola dottedParabola;

   // A reference to the dotted line that shows a straight line to the target location
   public DottedLine dottedLine;

   // A reference to the animator the for cannon charging up
   public Animator chargeAnimator;

   // A reference to the parent object of the charge animator, used to rotate and tween it
   public Transform animatorParent;

   // A reference to the sprite renderer of the charge up effect, used to change its color
   public SpriteRenderer chargeRenderer;

   // How far the player has charged up their cannon
   [HideInInspector]
   public float chargeAmount;

   // What the height of the apex of the dotted parabola should be
   [HideInInspector]
   public float parabolaHeight;

   #endregion

   public void setTarget (Vector3 targetPosition) {
      _targetPosition = targetPosition;
   }

   public void updateTargeter () {
      // Update target positions
      dottedParabola.parabolaEnd.position = _targetPosition;
      dottedLine.lineEnd.position = _targetPosition;
      
      // Update components
      dottedParabola.parabolaHeight = parabolaHeight;
      dottedParabola.updateParabola();
      dottedLine.updateLine();
      updateAnimator();
   }

   public void targetingConfirmed (Action onTargetingComplete) {
      StartCoroutine(CO_OnTargetingConfirmed(onTargetingComplete));
   }

   private void updateAnimator () {
      chargeAnimator.SetFloat(CHARGE_AMOUNT, chargeAmount);

      Vector2 toTarget = _targetPosition - transform.position;
      float aimAngle = -Util.angle(toTarget);
      animatorParent.rotation = Quaternion.Euler(0.0f, 0.0f, aimAngle);
   }

   private IEnumerator CO_OnTargetingConfirmed (Action onTargetingComplete) {
      // Play the fire animation and tween the cannon to show firing
      chargeAnimator.SetTrigger(FIRE);
      animatorParent.DOPunchScale(Vector3.up * 0.15f, 0.25f, 1);

      // Fade the visuals out
      float fadeTimer = 0.4f;
      while (fadeTimer > 0.0f) {
         fadeTimer -= Time.deltaTime;
         updateTargeter();
         setTargeterColor(new Color(1.0f, 1.0f, 1.0f, fadeTimer / 0.5f));
         yield return null;
      }

      // Disable targeter after completely faded
      gameObject.SetActive(false);
      setTargeterColor(Color.white);
      onTargetingComplete?.Invoke();
   }

   private void setTargeterColor (Color newColor) {
      chargeRenderer.color = newColor;
      dottedParabola.setParabolaColor(newColor);
      dottedLine.setLineColor(newColor);
   }

   #region Private Variables

   // The last position that the player targeted
   private Vector3 _targetPosition;

   // Animator parameter name for the cannon's charge amount
   private const string CHARGE_AMOUNT = "ChargeAmount";

   // Animator parameter name for the fire trigger
   private const string FIRE = "Fire";

   #endregion
}
