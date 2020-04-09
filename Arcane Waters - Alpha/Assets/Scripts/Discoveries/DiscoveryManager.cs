using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DiscoveryManager : MonoBehaviour {

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
      // Look up the Area associated with this intance
      Area area = AreaManager.self.getArea(instance.areaKey);

      // Find all of the possible discoveries in this Area
      foreach (DiscoverySpot spot in area.GetComponentsInChildren<DiscoverySpot>()) {
         // Have a random chance of spawning a discovery
         if (Random.value < spot.spawnChance) {
            Discovery discovery = createDiscoveryOnServer(instance, spot);

            // Keep track of the discoveries that we've created
            _discoveries.Add(discovery.id, discovery);
         }
      }
   }

   private Discovery createDiscoveryOnServer (Instance instance, DiscoverySpot spot) {
      // Instantiate a new Discovery
      Discovery discovery = Instantiate(discoveryPrefab, spot.transform.position, Quaternion.identity);

      // Keep it parented to this Manager
      discovery.transform.SetParent(this.transform, true);

      // Assign a unique ID
      discovery.id = _lastUsedId++;

      // Note which instance the discovery is in
      discovery.instanceId = instance.id;

      int randomIdIndex = Random.Range(0, spot.possibleDiscoveryIds.Count);

      discovery.assignDiscoveryId(spot.possibleDiscoveryIds[randomIdIndex]);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(discovery);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(discovery.gameObject);

      return discovery;
   }

   #region Private Variables

   // Stores the spawned discoveries
   private Dictionary<int, Discovery> _discoveries = new Dictionary<int, Discovery>();

   // The last unique ID we used
   private int _lastUsedId = 1;

   #endregion
}
