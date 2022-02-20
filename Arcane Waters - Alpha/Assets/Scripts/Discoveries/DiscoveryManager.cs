using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DiscoveryManager : MonoBehaviour
{
   #region Public Variables

   // The prefab for Discoveries
   public Discovery discoveryPrefab;

   // Self
   public static DiscoveryManager self;

   // Which discoveries has this client revealed (client-only)
   public HashSet<int> revealedDiscoveriesClient = new HashSet<int>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void createDiscoveriesForInstance (Instance instance) {
      // Look up the Area associated with this intance
      Area area = AreaManager.self.getArea(instance.areaKey);

      // Find all of the possible discoveries in this Area
      foreach (DiscoverySpot spot in area.GetComponentsInChildren<DiscoverySpot>()) {
         createDiscoveryOnServer(instance, spot);
      }
   }

   [Server]
   public void fetchDiscoveriesOnServer () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<DiscoveryData> dataList = DB_Main.getDiscoveriesList();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _discoveryDatas = new Dictionary<int, DiscoveryData>();

            foreach (DiscoveryData data in dataList) {
               if (_discoveryDatas.ContainsKey(data.discoveryId)) {
                  D.editorLog("Duplicated discovery id=" + data.discoveryId);
                  continue;
               }

               _discoveryDatas.Add(data.discoveryId, data);
            }
         });
      });
   }

   [Server]
   public void userEntersInstance (NetEntity player) {
      // Fetch all discoveries by this user
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<UserDiscovery> discoveries = DB_Main.getUserDiscoveries(player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Notify the user which discoveries he has uncovered
            player.rpc.Target_ReceiveFoundDiscoveryList(discoveries);
         });
      });
   }

   private Discovery createDiscoveryOnServer (Instance instance, DiscoverySpot spot) {
      int discoveryId = spot.targetDiscoveryID;

      if (!_discoveryDatas.ContainsKey(discoveryId)) {
         D.error("Discovery spot contains invalid discovery ID=" + discoveryId);
         return null;
      }

      // Instantiate a new Discovery, keep it parented to its spot
      Discovery discovery = Instantiate(discoveryPrefab, spot.transform.position, Quaternion.identity, spot.transform);

      // Initialize the discovery
      discovery.assignDiscoveryAndPosition(_discoveryDatas[discoveryId], spot.transform.position);

      // The Instance needs to keep track of all Networked objects inside
      InstanceManager.self.addDiscoveryToInstance(discovery, instance);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(discovery.gameObject);

      // Keep track of the discoveries that we've created
      _discoveries.Add(discovery);

      return discovery;
   }

   [Server]
   public bool isDiscoveryFindingValid (int discoveryId, NetEntity byPlayer, out Discovery discovery) {
      foreach (Discovery d in _discoveries) {
         if (d != null && d.data.discoveryId == discoveryId && getDistanceFromDiscovery(d, byPlayer) <= Discovery.MAX_VALID_DISTANCE && d.instanceId == byPlayer.instanceId) {
            discovery = d;
            return true;
         }
      }

      discovery = null;
      return false;
   }

   private float getDistanceFromDiscovery (Discovery discovery, NetEntity from) {
      return Vector2.Distance(discovery.transform.position, from.transform.position);
   }

   [Server]
   public void onDiscoveryDestroyed (Discovery d) {
      for (int i = 0; i < _discoveries.Count; i++) {
         if (_discoveries[i] == null || _discoveries[i] == d) {
            _discoveries.RemoveAt(i);
            i--;
         }
      }
   }

   #region Private Variables

   // Stores the spawned discoveries, index by instance and map editor assigned ID
   private List<Discovery> _discoveries = new List<Discovery>();

   // The cached discoveries in the database
   private Dictionary<int, DiscoveryData> _discoveryDatas = new Dictionary<int, DiscoveryData>();

   #endregion
}
