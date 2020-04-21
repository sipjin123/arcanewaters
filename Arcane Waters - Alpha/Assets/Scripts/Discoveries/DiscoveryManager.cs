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

   #endregion

   private void Awake () {
      self = this;
   }

   public void createDiscoveriesForInstance (Instance instance) {
      if (_discoveryDatas == null || _discoveryDatas.Count == 0) {
         fetchAndCreateDiscoveriesForInstance(instance);
         return;
      }

      // Look up the Area associated with this intance
      Area area = AreaManager.self.getArea(instance.areaKey);

      // Find all of the possible discoveries in this Area
      foreach (DiscoverySpot spot in area.GetComponentsInChildren<DiscoverySpot>()) {
         // Have a random chance of spawning a discovery only if the list of possible discoveries isn't empty
         if (spot.possibleDiscoveryIds.Count > 0 && Random.value <= spot.spawnChance) {
            Discovery discovery = createDiscoveryOnServer(instance, spot);

            if (discovery != null) {
               // Keep track of the discoveries that we've created
               _discoveries.Add(discovery.id, discovery);
            }
         }
      }
   }

   private void fetchAndCreateDiscoveriesForInstance (Instance instance) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<DiscoveryData> dataList = DB_Main.getDiscoveriesList();
         _discoveryDatas = new Dictionary<int, DiscoveryData>();

         foreach (DiscoveryData data in dataList) {
            if (_discoveryDatas.ContainsKey(data.discoveryId)) {
               D.log("Duplicated discovery id=" + data.discoveryId);
            }

            _discoveryDatas.Add(data.discoveryId, data);
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            createDiscoveriesForInstance(instance);
         });
      });
   }

   private Discovery createDiscoveryOnServer (Instance instance, DiscoverySpot spot) {
      // Get a random discovery from the list of valid discoveries
      int randomIdIndex = Random.Range(0, spot.possibleDiscoveryIds.Count);
      int discoveryId = spot.possibleDiscoveryIds[randomIdIndex];

      if (!_discoveryDatas.ContainsKey(discoveryId)) {
         D.error("Discovery spot contains invalid discovery ID=" + discoveryId);
         return null;
      }

      // Instantiate a new Discovery
      Discovery discovery = Instantiate(discoveryPrefab);

      // Keep it parented to its spot
      discovery.transform.SetParent(spot.transform);
      discovery.transform.position = spot.transform.position;

      // Assign a unique ID
      discovery.id = _lastUsedId++;

      // Note which instance the discovery is in
      discovery.instanceId = instance.id;

      // Initialize the discovery
      discovery.assignDiscoveryAndPosition(_discoveryDatas[discoveryId], spot.transform.position);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(discovery);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(discovery.gameObject);

      return discovery;
   }

   public Discovery getSpawnedDiscoveryById (int id) {
      return _discoveries[id];
   }

   #region Private Variables

   // Stores the spawned discoveries
   private Dictionary<int, Discovery> _discoveries = new Dictionary<int, Discovery>();

   // The last unique ID we used
   private int _lastUsedId = 1;

   // The cached discoveries in the database
   private Dictionary<int, DiscoveryData> _discoveryDatas = new Dictionary<int, DiscoveryData>();

   #endregion
}
