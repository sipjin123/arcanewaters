using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class GenericCombatCollider : MonoBehaviour
{
   #region Public Variables

   // The sprite renderer that will determine the shape of the collider
   public SpriteRenderer spriteRenderer;

   // If the collider adjusts dynamically
   public bool dynamicCollider = true;

   #endregion

   protected virtual void Awake () {
      if (spriteRenderer == null) {
         D.error($"CombatCollider in game object {gameObject.name} hasn't been assigned a SpriteRenderer. The shape of the collider won't be updated.");
         dynamicCollider = false;
         transform.localPosition = Vector3.zero;
      } else {
         // The position of the collider should match the position of the sprite
         transform.localPosition = spriteRenderer.transform.localPosition;
      }

      // The collider shouldn't be rotated unless we rotate the sprite 
      transform.localEulerAngles = Vector3.zero;

      _defaultScale = transform.localScale;
   }

   protected virtual void Update () {
      if (dynamicCollider) {
         if (_lastSprite != spriteRenderer.sprite) {
            updateCollider();
            _lastSprite = spriteRenderer.sprite;
         }

         Vector3 scale = transform.localScale;
         scale.x = _defaultScale.x * (spriteRenderer.flipX ? -1 : 1);
         transform.localScale = scale;
      }
   }

   protected abstract void updateCollider ();

   public void setScale (Vector3 scale) {
      transform.localScale = scale;
      _defaultScale = scale;
   }

   #region Private Variables

   // The sprite that's used for setting the current shape of the collider
   protected Sprite _lastSprite;

   // The original scale of the collider
   protected Vector3 _defaultScale;

   #endregion
}