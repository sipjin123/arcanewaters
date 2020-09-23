using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;
using System.Diagnostics;

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

            string machineID = string.Empty;

#if UNITY_STANDALONE_WIN
            machineID = System.Environment.MachineName;
#endif
            //D.debug("This machine is identified as: " + machineID);
            
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

               var proc = Process.GetCurrentProcess();
               var procName = proc.ProcessName;
               var procID = proc.Id.ToString();

               // Server.players_count
               var server = ServerNetwork.self.server;
               //D.debug($"Metrics Manager: server={machineID} - users={server.connectedUserIds.Count}");
               DB_Main.setMetricPlayersCount(machineID, procName, procID, server.connectedUserIds.Count);

               // Server.area_instances
               var areaInstances = InstanceManager.self.getAreas();
               //D.debug($"Metrics Manager: server={machineID} - areaInstances={areaInstances.Length}");
               DB_Main.setMetricAreaInstancesCount(machineID, procName, procID, areaInstances.Length);

               // Server.port
               var port = MyNetworkManager.self.telepathy.port;
               //D.debug($"Metrics Manager: server={machineID} - areaInstances={areaInstances.Length}");
               DB_Main.setMetricPort(machineID, procName, procID, port);

               // Server.ip
               var ip = MyNetworkManager.self.networkAddress;
               //D.debug($"Metrics Manager: server={machineID} - areaInstances={areaInstances.Length}");
               DB_Main.setMetricIP(machineID, procName, procID, ip);
               
               //D.debug($"Metrics Manager: address={ip} - port={port}");
            });
         } catch (Exception ex) {
            D.warning("MetricsManager: Couldn't update metrics. | " + ex.Message);
         }
      }
   }
}
