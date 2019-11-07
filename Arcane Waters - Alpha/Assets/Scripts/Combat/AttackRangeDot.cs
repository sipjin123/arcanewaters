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

   // The collider of the dot
   public Collider2D dotCollider;

   // The colors for each attack zone
   public Color weakZoneColor;
   public Color normalZoneColor;
   public Color strongZoneColor;

   #endregion

   public void setPosition (float angle, float radius) {
      transform.localPosition = Vector3.zero;

      // Move the image locally to the radius position
      Util.setLocalX(containerTr, radius);

      // Rotate to the angle
      transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
   }

   public void Update () {
      // Keep the image horizontal
      imageRenderer.transform.rotation = Quaternion.identity;
   }

   public void OnTriggerEnter2D (Collider2D other) {
      // Check if the other object is the grid terrain (land tiles)
      Grid grid = other.transform.GetComponent<Grid>();

      // Hide the dot if entering land
      if (grid != null) {
         imageRenderer.enabled = false;
      }
   }

   public void OnTriggerExit2D (Collider2D other) {
      // Check if the other object is the grid terrain (land tiles)
      Grid grid = other.transform.GetComponent<Grid>();

      // Show the dot when leaving the land
      if (grid != null) {
         imageRenderer.enabled = true;
      }
   }

   public void show (AttackZone.Type attackZone) {
      dotCollider.enabled = true;
      animator.SetBool("visible", true);
      imageRenderer.enabled = true;

      // Slightly randomizes the animation speed
      animator.SetFloat("speed", Random.Range(0.75f, 1.25f));

      // Set the color of the dot for the attack zone
      switch (attackZone) {
         case AttackZone.Type.Weak:
            imageRenderer.color = weakZoneColor;
            break;
         case AttackZone.Type.Normal:
            imageRenderer.color = normalZoneColor;
            break;
         case AttackZone.Type.Strong:
            imageRenderer.color = strongZoneColor;
            break;
         default:
            imageRenderer.color = Color.white;
            break;
      }
   }

   public void hide () {
      dotCollider.enabled = false;
      animator.SetBool("visible", false);
      imageRenderer.enabled = false;
   }

   #region Private Variables

   #endregion
}
