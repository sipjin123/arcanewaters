using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PenaltiesQueueManager : GenericGameManager
{
   #region Public Variables

   #endregion

   [ServerOnly]
   protected override void Awake () {
      base.Awake();

      // The game will check for new items in the penalties queue, every 10 seconds, only if this is a Cloud Build
      if (Util.isCloudBuild()) {
         InvokeRepeating(nameof(checkForPenaltiesQueue), 0, 10);
      }
   }

   [ServerOnly]
   private void checkForPenaltiesQueue () {
      // Only the master server checks for the penalties queue
      NetworkedServer server = ServerNetworkingManager.self.server;
      if (server == null || !server.isMasterServer()) {
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<PenaltyQueueItem> queue = DB_Main.getPenaltiesQueue();

         // If we have items in the penalties queue
         if (queue.Count > 0) {
            foreach (PenaltyQueueItem item in queue) {
               PenaltyInfo penaltyContent = MiniJSON.Json.Deserialize(item.jsonContent) as PenaltyInfo;
               bool success = false;

               List<PenaltyInfo> penalties = DB_Main.getPenaltiesForAccount(penaltyContent.targetAccId);

               switch (penaltyContent.penaltyType) {
                  case PenaltyActionType.Mute:
                  case PenaltyActionType.StealthMute:
                     if (penalties.Any(x => x.penaltyType == PenaltyActionType.Mute || x.penaltyType == PenaltyActionType.StealthMute)) {
                        D.log(string.Format("Penalty Queue: {0} is already muted.", penaltyContent.targetUsrName));
                     } else {
                        success = DB_Main.muteAccount(penaltyContent);
                        if (success) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.mutePlayer(penaltyContent.targetUsrId, penaltyContent.penaltyType == PenaltyActionType.StealthMute, penaltyContent.expiresAt);
                           });
                        } else {
                           D.log("Penalty Queue: Error while trying to mute or stealth mute an account.");
                        }
                     }
                     break;
                  case PenaltyActionType.Ban:
                  case PenaltyActionType.PermanentBan:
                     if (penalties.Any(x => x.penaltyType == PenaltyActionType.Ban || x.penaltyType == PenaltyActionType.PermanentBan)) {
                        D.log(string.Format("Penalty Queue: {0} is already banned.", penaltyContent.targetUsrName));
                     } else {
                        success = DB_Main.banAccount(penaltyContent);
                        if (success) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.banPlayer(penaltyContent.targetUsrId, penaltyContent.penaltyType == PenaltyActionType.PermanentBan, penaltyContent.expiresAt);
                           });
                        } else {
                           D.log("Penalty Queue: Error while trying to ban an account.");
                        }
                     }
                     break;
                  case PenaltyActionType.Kick:
                     // We store this penalty for history purposes
                     DB_Main.kickAccount(penaltyContent);
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        ServerNetworkingManager.self.kickPlayer(penaltyContent.targetUsrId);
                     });
                     break;
                  case PenaltyActionType.ForceSinglePlayer:
                     success = DB_Main.forceSinglePlayerForAccount(penaltyContent);
                     if (success) {
                        UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                           ServerNetworkingManager.self.forceSinglePlayerModeForUser(penaltyContent.targetUsrId);
                        });
                     } else {
                        D.log("Penalty Queue: Error while trying to force an account to Single Player mode.");
                     }

                     DB_Main.processPenaltyFromQueue(item.id);
                     break;
                  case PenaltyActionType.LiftMute:
                     if (penalties.Any(x => x.penaltyType == PenaltyActionType.Mute || x.penaltyType == PenaltyActionType.StealthMute)) {
                        success = DB_Main.unMuteAccount(penaltyContent);
                        if (success) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.unMutePlayer(penaltyContent.targetUsrId);
                           });
                        } else {
                           D.log("Penalty Queue: Error while trying to unmute an account.");
                        }
                     } else {
                        D.log(string.Format("Penalty Queue: {0} is not currently muted.", penaltyContent.targetUsrName));
                     }
                     break;
                  case PenaltyActionType.LiftBan:
                     if (penalties.Any(x => x.penaltyType == PenaltyActionType.Ban || x.penaltyType == PenaltyActionType.PermanentBan)) {
                        success = DB_Main.unBanAccount(penaltyContent);

                        // We don't need to send anything to the servers, since the banned status is checked when the player logs in.
                        if (!success) {
                           D.log("Penalty Queue: Error while trying to unban an account.");
                        }
                     } else {
                        D.log(string.Format("Penalty Queue: {0} is not currently banned.", penaltyContent.targetUsrName));
                     }
                     break;
               }
            }
         }
      });
   }

   #region Private Variables

   #endregion
}
