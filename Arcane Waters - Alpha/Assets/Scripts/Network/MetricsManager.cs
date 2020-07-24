using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;

namespace Assets.Scripts.Network
{
   public class MetricsManager : MonoBehaviour
   {
      #region "Public Variables"

      // UpdateFrequency.
      public float UpdateFrequencySecs = 10.0f;

      #endregion

      private void Awake () {

         InvokeRepeating("updateMetrics", 0.0f, UpdateFrequencySecs);
      }

      private void updateMetrics () {
         try {
            // We only do this check on the server
            if (!NetworkServer.active) {
               return;
            }

            string machineIdentifier = string.Empty;

#if UNITY_WINDOWS
            string machineIdentifier = System.Environment.MachineName + "_" + System.Environment.OSVersion;
#endif

            // Update the player count for each (server) and the area instances count.
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               
               var address = MyNetworkManager.self.networkAddress;
               var port = MyNetworkManager.self.telepathy.port;

               foreach (var server in ServerNetwork.self.servers) {
                  DB_Main.setMetricPlayersCount(machineIdentifier,address.ToString(), port.ToString(), server.connectedUserIds.Count);
               }

               // The API seems to expose only Open Areas for now.
               var openAreaInstances = InstanceManager.self.getOpenAreas(); 
               DB_Main.setMetricAreaInstancesCount(machineIdentifier,address.ToString(), port.ToString(),openAreaInstances.Count());
            });
         } catch (Exception ex) {
            D.warning("MetricsManager: Couldn't update metrics. " + ex);
         }
      }
   }
}
