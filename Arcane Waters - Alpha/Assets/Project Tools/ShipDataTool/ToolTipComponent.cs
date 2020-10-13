using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class ToolTipComponent : MonoBehaviour {
   #region Public Variables

   // The content of the tooltip
   public string message;

   #endregion

   private void Awake () {
      EventTrigger eventTrigger = GetComponent<EventTrigger>();
      EventTrigger.Entry eventEntry = new EventTrigger.Entry();
      eventEntry.eventID = EventTriggerType.PointerEnter;
      eventEntry.callback.AddListener((data) => { onHoverEnter((PointerEventData)data); });
      eventTrigger.triggers.Add(eventEntry);
      
      EventTrigger.Entry eventEntryExit = new EventTrigger.Entry();
      eventEntryExit.eventID = EventTriggerType.PointerExit;
      eventEntryExit.callback.AddListener((data) => { onHoverExit(); });
      eventTrigger.triggers.Add(eventEntryExit);
   }

   public void onHoverExit () {
      TooltipHandler.self.cancelToolTip();
   }

   public void onHoverEnter (PointerEventData eventData) {
      TooltipHandler.self.callToolTip(message, eventData.position);
   }

   #region Private Variables

   #endregion
}
