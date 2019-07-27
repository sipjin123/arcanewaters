using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreArea : MonoBehaviour {
   #region Public Variables

   // List of posible spawn positions
   public List<Transform> spawnPointList;

   // List of the ores in the location
   public List<GameObject> oreList;

   #endregion

   public List<Vector2> getPotentialSpawnPoints(int number) {
      // Ensures the ores to be spawned is less than the spawn points
      number = Mathf.Clamp(number, 0, spawnPointList.Count);
      List<Vector2> spawnPointHolder = new List<Vector2>();

      // Copies the location of each spawn point in the list
      for(int i = 0; i < spawnPointList.Count;i++) {
         spawnPointHolder.Add(spawnPointList[i].localPosition);
      }

      List<Vector2> returnSpawnList = new List<Vector2>();

      // Seeks random spawn point until number requirement is met
      while (returnSpawnList.Count < number) {
         int randomIndex = Random.Range(0, spawnPointHolder.Count);
         Vector2 coordToAdd = spawnPointHolder[randomIndex];

         returnSpawnList.Add(coordToAdd);
         spawnPointHolder.Remove(coordToAdd);
      }

      return returnSpawnList;
   }
}
