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
            renderer.sprite = switchSpriteBiome(renderer.sprite, currentBiome, biomeType);
         }
      } else {
         foreach (SpriteRenderer renderer in GetComponents<SpriteRenderer>()) {
            renderer.sprite = switchSpriteBiome(renderer.sprite, currentBiome, biomeType);
         }
      }

      currentBiome = biomeType;
   }

   protected Sprite switchSpriteBiome(Sprite sprite, Biome.Type from, Biome.Type to) {
      try {
         string textureName = sprite.name.Substring(0, sprite.name.Length - 2).TrimEnd('_');
         string targetTextureName = textureName.Replace(from.ToString().ToLower(), to.ToString().ToLower());
         int spriteIndex = int.Parse(sprite.name.Substring(sprite.name.Length - 2).TrimStart('_'));

         return ImageManager.getSprites(@"Biomable/" + targetTextureName)[spriteIndex];
      } catch {
         return sprite;
      }
   }

   #region Private Variables

   #endregion
}
