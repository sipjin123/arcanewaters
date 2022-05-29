using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class UserTrackingManager : MonoBehaviour
{
   #region Public Variables

   // Singleton
   public static UserTrackingManager self;

   #endregion

   private void Awake () {
      self = this;

      if (NetworkServer.active) {
         // Push tracked user actions to database periodically
         InvokeRepeating(nameof(pushPendingActions), 0, 2f);
      }
   }

   [Server]
   public void reportAction (NetEntity byUser, TrackedUserAction.Type type) {
      if (byUser.userId == 0 || byUser.accountId == 0) {
         throw new Exception("Reporting action by a user that doesn't have either userid or accountid: " + byUser.userId + ", " + byUser.accountId);
      }

      reportAction(byUser.userId, byUser.accountId, type);
   }

   [Server]
   public void reportAction (int userId, int accId, TrackedUserAction.Type type) {
      _pendingSaveActions.Add(new TrackedUserAction {
         id = 0,
         userId = userId,
         accId = accId,
         type = type,
         time = DateTime.UtcNow
      });
   }

   [Server]
   private void pushPendingActions () {
      if (_pendingSaveActions.Count == 0) {
         return;
      }

      // Copy over the actions that we want to push
      List<TrackedUserAction> actions = _pendingSaveActions;
      _pendingSaveActions = new List<TrackedUserAction>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fire the action saving to database and forget
         DB_Main.insertTrackedUserActions(actions);
      });
   }

   #region Private Variables

   // The actions that we need to save
   private List<TrackedUserAction> _pendingSaveActions = new List<TrackedUserAction>();

   #endregion
}
