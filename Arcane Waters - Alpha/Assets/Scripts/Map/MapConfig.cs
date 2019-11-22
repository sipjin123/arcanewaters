using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

// Struct for data used to deterministic creation of map
[System.Serializable]
public struct MapConfig
{
   #region Public Variables

   // The seed we're going to use to generate the random values for this particular map
   public int seed;

   // Setting that affects the appearance of the generated map
   public float persistance;

   // Setting that affects the appearance of the generated map
   public float lacunarity;

   // Setting that affects the appearance of the generated map
   public Vector2 offset;

   // The Area that's associated with this map configuration
   public string areaKey;

   // The Type of Biome that's associated with this map configuration
   public Biome.Type biomeType;

   // Seed for decision made during generating path
   public int seedPath;

   #endregion

   public MapConfig (int seed, float persistance, float lacunarity, Vector2 offset, string areaKey, Biome.Type biomeType, int seedPath) {
      this.seed = seed;
      this.persistance = persistance;
      this.lacunarity = lacunarity;
      this.offset = offset;
      this.areaKey = areaKey;
      this.biomeType = biomeType;
      this.seedPath = seedPath;
   }

   public override string ToString () {
      return seed.ToString() + " : " + seedPath.ToString() + " : " + lacunarity.ToString() + " : " + persistance.ToString();
   }

   public override bool Equals (object obj) {
      MapConfig mapConfig = (MapConfig) obj;
      return mapConfig.seed == seed &&
         mapConfig.seedPath == seedPath &&
         mapConfig.lacunarity == lacunarity &&
         mapConfig.persistance == persistance;
   }

   public override int GetHashCode () {
      var hashCode = -1996336407;
      hashCode = hashCode * -1521134295 + seed.GetHashCode();
      hashCode = hashCode * -1521134295 + persistance.GetHashCode();
      hashCode = hashCode * -1521134295 + lacunarity.GetHashCode();
      hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(offset);
      hashCode = hashCode * -1521134295 + areaKey.GetHashCode();
      hashCode = hashCode * -1521134295 + biomeType.GetHashCode();
      hashCode = hashCode * -1521134295 + seedPath.GetHashCode();
      return hashCode;
   }

   #region Private Variables

   #endregion
}
