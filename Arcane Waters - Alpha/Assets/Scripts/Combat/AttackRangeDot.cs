using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackRangeDot : MonoBehaviour
{
   #region Public Variables

   // The container of the dot
   public Transform containerTr;

   // The renderer of the image
   public SpriteRenderer imageRenderer;

   // The animator for the deploy animation
   public Animator animator;

   #endregion

   public void Awake () {
      gameObject.SetActive(false);
   }

   public void setPosition (AttackRangeCircle attackRangeCircle, float angle, float radius) {
      gameObject.SetActive(true);
      _attackRangeCircle = attackRangeCircle;
      transform.localPosition = Vector3.zero;

      // Move the image locally to the radius position
      Util.setLocalX(containerTr, radius);

      // Rotate to the angle
      transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
   }

   public void Update () {
      // Keep the image horizontal
      imageRenderer.transform.rotation = Quaternion.identity;

      // Hide the dot when over a land tile
      if (_attackRangeCircle.isOverLandTile(imageRenderer.transform.position)) {
         imageRenderer.enabled = false;
      } else {
         imageRenderer.enabled = true;
      }
   }

   public void show () {
      gameObject.SetActive(true);
      animator.SetBool("visible", true);

      // Slightly randomizes the animation speed
      animator.SetFloat("speed", Random.Range(0.75f, 1.25f));
   }

   public void hide () {
      animator.SetBool("visible", false);
      gameObject.SetActive(false);
   }

   #region Private Variables

   // A reference to the circle this dot is part of
   private AttackRangeCircle _attackRangeCircle;

   #endregion
}
