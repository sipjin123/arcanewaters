using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PendingActionManager : MonoBehaviour
{
   #region Public Variables

   #endregion

   private void Start () {
      //#if IS_SERVER_BUILD && CLOUD_BUILD
      //D.debug("This is a Server build and a Cloud build, proceed to check for pending actions each 5 seconds");
      //InvokeRepeating("checkForPendingActions", 0, 5f);
      //#endif
   }

   [ServerOnly]
   private void checkForPendingActions () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<PendingActionInfo> pendingActions = DB_Main.getPendingActions();

         // If we have pending actions
         if (pendingActions.Count > 0) {
            foreach (PendingActionInfo pendingAction in pendingActions) {
               Dictionary<string, object> pendingJson = MiniJSON.Json.Deserialize(pendingAction.actionJson) as Dictionary<string, object>;

               switch (pendingAction.actionType) {
                  // For mutes, we get the targetAccId and targetUsrId from the pending action Json. Then, we send the muteInfo to the player, if connected, and show the message
                  case PendingActionType.NotifyMute:
                     PenaltyInfo muteInfo = DB_Main.getPenaltyInfoForAccount(int.Parse(pendingJson["targetAccId"].ToString()), PenaltyType.Mute);

                     // If we found a mute penalty, currently active
                     if (muteInfo != null) {
                        UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                           NetEntity player = EntityManager.self.getEntityWithAccId(muteInfo.targetAccId);
                           if (player != null) {
                              player.Rpc_ReceiveMuteInfo(muteInfo);

                              if (muteInfo.penaltyType != PenaltyType.StealthMute) {
                                 player.Target_ReceiveNormalChat($"You have been muted until {Util.getTimeInEST(muteInfo.penaltyEnd)}", ChatInfo.Type.System);
                              }
                           }
                        });
                     }

                     break;
                  case PendingActionType.NotifyBan:
                     PenaltyInfo banInfo = DB_Main.getPenaltyInfoForAccount(int.Parse(pendingJson["targetAccId"].ToString()), PenaltyType.Ban);

                     // If we found a ban penalty, currently active
                     if (banInfo != null) {
                        UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                           NetEntity player = EntityManager.self.getEntityWithAccId(banInfo.targetAccId);
                           if (player != null) {
                              player.connectionToClient.Send(new ErrorMessage(Global.netId, ErrorMessage.Type.Banned, ServerMessageManager.getBannedMessage(banInfo)));
                              player.connectionToClient.Disconnect();
                           }
                        });
                     }
                     break;
                  case PendingActionType.LiftMute:
                  case PendingActionType.LiftBan:
                     int targetAccId = int.Parse(pendingJson["targetAccId"].ToString());
                     PenaltyType penaltyType = (PenaltyType) int.Parse(pendingJson["penaltyType"].ToString());

                     DB_Main.liftPenaltyForAccount(targetAccId, penaltyType);

                     // If the user is muted and playing, lift the mute penalty (normal or stealth)
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        NetEntity player = EntityManager.self.getEntityWithAccId(targetAccId);

                        if (player != null && penaltyType == PenaltyType.Mute) {
                           player.Target_LiftMute();
                        }
                     });

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
