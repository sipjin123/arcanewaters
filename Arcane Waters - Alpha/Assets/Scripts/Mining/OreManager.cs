using System.Collections.Generic;
using Mirror;
using UnityEngine;

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

      // Keep track of the ore nodes that we've created
      registerOreNode(instance.areaKey, oreNode);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(oreNode);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(oreNode.gameObject);

      return oreNode;
   }

   public void registerOreNode (string areaKey, OreNode node) {
      // Create dictionary if none exists 
      if (!_oreNodes.ContainsKey(areaKey)) {
         _oreNodes.Add(areaKey, new List<OreNode>());
      }

      if (!_oreNodes[areaKey].Contains(node)) {
         _oreNodes[areaKey].Add(node);
      } else {
         D.debug("Ore {" + node.id + "} already existing for area:{" + areaKey + "}");
      }
   }

   public OreNode getOreNode (string areaKey, int id) {
      if (!_oreNodes.ContainsKey(areaKey)) {
         D.debug("No ore node dictionary for area:{" + areaKey + "}");
         return null;
      }

      return _oreNodes[areaKey].Find(_ => _.id == id);
   }

   #region Private Variables

   // Stores the ore nodes we've created, indexed by their unique ID
   protected Dictionary<string, List<OreNode>> _oreNodes = new Dictionary<string, List<OreNode>>();

   // An ID we use to uniquely identify ore nodes
   protected static int _id = 1;

   #endregion
}
