using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class MouseOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // References to all gameObjects that will be activated when this object is moused over, and deactivated when this object is moused off
   public List<GameObject> activateOnMouseOver;

   #endregion

   public void OnPointerEnter (PointerEventData eventData) {
      foreach (GameObject obj in activateOnMouseOver) {
         obj.SetActive(true);
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      foreach (GameObject obj in activateOnMouseOver) {
         obj.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
