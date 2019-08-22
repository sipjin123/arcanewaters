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

   // The Area Type that's associated with this map configuration
   public Area.Type areaType;

   // The Type of Biome that's associated with this map configuration
   public Biome.Type biomeType;

   #endregion

   public MapConfig (int seed, float persistance, float lacunarity, Vector2 offset, Area.Type areaType, Biome.Type biomeType) {
      this.seed = seed;
      this.persistance = persistance;
      this.lacunarity = lacunarity;
      this.offset = offset;
      this.areaType = areaType;
      this.biomeType = biomeType;
   }

   #region Private Variables

   #endregion
}
