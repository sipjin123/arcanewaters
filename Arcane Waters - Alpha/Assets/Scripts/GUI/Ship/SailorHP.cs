using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;

public class SailorHP : MonoBehaviour
{
   #region Public Variables

   // The sailor statuses
   public enum Status
   {
      None = 0,
      Healthy = 1,
      Damaged = 2,
      KnockedOut = 3,
      Hidden = 4
   }

   // The image component
   public Image image;

   // The sprites for the different sailor statuses
   public Sprite healthySprite;
   public Sprite damagedSprite;
   public Sprite knockedOutSprite;

   // The animator component
   public Animator animator;

   #endregion

   public void setStatus (Status status) {
      if (_status == status) {
         return;
      }

      switch (status) {
         case Status.Healthy:
            activate();
            image.sprite = healthySprite;
            break;
         case Status.Damaged:
            activate();
            image.sprite = damagedSprite;
            break;
         case Status.KnockedOut:
            activate();
            image.sprite = knockedOutSprite;
            animator.SetBool("isBlinking", false);
            break;
         case Status.Hidden:
         default:
            deactivate();
            break;
      }

      _status = status;
   }

   public void blink (int loopCount) {
      if (_status == Status.Hidden || _status == Status.KnockedOut) {
         return;
      }

      transform.DORewind();
      transform.DOShakeRotation(0.2f, Vector3.forward * 70.0f, vibrato: 40);

      if (loopCount <= 0) {
         return;
      }

      _blinkLoopsLeft = loopCount;
      animator.SetFloat("blinkSpeedMultiplier", loopCount);
      animator.SetBool("isBlinking", true);
   }

   public void onBlinkLoop () {
      _blinkLoopsLeft--;
      if (_blinkLoopsLeft <= 0) {
         animator.SetBool("isBlinking", false);
      }
   }

   public void activate() {
      if (!gameObject.activeSelf) {
         gameObject.SetActive(true);
      }
   }

   public void deactivate () {
      if (gameObject.activeSelf) {
         gameObject.SetActive(false);
      }
   }

   #region Private Variables

   // The current sailor status
   private Status _status = Status.None;

   // The number of blink loops left
   private int _blinkLoopsLeft = 0;

   #endregion
}
