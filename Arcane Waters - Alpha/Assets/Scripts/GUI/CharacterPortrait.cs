using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CharacterPortrait : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The character stack
   public CharacterStack characterStack;

   // The icon displayed when the portrait is disabled
   public GameObject disabledIcon;

   // The click events
   public UnityEvent pointerEnterEvent;
   public UnityEvent pointerExitEvent;

   #endregion

   public void setPortrait (UserObjects userObjects) {
      // Update the character stack
      characterStack.updateLayers(userObjects);
      characterStack.setDirection(Direction.East);
      characterStack.pauseAnimation();
   }

   public void enable () {
      if (disabledIcon.activeSelf) {
         disabledIcon.SetActive(false);
      }
   }

   public void disable () {
      if (!disabledIcon.activeSelf) {
         disabledIcon.SetActive(true);
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      pointerEnterEvent.Invoke();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      pointerExitEvent.Invoke();
   }

   #region Private Variables

   #endregion
}
