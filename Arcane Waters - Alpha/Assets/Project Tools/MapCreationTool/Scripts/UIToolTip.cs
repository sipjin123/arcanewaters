using UnityEngine;
using UnityEngine.EventSystems;

namespace MapCreationTool
{
   public class UIToolTip : ToolTip
   {
      private void Awake () {
         EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerEnter, (e) => pointerEnter());
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerExit, (e) => pointerExit());
      }
   }
}

