using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

public class SubPanel : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   #endregion

   public virtual void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public virtual void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.rawPointerPress == this.gameObject) {
         // If the mouse is over the input field zone, select it through the panel black background
         if (!Panel.tryFocusChatInputField()) {
            // If the black background outside is clicked, hide the panel
            hide();
         }
      }
   }

   #region Private Variables

   #endregion
}

