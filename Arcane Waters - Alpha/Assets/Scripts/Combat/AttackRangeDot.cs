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

   // The animator for the deploy animation
   public Animator animator;

   #endregion

   public void setPosition (float angle, float radius) {
      transform.localPosition = Vector3.zero;

      // Move the image locally to the radius position
      Util.setLocalX(containerTr, radius);

      // Rotate to the angle
      transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
   }

   public void show () {
      animator.SetBool("visible", true);

      // Slightly randomizes the animation speed
      animator.SetFloat("speed", Random.Range(0.5f, 1.5f));
   }

   public void hide () {
      animator.SetBool("visible", false);
   }

   #region Private Variables

   #endregion
}
