using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(BoxCollider2D))]
public class CombatBoxCollider : GenericCombatCollider
{
   #region Public Variables

   #endregion

   private void Start () {
      _collider = GetComponent<BoxCollider2D>();
   }

   protected override void updateCollider () {
      Sprite sprite = spriteRenderer.sprite;
      
      // Make sure our ship's sprite has a valid physics outline ("Generate Physics Shape" needs to be enabled in the import settings)
      int shapesCount = sprite.GetPhysicsShape(0, _spriteShapePoints);

      if (shapesCount > 0) {
         // Find the minimum and maximum points of the sprite and adapt the size of the collider to it
         Vector2 minPoint = new Vector2(_spriteShapePoints[0].x, _spriteShapePoints[0].y);
         Vector2 maxPoint = new Vector2(_spriteShapePoints[0].x, _spriteShapePoints[0].y);

         for (int i = 1; i < shapesCount; i++) {
            minPoint.x = Mathf.Min(minPoint.x, _spriteShapePoints[i].x);
            minPoint.y = Mathf.Min(minPoint.y, _spriteShapePoints[i].y);
            maxPoint.x = Mathf.Max(maxPoint.x, _spriteShapePoints[i].x);
            maxPoint.y = Mathf.Max(maxPoint.y, _spriteShapePoints[i].y);
         }

         // Find the offset (in case the sprite isn't perfectly centered)
         Vector2 offset = (minPoint + maxPoint) * 0.5f;
         
         // Calculate the width and height of the collider
         Vector2 size = new Vector2(Mathf.Abs(minPoint.x) + Mathf.Abs(maxPoint.x), Mathf.Abs(minPoint.y) + Mathf.Abs(maxPoint.y));

         // Let's scale the collider (3/4 the size of the sprite by default) so it's not too big
         _collider.size = size;

         // Apply the offset
         _collider.offset = offset;
      } else {
         // If the sprite doesn't have a valid physics shape, we'll disable the script to avoid spamming the log
         D.error($"The ship {gameObject.name} doesn't have a properly defined outline. Make sure Generate Physics Shape is enabled in the sprite settings. The script will be disabled.");
         this.enabled = false;         
      }
   }

   #region Private Variables

   // A list containing the points of the sprite physics shape
   protected List<Vector2> _spriteShapePoints = new List<Vector2>();

   // Our collider used for projectile detection
   protected BoxCollider2D _collider;

   #endregion
}
