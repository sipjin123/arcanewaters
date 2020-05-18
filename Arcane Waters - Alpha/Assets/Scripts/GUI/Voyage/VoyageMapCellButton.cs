using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class VoyageMapCellButton : MonoBehaviour, IPointerDownHandler,
   IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
   #region Public Variables

   #endregion

   public void Awake () {
      _cell = GetComponentInParent<VoyageMapCell>();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      _cell.onPointerEnterButton();
   }

   public void OnPointerExit (PointerEventData eventData) {
      _cell.onPointerExitButton();
   }

   public void OnPointerDown (PointerEventData eventData) {
      _cell.onPointerDownOnButton();
   }

   public void OnPointerUp (PointerEventData eventData) {
      _cell.OnPointerUpOnButton();
   }

   public void OnPointerClick (PointerEventData eventData) {
      _cell.onPointerClickButton();
   }

   #region Private Variables

   // The parent cell
   private VoyageMapCell _cell;

   #endregion
}
