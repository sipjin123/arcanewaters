using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class ItemDropZone : MonoBehaviour
{

   #region Public Variables

   // The rect transform of this object
   public RectTransform rectTransform;

   #endregion

   public bool isInZone(Vector2 screenPoint) {
      return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint);
   }

   #region Private Variables

   #endregion
}