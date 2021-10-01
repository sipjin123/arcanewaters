using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Text;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class PvpShopPopupPanel : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // The action to perform when the background outside the panel is clicked
   public UnityEvent onBackgroundClickedAction;

   #endregion

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.rawPointerPress == this.gameObject) {
         onBackgroundClickedAction?.Invoke();
      }
   }

   #region Private Variables

   #endregion
}
