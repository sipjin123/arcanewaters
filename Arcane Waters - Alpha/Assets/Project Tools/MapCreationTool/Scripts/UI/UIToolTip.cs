using UnityEngine;
using UnityEngine.EventSystems;

namespace MapCreationTool
{
   public class UIToolTip : ToolTip, IPointerEnterHandler, IPointerExitHandler
   {
      #region Public Variables

      [Tooltip(" Whether the component should try to destroy EventTrigger componenet on awake for legacy reasons")]
      public bool destroyEventTriggerOnAwake = true;

      #endregion

      private void Awake () {
         if (destroyEventTriggerOnAwake) {
            EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
            if (eventTrigger != null) {
               Destroy(eventTrigger);
            }
         }
      }

      public void OnPointerEnter (PointerEventData eventData) {
         pointerEnter();
      }

      public void OnPointerExit (PointerEventData eventData) {
         pointerExit();
      }

      #region Private Variables

      #endregion
   }
}

