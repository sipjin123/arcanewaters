using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreManager : MonoBehaviour
{
   #region Public Variables

   // Prefab of the network spawn ore
   public OreObj oreObjPrefab;

   // Self
   public static OreManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public void createOreForInstance (Instance instance) {
      // Look up the Area associated with this intance
      Area area = AreaManager.self.getArea(instance.areaType);
      OreArea oreArea = area.GetComponent<OreArea>();
      oreArea.oreList = new List<OreObj>();

      // Creates an ore network instance before initializing
      for (int i = 0; i < oreArea.spawnPointList.Count; i++) { 
         OreObj oreObj = createOreObj(instance, oreArea);
         oreObj.hasParentList = true;
         oreObj.areaID = (int) area.areaType;
         oreArea.oreList.Add(oreObj);
      }
      oreArea.initOreArea();
   }

   protected OreObj createOreObj (Instance instance, OreArea oreArea) {
      // Instantiate a new ore object
      OreObj oreObj = Instantiate(oreObjPrefab, transform.position, Quaternion.identity);

      oreObj.transform.SetParent(oreArea.oreObjHolder);

      // Assign a unique ID
      oreObj.id = _id++;

      // Note which instance the ore is in
      oreObj.instanceId = instance.id;

      oreObj.oreArea = oreArea.GetComponent<Area>().areaType;

      // The Instance needs to keep track of all Networked objects inside
      instance.entities.Add(oreObj);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(oreObj.gameObject);

      return oreObj;
   }
   
   public OreObj getOreObj(int id) {
      return _oreObjects[id];
   }

   public void registerOreObj(int id, OreObj oreObj) {
      _oreObjects.Add(id, oreObj);
   }

   #region Private Variables

   // An ID we use to uniquely identify ore obj
   protected static int _id = 1;


   // Stores the ore obj we've created
   protected Dictionary<int, OreObj> _oreObjects = new Dictionary<int, OreObj>();

   #endregion
}