using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PendingActionManager : GenericGameManager
{
   #region Public Variables

   #endregion

   [ServerOnly]
   protected override void Awake () {
      base.Awake();
      D.log("This is a Server build, proceeding to check for pending actions each 5 seconds");
      InvokeRepeating(nameof(checkForPendingActions), 0, 10f);
   }

   [ServerOnly]
   private void checkForPendingActions () {
      // Only the master server performs this operation
      NetworkedServer server = ServerNetworkingManager.self.server;
      if (server == null || !server.isMasterServer()) {
         return;
      }

      PendingActionServerType serverType = PendingActionServerType.Development;
      string branch = Util.getBranchType();

      // Getting the current server type
      if (MyNetworkManager.self.serverOverride == MyNetworkManager.ServerType.Localhost) {
         serverType = PendingActionServerType.Localhost;
      } else if (branch == "Production") {
         serverType = PendingActionServerType.Production;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<PendingActionInfo> pendingActions = DB_Main.getPendingActions(serverType);

         // If we have pending actions
         if (pendingActions.Count > 0) {
            D.log("I'm a master server, and I'll process the pending actions!");

            foreach (PendingActionInfo pendingAction in pendingActions) {
               Dictionary<string, object> pendingJson = MiniJSON.Json.Deserialize(pendingAction.actionJson) as Dictionary<string, object>;

               switch (pendingAction.actionType) {
                  // For mutes, we get the targetAccId and targetUsrId from the pending action Json. Then, we send the muteInfo to the player, if connected, and show the message
                  case PendingActionType.NotifyMute:
                     PenaltyInfo muteInfo = DB_Main.getPenaltyInfoForAccount(int.Parse(pendingJson["targetAccId"].ToString()), PenaltyType.Mute);

                     // If we found a mute penalty, currently active
                     if (muteInfo != null) {
                        UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                           ServerNetworkingManager.self.setPenaltyInPlayerEntityByAccount(muteInfo.targetAccId, muteInfo);
                        });
                     }

                     break;
                  case PendingActionType.NotifyBan:
                     PenaltyInfo banInfo = DB_Main.getPenaltyInfoForAccount(int.Parse(pendingJson["targetAccId"].ToString()), PenaltyType.Ban);

                     // If we found a ban penalty, currently active
                     if (banInfo != null) {
                        UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                           ServerNetworkingManager.self.setPenaltyInPlayerEntityByAccount(banInfo.targetAccId, banInfo);
                        });
                     }
                     break;
                  case PendingActionType.LiftMute:
                  case PendingActionType.LiftBan:
                     int targetAccId = int.Parse(pendingJson["targetAccId"].ToString());
                     PenaltyType penaltyType = (PenaltyType) int.Parse(pendingJson["penaltyType"].ToString());

                     // Lifting the penalty in the database
                     DB_Main.liftPenaltyForAccount(targetAccId, penaltyType);

                     // If the user is muted and playing, lift the mute penalty (normal or stealth)
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        ServerNetworkingManager.self.liftPenaltyInPlayerEntityByAccount(targetAccId, penaltyType);
                     });

                     break;
                  case PendingActionType.KickPlayer:
                     PenaltyInfo kickInfo = DB_Main.getPenaltyInfoForAccount(int.Parse(pendingJson["targetAccId"].ToString()), PenaltyType.Kick);

                     if (kickInfo != null) {
                        UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                           ServerNetworkingManager.self.setPenaltyInPlayerEntityByAccount(kickInfo.targetAccId, kickInfo);
                        });
                     }
                     break;
               }

               DB_Main.completePendingAction(pendingAction.id);
            }
         }
      });
   }

   #region Private Variables

   #endregion
}
