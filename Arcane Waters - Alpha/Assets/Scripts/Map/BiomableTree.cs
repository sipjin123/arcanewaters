using System.Collections.Generic;
using UnityEngine;

public class BiomableTree : MonoBehaviour, IBiomable
{
   #region Public Variables

   [Tooltip("The current set biome")]
   public Biome.Type currentBiome = Biome.Type.Forest;

   #endregion

   public void setBiome (Biome.Type biomeType) {
      foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
         renderer.sprite = Util.switchSpriteBiome(renderer.sprite, currentBiome, biomeType);

         PolygonCollider2D col = renderer.GetComponent<PolygonCollider2D>();
         if (col != null) {
            for (int i = 0; i < col.pathCount; i++) {
               col.SetPath(i, new List<Vector2>());
            }
            col.pathCount = renderer.sprite.GetPhysicsShapeCount();

            List<Vector2> path = new List<Vector2>();
            for (int i = 0; i < col.pathCount; i++) {
               path.Clear();
               renderer.sprite.GetPhysicsShape(i, path);
               col.SetPath(i, path.ToArray());
               List<Vector2> points = new List<Vector2>();
            }
         }
      }

      currentBiome = biomeType;
   }

   #region Private Variables

   #endregion
}
