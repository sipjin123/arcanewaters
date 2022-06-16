using System.Collections.Generic;
using System.Linq;

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
         // Fetch the penalties added to the queue
         List<PenaltiesQueueItem> queue = DB_Main.getPenaltiesQueue();

         // If we have items in the penalties queue
         if (queue.Count > 0) {
            foreach (PenaltiesQueueItem item in queue) {
               List<PenaltyInfo> penalties = DB_Main.getPenaltiesForAccount(item.targetAccId);

               bool isMuted = penalties.Any(x => x.IsMute());
               bool isBanned = penalties.Any(x => x.IsBan());

               PenaltyInfo newPenalty = new PenaltyInfo(item.sourceAccId, item.targetAccId, item.penaltyType, item.penaltyReason, item.penaltyTime);

               switch (item.penaltyType) {
                  case PenaltyInfo.ActionType.Mute:
                  case PenaltyInfo.ActionType.StealthMute:
                     if (isMuted) {
                        D.log(string.Format("Penalties Queue: Account #{0} is already muted.", item.targetAccId));
                     } else {
                        if (DB_Main.savePenalty(newPenalty)) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.applyPenaltyToPlayer(item.targetAccId, item.penaltyType, item.penaltyTime);
                              D.log(string.Format("Muting Account #{0}, for {1} seconds.", item.targetAccId, item.penaltyTime));
                           });
                        } else {
                           D.log(string.Format("Penalties Queue: Error while trying to mute or stealth mute Account #{0}.", item.targetAccId));
                        }
                     }
                     break;
                  case PenaltyInfo.ActionType.LiftMute:
                     if (isMuted) {
                        newPenalty.id = penalties.First(x => x.penaltyType == PenaltyInfo.ActionType.Mute || x.penaltyType == PenaltyInfo.ActionType.StealthMute).id;

                        if (DB_Main.savePenalty(newPenalty)) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.applyPenaltyToPlayer(item.targetAccId, item.penaltyType, item.penaltyTime);
                              D.log(string.Format("Unmuting Account #{0}.", item.targetAccId));
                           });
                        } else {
                           D.log(string.Format("Penalty Queue: Error while trying to unmute Account {0}.", item.targetAccId));
                        }
                     } else {
                        D.log(string.Format("Penalty Queue: Account #{0} is not currently muted.", item.targetAccId));
                     }
                     break;
                  case PenaltyInfo.ActionType.SoloBan:
                  case PenaltyInfo.ActionType.SoloPermanentBan:
                     if (isBanned) {
                        D.log(string.Format("Penalty Queue: Account #{0} is already banned.", item.targetAccId));
                     } else {
                        if (DB_Main.savePenalty(newPenalty)) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.applyPenaltyToPlayer(item.targetAccId, item.penaltyType, item.penaltyTime);
                           });
                        } else {
                           D.log(string.Format("Penalty Queue: Error while trying to ban Account #{0}.", item.targetAccId));
                        }
                     }
                     break;
                  case PenaltyInfo.ActionType.LiftBan:
                     if (isBanned) {
                        newPenalty.id = penalties.First(x => x.penaltyType == PenaltyInfo.ActionType.SoloBan || x.penaltyType == PenaltyInfo.ActionType.SoloPermanentBan).id;

                        if (DB_Main.savePenalty(newPenalty)) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              D.log(string.Format("Unbanning Account #{0}.", item.targetAccId));
                           });
                        } else {
                           D.log(string.Format("Penalty Queue: Error while trying to unban Account {0}.", item.targetAccId));
                        }
                     } else {
                        D.log(string.Format("Penalty Queue: Account #{0} is not currently banned.", item.targetAccId));
                     }
                     break;
                  case PenaltyInfo.ActionType.Kick:
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        ServerNetworkingManager.self.applyPenaltyToPlayer(item.targetAccId, item.penaltyType, item.penaltyTime);
                     });
                     DB_Main.savePenalty(newPenalty);
                     break;
                  case PenaltyInfo.ActionType.ForceSinglePlayer:
                  case PenaltyInfo.ActionType.LiftForceSinglePlayer:
                     if (DB_Main.savePenalty(newPenalty)) {
                        if (item.penaltyType == PenaltyInfo.ActionType.ForceSinglePlayer) {
                           UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                              ServerNetworkingManager.self.applyPenaltyToPlayer(item.targetAccId, item.penaltyType, item.penaltyTime);
                           });
                        }
                     } else {
                        D.log(string.Format("Penalty Queue: Error while trying to force Account #{0} to Single Player mode.", item.targetAccId));
                     }
                     break;
               }

               DB_Main.removePenaltiesQueueItem(item.id);
            }
         }
      });
   }

   #region Private Variables

   #endregion
}
