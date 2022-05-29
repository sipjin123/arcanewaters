using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class RandomEnemySpawner : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // Enemy types that will be spawned at randow through RandomEnemySpawner class
   [Header("Random enemies")]
   public Enemy.Type[] forestBiomeEnemies;
   public Enemy.Type[] desertBiomeEnemies;
   public Enemy.Type[] lavaBiomeEnemies;
   public Enemy.Type[] snowBiomeEnemies;
   public Enemy.Type[] pineBiomeEnemies;
   public Enemy.Type[] mushroomBiomeEnemies;

   // The respawn timer if any
   public float respawnTimer = -1;

   #endregion

   private void Awake () {
      if (!NetworkServer.active) {
         return;
      }

      Area area = GetComponentInParent<Area>();
      Enemy_Spawner spawner = gameObject.AddComponent<Enemy_Spawner>();
      spawner.setRandomSpawner(this);
      EnemyManager.self.storeSpawner(spawner, area.areaKey);
   }

   public Enemy.Type getRandomEnemyType (Biome.Type biome) {
      switch (biome) {
         case Biome.Type.Forest:
            return forestBiomeEnemies.ChooseRandom();
         case Biome.Type.Desert:
            return desertBiomeEnemies.ChooseRandom();
         case Biome.Type.Pine:
            return pineBiomeEnemies.ChooseRandom();
         case Biome.Type.Snow:
            return snowBiomeEnemies.ChooseRandom();
         case Biome.Type.Lava:
            return lavaBiomeEnemies.ChooseRandom();
         case Biome.Type.Mushroom:
            return mushroomBiomeEnemies.ChooseRandom();
      }
      return Enemy.Type.None;
   }

   public int getEnemyBiomeCount (Biome.Type biome) {
      switch (biome) {
         case Biome.Type.Forest:
            return forestBiomeEnemies.Length;
         case Biome.Type.Desert:
            return desertBiomeEnemies.Length;
         case Biome.Type.Pine:
            return pineBiomeEnemies.Length;
         case Biome.Type.Snow:
            return snowBiomeEnemies.Length;
         case Biome.Type.Lava:
            return lavaBiomeEnemies.Length;
         case Biome.Type.Mushroom:
            return mushroomBiomeEnemies.Length;
      }
      return 0;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.RESPAWN_TIME) == 0) {
            float newRespawnTime = float.Parse(field.v.Split(':')[0]);
            respawnTimer = newRespawnTime;
         }
      }
   }

   #region Private Variables

   #endregion
}
