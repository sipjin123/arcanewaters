using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;

public class MapManager : MonoBehaviour {
   #region Public Variables

   // Whether or not we want to spawn the live maps from the database
   public static bool isSpawningMaps = false;

   // Convenient self reference
   public static MapManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void spawnLiveMaps () {
      if (!isSpawningMaps) {
         return;
      }

      // For now we'll just spawn one for testing purposes
      string areaKey = "Pineward";
      MapData mapData = DB_Main.getLiveMapData(areaKey);
      MapImporter.instantiateMapData(mapData.serializedData, areaKey, new Vector3(500f, 500f));
   }

   #region Private Variables

   // Keeps track of the Maps we've created, indexed by their Area Key string
   protected Dictionary<string, GameObject> _maps = new Dictionary<string, GameObject>();

   #endregion
}
