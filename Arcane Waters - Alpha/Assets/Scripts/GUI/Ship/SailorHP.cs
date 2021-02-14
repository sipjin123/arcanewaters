using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class SailorHP : MonoBehaviour
{
   #region Public Variables

   // The sailor statuses
   public enum Status
   {
      None,
      Healthy,
      Damaged,
      KnockedOut,
      Hidden
   }

   // The image component
   public Image image;

   // The sprites for the different sailor statuses
   public Sprite healthySprite;
   public Sprite damagedSprite;
   public Sprite knockedOutSprite;

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
            break;
         case Status.Hidden:
         default:
            deactivate();
            break;
      }

      _status = status;
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

   #endregion

}
