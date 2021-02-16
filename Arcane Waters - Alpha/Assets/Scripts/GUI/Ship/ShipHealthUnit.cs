using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ShipHealthUnit : MonoBehaviour
{
   #region Public Variables

   // The unit statuses
   public enum Status
   {
      None = 0,
      Healthy = 1,
      Damaged = 2,
      Predicted = 3,
      Hidden = 4
   }

   // The image component
   public Image image;

   // The colors for the different statuses
   public Color healthyColor;
   public Color damagedColor;
   public Color predictedColor;
   public Color hiddenColor;

   #endregion

   public void setStatus (Status status) {
      if (_status == status) {
         return;
      }

      switch (status) {
         case Status.Healthy:
            image.color = healthyColor;
            break;
         case Status.Damaged:
            image.color = damagedColor;
            break;
         case Status.Predicted:
            image.color = predictedColor;
            break;
         case Status.Hidden:
         default:
            image.color = hiddenColor;
            break;
      }

      _status = status;
   }

   #region Private Variables

   // The current status
   private Status _status = Status.None;

   #endregion
}
