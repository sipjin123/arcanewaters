using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MapCreationTool
{
   public class Utilities
   {
      public static void addPointerListener (
          EventTrigger trigger,
          EventTriggerType eventType,
          UnityAction<PointerEventData> callback) {
         EventTrigger.Entry entry = new EventTrigger.Entry();
         entry.eventID = eventType;
         entry.callback.AddListener((eventData) => { callback(eventData as PointerEventData); });
         trigger.triggers.Add(entry);
      }

      public static void warning (string message) {
         try {
            D.warning(message);
         } catch {
            Debug.LogWarning(message);
         }
      }
   }
}