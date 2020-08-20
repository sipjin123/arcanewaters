using UnityEngine;

[System.Serializable]
public class BiomableSprite
{
   #region Public Variables

   // Sprites for various biomes
   public Sprite desert;
   public Sprite forest;
   public Sprite lava;
   public Sprite pine;
   public Sprite shroom;
   public Sprite snow;

   #endregion

   public Sprite get (Biome.Type biome) {
      switch (biome) {
         case Biome.Type.Desert:
            return desert;
         case Biome.Type.Forest:
            return forest;
         case Biome.Type.Lava:
            return lava;
         case Biome.Type.Pine:
            return pine;
         case Biome.Type.Mushroom:
            return shroom;
         case Biome.Type.Snow:
            return snow;
      }

      return null;
   }

   #region Private Variables

   #endregion
}
