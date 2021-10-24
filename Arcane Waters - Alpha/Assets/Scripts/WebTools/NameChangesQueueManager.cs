using System.Collections.Generic;
using Newtonsoft.Json;

public class NameChangesQueueManager : GenericGameManager
{
   #region Public Variables

   #endregion

   [ServerOnly]
   protected override void Awake () {
      base.Awake();

      // The game will check for new username changes requests, every 10 seconds, only if this is a Cloud Build.
      if (Util.isCloudBuild()) {
         InvokeRepeating(nameof(checkForUserNamesChangesQueue), 0, 10);
      }
   }

   [ServerOnly]
   private void checkForUserNamesChangesQueue () {
      // Only the master server checks for this queue
      NetworkedServer server = ServerNetworkingManager.self.server;
      if (server == null || !server.isMasterServer()) {
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<QueueItem> queue = DB_Main.getUserNamesChangesQueue();

         // If we have items in the queue
         if (queue.Count > 0) {
            foreach (QueueItem item in queue) {
               NameChangeInfo content = JsonConvert.DeserializeObject<NameChangeInfo>(item.jsonContent);
               try {
                  DB_Main.changeUserName(content);
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerNetworkingManager.self.changeUserName(content.targetUsrId, content.newUsrName);
                  });
               } catch {
                  D.log($"User Names Changes Queue: Error while trying to process a request, {item.id}");
               }
            }
         }
      });
   }

   #region Private Variables

   #endregion
}
