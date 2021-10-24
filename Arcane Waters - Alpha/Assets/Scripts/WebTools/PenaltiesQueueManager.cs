using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
         List<QueueItem> queue = DB_Main.getPenaltiesQueue();

         // If we have items in the penalties queue
         if (queue.Count > 0) {
            foreach (QueueItem item in queue) {
               PenaltyInfo penaltyContent = JsonConvert.DeserializeObject<PenaltyInfo>(item.jsonContent);
               bool success = false;

               List<PenaltyInfo> penalties = DB_Main.getPenaltiesForAccount(penaltyContent.targetAccId);

               // Adding penalty time, only if it's a ban or non-permanent ban
               if (penaltyContent.penaltyType == PenaltyInfo.ActionType.Mute || penaltyContent.penaltyType == PenaltyInfo.ActionType.StealthMute || penaltyContent.penaltyType == PenaltyInfo.ActionType.Ban) {
                  penaltyContent.expiresAt = DateTime.UtcNow.AddSeconds(penaltyContent.penaltyTime).Ticks;
               }

               switch (penaltyContent.penaltyType) {
                  case PenaltyInfo.ActionType.Mute:
                  case PenaltyInfo.ActionType.StealthMute:
                     if (penalties.Any(x => x.penaltyType == PenaltyInfo.ActionType.Mute || x.penaltyType == PenaltyInfo.ActionType.StealthMute)) {
                        D.log(string.Format("Penalty Queue: {0} is already muted.", penaltyContent.targetUsrName));
                     } else {
                        success = DB_Main.muteAccount(penaltyContent);
                        if (success) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.mutePlayer(penaltyContent.targetUsrId, penaltyContent.penaltyType == PenaltyInfo.ActionType.StealthMute, penaltyContent.expiresAt);
                           });
                        } else {
                           D.log("Penalty Queue: Error while trying to mute or stealth mute an account.");
                        }
                     }
                     break;
                  case PenaltyInfo.ActionType.Ban:
                  case PenaltyInfo.ActionType.PermanentBan:
                     if (penalties.Any(x => x.penaltyType == PenaltyInfo.ActionType.Ban || x.penaltyType == PenaltyInfo.ActionType.PermanentBan)) {
                        D.log(string.Format("Penalty Queue: {0} is already banned.", penaltyContent.targetUsrName));
                     } else {
                        success = DB_Main.banAccount(penaltyContent);
                        if (success) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.banPlayer(penaltyContent.targetUsrId, penaltyContent.penaltyType == PenaltyInfo.ActionType.PermanentBan, penaltyContent.expiresAt);
                           });
                        } else {
                           D.log("Penalty Queue: Error while trying to ban an account.");
                        }
                     }
                     break;
                  case PenaltyInfo.ActionType.Kick:
                     // We store this penalty for history purposes
                     DB_Main.kickAccount(penaltyContent);
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        ServerNetworkingManager.self.kickPlayer(penaltyContent.targetUsrId);
                     });
                     break;
                  case PenaltyInfo.ActionType.ForceSinglePlayer:
                  case PenaltyInfo.ActionType.LiftForceSinglePlayer:
                     success = DB_Main.forceSinglePlayerForAccount(penaltyContent);
                     if (success) {
                        if (penaltyContent.penaltyType == PenaltyInfo.ActionType.ForceSinglePlayer) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.forceSinglePlayerModeForUser(penaltyContent.targetUsrId);
                           });
                        }
                     } else {
                        D.log("Penalty Queue: Error while trying to force an account to Single Player mode.");
                     }

                     DB_Main.processPenaltyFromQueue(item.id);
                     break;
                  case PenaltyInfo.ActionType.LiftMute:
                     if (penalties.Any(x => x.penaltyType == PenaltyInfo.ActionType.Mute || x.penaltyType == PenaltyInfo.ActionType.StealthMute)) {
                        penaltyContent.id = penalties.First(x => x.penaltyType == PenaltyInfo.ActionType.Mute || x.penaltyType == PenaltyInfo.ActionType.StealthMute).id;
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
                  case PenaltyInfo.ActionType.LiftBan:
                     if (penalties.Any(x => x.penaltyType == PenaltyInfo.ActionType.Ban || x.penaltyType == PenaltyInfo.ActionType.PermanentBan)) {
                        penaltyContent.id = penalties.First(x => x.penaltyType == PenaltyInfo.ActionType.Ban || x.penaltyType == PenaltyInfo.ActionType.PermanentBan).id;
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

               DB_Main.processPenaltyFromQueue(item.id);
            }
         }
      });
   }

   #region Private Variables

   #endregion
}
