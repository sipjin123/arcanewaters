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
      if (oreType == OreNode.Type.None) {
         D.debug("Ore type was set to None in area {" + instance.areaKey + "}, setting to default type: Iron");
         oreType = OreNode.Type.Iron;
      }
      
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
      registerOreNode(oreNode.id, oreNode);

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(oreNode);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(oreNode.gameObject);

      return oreNode;
   }

   public void registerOreNode (int id, OreNode node) {
      if (!_oreNodesRaw.ContainsKey(id)) {
         _oreNodesRaw.Add(id, node);
         _oreNodeEditorList.Add(node);
      } else {
         if (_oreNodesRaw[id] == null) {
            D.adminLog("Ore node {" + id + "} is null! replacing new ore node", D.ADMIN_LOG_TYPE.Mine);
            _oreNodesRaw[id] = node;
         } else {
            D.adminLog("An ore node is already existing for id {" + id + "}", D.ADMIN_LOG_TYPE.Mine);
         }
      }
   }

   public OreNode getOreNode (int id) {
      if (!_oreNodesRaw.ContainsKey(id)) {
         D.adminLog("No ore node dictionary for area:{" + id + "}", D.ADMIN_LOG_TYPE.Mine);
         return null;
      }
      if (_oreNodesRaw[id] == null) {
         D.adminLog("Missing ore node:{" + id + "}", D.ADMIN_LOG_TYPE.Mine);
         return null;
      }

      return _oreNodesRaw[id];
   }

   #region Private Variables

   // This is to review the ore nodes registered to this manager on the unity editor side
   [SerializeField]
   protected List<OreNode> _oreNodeEditorList = new List<OreNode>();

   // Stores the ore nodes we've created, indexed by their unique ID
   protected Dictionary<int, OreNode> _oreNodesRaw = new Dictionary<int, OreNode>();

   // An ID we use to uniquely identify ore nodes
   protected static int _id = 1;

   #endregion
}
