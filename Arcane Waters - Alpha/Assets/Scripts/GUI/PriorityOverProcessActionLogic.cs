using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class PriorityOverProcessActionLogic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables
      
   #endregion

   private void Awake () {
      _transform = GetComponent<RectTransform>();
      if (!_transform) {
         this.enabled = false;
      }
   }

   // Determines whether any UI component with this script attached is hovered by mouse
   public static bool isAnyHovered () {
      return _hoveredElements.Count > 0;
   }

   public void OnPointerEnter (PointerEventData pointerEventData) {
      if (_transform != null && !_hoveredElements.Contains(_transform)){
         _hoveredElements.Add(_transform);
      }
   }

   public void OnPointerExit (PointerEventData pointerEventData) {
      _hoveredElements.Remove(_transform);
   }

   public void removeFromList () {
      _hoveredElements.Remove(_transform);
   }

   private void OnDisable () {
      if (_transform != null && _transform.gameObject.activeSelf == false) {
         _hoveredElements.Remove(_transform);
      }
   }

   #region Private Variables

   // Store hovered elements
   private static List<RectTransform> _hoveredElements = new List<RectTransform>();

   // Current RectTransform
   private RectTransform _transform;

   #endregion
}
