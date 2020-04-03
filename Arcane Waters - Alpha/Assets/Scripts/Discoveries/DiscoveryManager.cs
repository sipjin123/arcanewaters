using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DiscoveryManager : MonoBehaviour {

   #region Public Variables

   // The prefab for Discoveries
   public DiscoverySpot discoveryPrefab;

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
         if (Random.value <= spot.spawnChance) {
            DiscoverySpot discovery = createDiscoveryOnServer(instance, spot);

            // Keep track of the discoveries that we've created
            _discoveries.Add(discovery.id, discovery);
         }
      }
   }

   private DiscoverySpot createDiscoveryOnServer (Instance instance, DiscoverySpot spot) {
      // Instantiate a new Discovery
      DiscoverySpot discovery = Instantiate(discoveryPrefab, spot.transform.position, Quaternion.identity);

      // Keep it parented to this Manager
      discovery.transform.SetParent(this.transform, true);

      // Assign a unique ID
      discovery.id = _lastUsedId++;

      // Note which instance the discovery is in
      discovery.instanceId = instance.id;

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(discovery);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(discovery.gameObject);

      return discovery;
   }

   #region Private Variables

   // Stores the spawned discoveries
   private Dictionary<int, DiscoverySpot> _discoveries = new Dictionary<int, DiscoverySpot>();

   // The last unique ID we used
   private int _lastUsedId = 1;

   #endregion
}
