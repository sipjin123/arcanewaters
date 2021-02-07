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

   // Sprites of each ore
   public Sprite goldSprite, ironSprite, silverSprite;

   #endregion

   public void Awake () {
      self = this;
   }

   public Sprite getSprite (OreNode.Type oreType) {
      switch (oreType) {
         case OreNode.Type.Gold:
            return goldSprite;
         case OreNode.Type.Iron:
            return ironSprite;
         case OreNode.Type.Silver:
            return silverSprite;
      }
      return null;
   }

   public void createOreNodesForInstance (Instance instance) {
      // Look up the Area associated with this intance
      Area area = AreaManager.self.getArea(instance.areaKey);

      // Find all of the possible ore spots in this Area
      foreach (OreSpot spot in area.GetComponentsInChildren<OreSpot>()) {
         // Have a random chance of spawning an ore node there
         if (Random.Range(0f, 1f) <= 1.0f) {
            OreNode ore = createOreNode(instance, spot);
         }
      }
   }

   public OreNode createOreNode (Instance instance, OreSpot spot) {
      return createOreNode(instance, spot.transform.localPosition, spot.possibleOreTypes.ChooseByRandom());
   }

   public OreNode createOreNode (Instance instance, Vector3 localPosition, OreNode.Type oreType) {
      // Instantiate a new Ore Node
      OreNode oreNode = Instantiate(oreNodePrefab);
      oreNode.areaKey = instance.areaKey;

      // Set position
      oreNode.transform.localPosition = localPosition;

      // Assign a unique ID
      oreNode.id = _id++;

      // Pick a type
      oreNode.oreType = oreType;

      // Note which instance the ore node is in
      oreNode.instanceId = instance.id;

      // Assign the voyage id
      oreNode.voyageId = instance.voyageId;

      if (_oreNodes.ContainsKey(oreNode.id)) {
         _oreNodes.Remove(oreNode.id);
      }

      // Keep track of the ore nodes that we've created
      _oreNodes.Add(oreNode.id, oreNode);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(oreNode);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(oreNode.gameObject);

      return oreNode;
   }

   public void registerOreNode (int id, OreNode node) {
      if (!_oreNodes.ContainsKey(id)) {
         _oreNodes.Add(id, node);
      } 
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
