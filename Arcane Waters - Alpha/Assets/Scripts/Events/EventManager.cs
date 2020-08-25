using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class EventManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static EventManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public static void StartListening (string eventName, UnityAction listener) {
      UnityEvent thisEvent = null;

      if (_eventDictionary.TryGetValue(eventName, out thisEvent)) {
         thisEvent.AddListener(listener);
      } else {
         thisEvent = new UnityEvent();
         thisEvent.AddListener(listener);
         _eventDictionary.Add(eventName, thisEvent);
      }
   }

   public static void StopListening (string eventName, UnityAction listener) {
      UnityEvent thisEvent = null;

      if (_eventDictionary.TryGetValue(eventName, out thisEvent)) {
         thisEvent.RemoveListener(listener);
      }
   }

   public static void TriggerEvent (string eventName) {
      UnityEvent thisEvent = null;

      if (_eventDictionary.TryGetValue(eventName, out thisEvent)) {
         thisEvent.Invoke();
      }
   }

   #region Private Variables

   // Our Event dictionary
   private static Dictionary<string, UnityEvent> _eventDictionary = new Dictionary<string, UnityEvent>();

   #endregion
}