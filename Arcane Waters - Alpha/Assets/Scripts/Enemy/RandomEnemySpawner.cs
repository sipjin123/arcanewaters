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

   #endregion

   private void Awake () {
      if (!Util.isServer()) {
         return;
      }

      Enemy.Type type = Enemy.Type.None;
      Area area = GetComponentInParent<Area>();
      switch (area.biome) {
         case Biome.Type.Forest:
            type = forestBiomeEnemies.ChooseRandom();
            break;
         case Biome.Type.Desert:
            type = desertBiomeEnemies.ChooseRandom();
            break;
         case Biome.Type.Pine:
            type = pineBiomeEnemies.ChooseRandom();
            break;
         case Biome.Type.Snow:
            type = snowBiomeEnemies.ChooseRandom();
            break;
         case Biome.Type.Lava:
            type = lavaBiomeEnemies.ChooseRandom();
            break;
         case Biome.Type.Mushroom:
            type = mushroomBiomeEnemies.ChooseRandom();
            break;
      }

      Enemy_Spawner spawner = gameObject.AddComponent<Enemy_Spawner>();
      spawner.enemyType = type;
      EnemyManager.self.storeSpawner(spawner, area.areaKey);
   }

   public void receiveData (DataField[] dataFields) {
      // Do not need any data
   }

   #region Private Variables

   #endregion
}
