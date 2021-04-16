using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class Notification
{
   #region Public Variables

   // The notification types
   public enum Type
   {
      None = 0,
      ReturnToVoyage = 1,
      NewLocationUnblocked = 2,
   }

   // The notification type
   public Type type;

   // The action to perform when the user confirms the notification
   public UnityAction action;

   #endregion

   public Notification (Type type, UnityAction action) {
      this.type = type;
      this.action = action;
   }

   public string getMessage () {
      switch (type) {
         case Type.ReturnToVoyage:
            return "Go to the docks to return to your voyage!";
         case Type.NewLocationUnblocked:
            return "A new locale is accessible!";
         default:
            return "";
      }
   }

   public string getButtonText () {
      switch (type) {
         case Type.NewLocationUnblocked:
            return "View Map";
         default:
            return "Got it!";
      }
   }

   public bool canBeDisabled () {
      return canBeDisabled(type);
   }

   public static bool canBeDisabled (Type type) {
      switch (type) {
         case Type.NewLocationUnblocked:
            return false;
         default:
            return true;
      }
   }

   #region Private Variables

   #endregion
}
