using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OreArea : MonoBehaviour {

   #region Public Variables

   public List<Transform> _spawnPointList;

   public List<GameObject> _oreList;

   #endregion

   public List<Vector2> getPotentialSpawnPoints(int number) {
      number = Mathf.Clamp(number, 0, _spawnPointList.Count);

      List<Vector2> spawnPointHolder = new List<Vector2>();
      for(int i = 0; i < _spawnPointList.Count;i++) {
         spawnPointHolder.Add(_spawnPointList[i].localPosition);
      }

      List<Vector2> returnSpawnList = new List<Vector2>();

      while (returnSpawnList.Count < number) {
         int randomIndex = Random.Range(0, spawnPointHolder.Count);
         Vector2 tempSpawn = spawnPointHolder[randomIndex];

         returnSpawnList.Add(tempSpawn);
         spawnPointHolder.Remove(tempSpawn);
         Debug.LogError("Addint position of : " + tempSpawn);
      }

      return returnSpawnList;
   }
}
