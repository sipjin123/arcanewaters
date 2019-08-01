using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreArea : MonoBehaviour {
   #region Public Variables

   // The parent of the spawned ore objects
   public Transform oreObjHolder;

   // List of posible spawn positions
   public List<Transform> spawnPointList;

   // List of the ores in the location
   public List<OreObj> oreList;

   // List of data of ores
   public List<OreData> oreDataList;

   // Cache coordinates and spawn index
   public class SpawnPointIndex
   {
      public Vector2 coordinates;
      public int index;
   }

   // Determines if the server needs to initialize the spawning
   public bool hasInitialized;

   #endregion

   public void initOreArea() {
      Area.Type currAreaType = GetComponent<Area>().areaType;
      for (int i = 0; i < oreList.Count; i++) {
         OreObj oreObj = oreList[i];

         // Setup ore id
         string idString = ((int)currAreaType).ToString() + "" + i;
         int newID = int.Parse(idString);

         // Determines the type of ore to be spawned
         OreType oreType = OreType.None;
         int silverOreCap = 6;
         int oreTypeRandomizer = Random.Range(1, 10);
         if(oreTypeRandomizer < silverOreCap) {
            oreType = OreType.Silver;
         }
         else {
            oreType = OreType.Gold;
         }

         // Find the type of data
         OreData oreData = oreDataList.Find(_ => _.oreType == oreType);

         // Setup data for individual ore
         oreObj.setOreData(newID, currAreaType, oreData);
      }
   }

   public List<SpawnPointIndex> getPotentialSpawnPoints(int number) {
      // Ensures the ores to be spawned is less than the spawn points
      number = Mathf.Clamp(number, 0, spawnPointList.Count);
      List<Vector3> spawnPointHolder = new List<Vector3>();

      // Copies the location of each spawn point in the list
      for(int i = 0; i < spawnPointList.Count;i++) {
         spawnPointHolder.Add(spawnPointList[i].localPosition);
      }

      List<SpawnPointIndex> returnSpawnList = new List<SpawnPointIndex>();

      // Seeks random spawn point until number requirement is met
      while (returnSpawnList.Count < number) {
         int randomIndex = Random.Range(0, spawnPointHolder.Count);
         Vector2 coordToAdd = spawnPointHolder[randomIndex];
         Transform locatedIndex = spawnPointList.Find(_ => _.localPosition == spawnPointHolder[randomIndex]);

         returnSpawnList.Add(new SpawnPointIndex { coordinates = coordToAdd, index = spawnPointList.IndexOf(locatedIndex) });
         spawnPointHolder.RemoveAt(randomIndex);
      }

      return returnSpawnList;
   }
}