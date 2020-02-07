using System;
using UnityEngine;

public class BiomableSpriteRenderer : MonoBehaviour, IBiomable
{
   #region Public Variables

   [Tooltip("Whether to update the sprite renderers on child gameobjects as well")]
   public bool includeChildren = false;

   [Tooltip("The current set biome")]
   public Biome.Type currentBiome = Biome.Type.Forest;

   #endregion

   public void setBiome (Biome.Type biomeType) {
      if (includeChildren) {
         foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
            renderer.sprite = Util.switchSpriteBiome(renderer.sprite, currentBiome, biomeType);
         }
      } else {
         foreach (SpriteRenderer renderer in GetComponents<SpriteRenderer>()) {
            renderer.sprite = Util.switchSpriteBiome(renderer.sprite, currentBiome, biomeType);
         }
      }

      currentBiome = biomeType;
   }

   #region Private Variables

   #endregion
}
