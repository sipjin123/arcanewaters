using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreManager : MonoBehaviour
{
   #region Public Variables

   // The prefab we use for creating Ore Nodes
   public OreNode oreNodePrefab;

   // Self
   public static OreManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void createOreNodesForInstance (Instance instance) {
      // Look up the Area associated with this intance
      Area area = AreaManager.self.getArea(instance.areaType);

      // Find all of the possible ore spots in this Area
      foreach (OreSpot spot in area.GetComponentsInChildren<OreSpot>()) {
         // Have a random chance of spawning an ore node there
         if (Random.Range(0f, 1f) <= 1.0f) {
            OreNode ore = createOreNode(instance, spot);
         }
      }
   }

   protected OreNode createOreNode (Instance instance, OreSpot spot) {
      // Instantiate a new Ore Node
      OreNode oreNode = Instantiate(oreNodePrefab, spot.transform.position, Quaternion.identity);

      // Keep it parented to this Manager
      oreNode.transform.SetParent(this.transform, true);

      // Assign a unique ID
      oreNode.id = _id++;

      // Pick a type
      oreNode.oreType = spot.possibleOreTypes.ChooseByRandom();

      // Note which instance the ore node is in
      oreNode.instanceId = instance.id;

      // Keep track of the ore nodes that we've created
      _oreNodes.Add(oreNode.id, oreNode);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(oreNode);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(oreNode.gameObject);

      return oreNode;
   }

   public OreNode getOreNode (int id) {
      return _oreNodes[id];
   }

   #region Private Variables

   // Stores the ore nodes we've created, indexed by their unique ID
   protected Dictionary<int, OreNode> _oreNodes = new Dictionary<int, OreNode>();

   // An ID we use to uniquely identify ore nodes
   protected static int _id = 1;

   #endregion
}
