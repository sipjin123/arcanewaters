using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class KnockbackEffect : MonoBehaviour {
   #region Public Variables

   // A list of references to the sprites used for the knockback effect, in the order NNE, NE, ENE
   public List<Sprite> projectileSprites;

   // A reference to the sprite renderer for the explosion start
   public SpriteRenderer explosionStartRenderer;

   // A reference to the game object that displays the water shockwave
   public GameObject waterShockwaveObject;

   #endregion

   public void init (float effectRadius) {
      explosionStartRenderer.DOFade(0.0f, FADE_DURATION);

      float scaleFactor = effectRadius / DEFAULT_RADIUS;
      explosionStartRenderer.transform.localScale = Vector3.one * scaleFactor;
      waterShockwaveObject.transform.localScale = Vector3.one * scaleFactor;

      StartCoroutine(CO_SpawnProjectiles(effectRadius));
      StartCoroutine(CO_DestroyEffect(EFFECT_DURATION));
   }

   private IEnumerator CO_SpawnProjectiles (float effectRadius) {
      yield return new WaitForSeconds(PROJECTILE_SPAWN_DELAY);
      
      float anglePerProjectile = 360.0f / NUM_PROJECTILES;

      for (int i = 0; i < NUM_PROJECTILES; i++) {
         float launchAngle = 15.0f + i * anglePerProjectile;
         Vector2 endPosition = Quaternion.Euler(0.0f, 0.0f, -launchAngle) * Vector3.up * effectRadius;
         float spawnRotationZ = (Mathf.FloorToInt(i / 3) * 90.0f);
         GameObject projectile = Instantiate(PrefabsManager.self.knockbackEffectProjectilePrefab, transform.position, Quaternion.Euler(0.0f, 0.0f, -spawnRotationZ), transform);
         projectile.transform.localPosition = new Vector3(0.0f, 0.0f, -0.01f);
         projectile.GetComponent<SpriteRenderer>().sprite = projectileSprites[i % 3];
         projectile.transform.DOLocalMove(endPosition, EFFECT_DURATION - PROJECTILE_SPAWN_DELAY).SetEase(Ease.OutCubic);
      }
   }

   private IEnumerator CO_DestroyEffect (float afterSeconds) {
      yield return new WaitForSeconds(afterSeconds);

      Destroy(this.gameObject);
   }

   #region Private Variables

   // The radius of the water shockwave sprite
   private const float DEFAULT_RADIUS = 0.62f;

   // How many projectiles will be created
   private const int NUM_PROJECTILES = 12;

   // How long the knockback effect takes to fade in or out
   private const float FADE_DURATION = 0.125f;

   // How long the knockback effect lasts for
   private const float EFFECT_DURATION = 0.5f;

   // How long a delay there is before the projectiles are spawned
   private const float PROJECTILE_SPAWN_DELAY = 0f;

   #endregion
}
