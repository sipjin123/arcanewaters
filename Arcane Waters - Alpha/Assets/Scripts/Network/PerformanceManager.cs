using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;
using System.Diagnostics;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Network
{
   public class PerformanceManager : GenericGameManager
   {
      #region Public Variables

      // The Game Objects to take out from the scene.
      public GameObject[] Removables;

      // Should Statics be removed as well?
      public bool DestroyStatics = false;

      #endregion

      protected override void Awake () {
         base.Awake();
         startTime = DateTime.UtcNow;
         commandLineArgs = System.Environment.GetCommandLineArgs();
         D.adminLog(GetType().ToString() + " Performance Management Enabled: " + isPerformanceManagementEnabled(), D.ADMIN_LOG_TYPE.Initialization);
         D.adminLog(GetType().ToString() + " Metrics Management Enabled: " + isMetricsManagementEnabled(), D.ADMIN_LOG_TYPE.Initialization);
         InvokeRepeating("execute", 0.0f, UpdateFrequencySecs);
      }

      private void execute () {

         // We only do this check on the server
         if (!NetworkServer.active) {
            return;
         }

         // Wait until the server is initialized
         var server = ServerNetworkingManager.self.server;
         if (server == null) {
            return;
         }

         if (isPerformanceManagementEnabled()) {
            removeUnusedAssets();
         }

         if (isMetricsManagementEnabled()) {
            try {
               string machineID = string.Empty;

               #if UNITY_STANDALONE_WIN
               machineID = System.Environment.MachineName;
               #endif

               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

                  var proc = Process.GetCurrentProcess();
                  var procName = proc.ProcessName;
                  var procID = proc.Id.ToString();

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

                  // Server.uptime (Ticks)
                  long uptime = DateTime.UtcNow.Ticks - startTime.Ticks;
                  DB_Main.setMetricUptime(machineID, procName, procID, uptime);

                  //D.debug($"Metrics Manager: address={ip} - port={port}");
               });

            } catch (Exception ex) {
               D.warning("MetricsManager: Couldn't update metrics. | " + ex.Message);
            }
         }

      }

      private bool isMetricsManagementEnabled () {
         return commandLineArgs.Contains("METRICS");
      }

      private bool isPerformanceManagementEnabled () {
         return commandLineArgs.Contains("LEAN");
      }

      private bool deactivateRenderers (IEnumerable<Renderer> renderers) {
         foreach (Renderer r in renderers) {
            if (r != null) {
               r.material.mainTexture = null;
               r.enabled = false;
            }
         }
         return true;
      }

      private bool deactivateSpriteRenderers (IEnumerable<SpriteRenderer> renderers) {
         foreach (SpriteRenderer r in renderers) {
            if (r != null) {
               r.sprite = null;
               r.enabled = false;
            }
         }
         return true;
      }

      private bool deactivateAudioSources (AudioSource[] audioSources) {
         foreach (AudioSource audioSource in audioSources) {
            if (audioSource != null) {
               audioSource.clip = null;
               audioSource.enabled = false;
            }
         }
         return true;
      }

      private bool deactivateImages (UnityEngine.UI.Image[] images) {
         foreach (UnityEngine.UI.Image i in images) {
            if (i != null) {
               i.sprite = null;
               i.enabled = false;
            }
         }
         return true;
      }

      private bool cleanUpGameObject (GameObject go) {

         deactivateRenderers(go.GetComponentsInChildren<Renderer>(true));

         // deactivate sprite renderers.
         deactivateSpriteRenderers(go.GetComponentsInChildren<SpriteRenderer>(true));

         // deactivate audio clips.
         deactivateAudioSources(go.GetComponentsInChildren<AudioSource>(true));

         // deactivate Images.
         deactivateImages(go.GetComponentsInChildren<UnityEngine.UI.Image>(true));

         return true;
      }

      private void removeUnusedAssets () {

         // Clean Up All Root Items, by removing unused references to assets that the Server doesn't need
         SceneManager.GetActiveScene()
            .GetRootGameObjects()
            .Select(cleanUpGameObject);

         // These items are actually removed from the scene
         foreach (GameObject removable in Removables) {
            if (removable != null) {
               try {
                  Destroy(removable);
               } catch (Exception ex) {
                  D.error($"Exception caught while removing {removable.name} from the scene. Message: {ex.Message}");
               }
            }
         }

         // Some static variables might store references to unnecessary resources
         if (DestroyStatics) {

            // Disables Audio Globally
            Destroy(AudioClipManager.self); AudioClipManager.self = null;
            Destroy(AmbienceManager.self); AmbienceManager.self = null;
            Destroy(SoundEffectManager.self); SoundEffectManager.self = null;
            Destroy(SoundManager.self); SoundManager.self = null;
         }

         // This call acts like a catch-all for those objects that were left behind by the function above.
         // In fact, DontDestroyOnLoad objects are normally bypassed by the above function.
         Transform[] sceneObjects = FindObjectsOfType<Transform>();
         foreach (Transform sceneObj in sceneObjects) {
            cleanUpGameObject(sceneObj.gameObject);
         }

         Resources.UnloadUnusedAssets();

         GC.Collect();
      }

      #region Private Variables

      // Starting time for Metrics purposes
      DateTime startTime = DateTime.UtcNow;

      // Local storage of the command line arguments
      private string[] commandLineArgs;

      // UpdateFrequency
      [SerializeField]
      private float UpdateFrequencySecs = 10.0f;

      #endregion
   }
}
