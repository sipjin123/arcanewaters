using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Enemy_Spawner : MonoBehaviour {
   #region Public Variables

   // The Type of Enemy to spawn
   public Enemy.Type enemyType;

   #endregion

   public void setExtraInfo (string extraInfo) {
      // If a specific type of Enemy was specified, then set the type to that
      if (!Util.isEmpty(extraInfo)) {
         this.enemyType = (Enemy.Type) System.Enum.Parse(typeof(Enemy.Type), extraInfo, true);
      }
   }

   public void setRandomSpawner (RandomEnemySpawner randomSpawner) {
      _randomSpawner = randomSpawner;
   }

   public Enemy.Type getEnemyType (Biome.Type biome) {
      if (_randomSpawner != null) {
         return _randomSpawner.getRandomEnemyType(biome);
      }
      return enemyType;
   }

   public int getEnemyCount (Biome.Type biome) {
      if (_randomSpawner != null) {
         return _randomSpawner.getEnemyBiomeCount(biome);
      }
      return 0;
   }

   public float getRespawnTimer () {
      if (_randomSpawner != null) {
         return _randomSpawner.respawnTimer;
      }
      return -1;
   }

   #region Private Variables

   // The attached random enemy spawner
   private RandomEnemySpawner _randomSpawner = null;

   #endregion
}
