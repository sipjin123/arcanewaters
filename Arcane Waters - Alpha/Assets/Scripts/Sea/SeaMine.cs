using UnityEngine;
using Mirror;
using System;
using System.Collections;

public class SeaMine : NetworkBehaviour {
   #region Public Variables

   // The force of the explosion for objects that are closer to the mine
   public float maxExplosiveForce = 100.0f;

   // The force of the explosion for objects that are inside the radius but far from the mine
   public float minExplosiveForce = 25.0f;

   // The radius of the explosion
   public float explosionRadius = 1.0f;

   // The minimum damage this mine can cause
   public int minDamage = 10;

   // The maximum damage this mine can cause
   public int maxDamage = 120;

   // The instance ID of this mine
   public int instanceId;

   #endregion

   private void Awake () {
      _collider = GetComponent<Collider2D>();
      _spriteRenderer = GetComponent<SpriteRenderer>();
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      // Create an explosive force at the position of the mine if a player touches the mine
      if (NetworkServer.active) {
         PlayerShipEntity entity = collision.gameObject.GetComponent<PlayerShipEntity>();
         if (entity != null && entity.instanceId == instanceId) {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

            // Disable the collider so the mine can't be triggered again before we destroy it
            _collider.enabled = false;

            foreach (Collider2D col in colliders) {
               SeaEntity seaEntity = col.GetComponent<SeaEntity>();

               // We only want to push away SeaEntities
               if (seaEntity != null && seaEntity.instanceId == instanceId) {
                  Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
                
                  if (rb != null) {
                     // Apply some damage to the entity
                     float distance = (seaEntity.transform.position - transform.position).magnitude;
                     float damageAmount = Mathf.InverseLerp(0, explosionRadius, distance);
                     int damage = (int)Mathf.Lerp(minDamage, maxDamage, damageAmount);
                     int finalDamage = seaEntity.applyDamage(damage, netId);

                     seaEntity.Rpc_ShowDamageTaken(finalDamage, true);

                     // Don't apply the force if the explosion destroyed the ship
                     if (!seaEntity.isDead()) {
                        rb.AddExplosiveForce(minExplosiveForce, maxExplosiveForce, explosionRadius, transform.position);
                     }
                  }
               }
            }

            Rpc_ShowExplosion();

            StartCoroutine(CO_destroyDelayed());
         }
      }
   }

   private IEnumerator CO_destroyDelayed () {      
      yield return new WaitForSeconds(1.0f);
      NetworkServer.Destroy(gameObject);
   }

   [ClientRpc]
   private void Rpc_ShowExplosion () {
      GameObject explosionEffect = PrefabsManager.self.requestCannonExplosionPrefab(Attack.ImpactMagnitude.Strong);
      explosionEffect.transform.position = transform.position;

      SoundEffectManager.self.playFmodSoundEffect(SoundEffectManager.SHIP_CANNON, this.transform);
      //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Cannon_1, transform.position);

      _spriteRenderer.enabled = false;
      _collider.enabled = false;
   }

   #region Private Variables

   // The trigger collider
   protected Collider2D _collider;

   // The sprite renderer
   protected SpriteRenderer _spriteRenderer;

   #endregion
}
