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
      NewLocationUnlocked = 2,
      VoyageCompleted = 3,
   }

   // The notification type
   public Type type;

   // The action to perform when the user confirms the notification
   public UnityAction action;

   // Gets set to true when the notification panel must be closed when pressing the confirm button
   public bool shouldCloseAtConfirm;

   #endregion

   public Notification (Type type, UnityAction action, bool shouldCloseAtConfirm = true) {
      this.type = type;
      this.action = action;
      this.shouldCloseAtConfirm = shouldCloseAtConfirm;
   }

   public string getMessage () {
      switch (type) {
         case Type.ReturnToVoyage:
            return "Go to the docks to return to your voyage!";
         case Type.NewLocationUnlocked:
            return "A new locale is accessible!";
         case Type.VoyageCompleted:
            return "Voyage complete!";
         default:
            return "";
      }
   }

   public string getButtonText () {
      switch (type) {
         case Type.NewLocationUnlocked:
         case Type.VoyageCompleted:
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
         case Type.NewLocationUnlocked:
         case Type.VoyageCompleted:
            return false;
         default:
            return true;
      }
   }

   #region Private Variables

   #endregion
}
