using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class SeaMine : NetworkBehaviour, IObserver
{
   #region Public Variables

   // The minimum damage this mine can cause
   public int minDamage = 10;

   // The maximum damage this mine can cause
   public int maxDamage = 120;

   // The instance ID of this mine
   public int instanceId;

   // The net id of the entity that created this mine
   [SyncVar]
   public uint sourceEntityNetId;

   // A reference to the effector that will provide the explosive force
   public PointEffector2D explosionEffector;

   // A reference to the collider for the explosion effector
   public CircleCollider2D explosionEffectorCollider;

   // References to the sprite renderers for this mine
   public SpriteRenderer barrelSpriteRenderer, waterSpriteRenderer;

   // The color of the detection range indicator circle
   public Color detectionRangeColor;

   // A reference to the mesh renderer that displays our explosion range
   public MeshRenderer detectionRangeRenderer;

   // References to the animators for this sea mine
   public Animator barrelAnimator, waterAnimator;

   // A reference to the game object that will display the explosion visual effect
   public GameObject explosionVisualEffect;

   // A reference to the sprite group controlling the alpha value of the explosion sprites
   public SpriteGroup explosionSpriteGroup;

   // A reference to the sprite outline for this mine
   public SpriteOutline spriteOutline;

   public enum MineState { None = 0, Armed = 1, Triggered = 2, Exploded = 3 }

   #endregion

   private void Awake () {
      _collider = GetComponent<CircleCollider2D>();
      updateVisualsForState(_state);

      detectionRangeRenderer.material.SetFloat("_Radius", _explosionRadius);
      detectionRangeRenderer.material.SetFloat("_FillAmount", 1.0f);
      Color circleStartColor = detectionRangeColor;
      circleStartColor.a = 0.0f;
      detectionRangeRenderer.material.SetColor("_Color", circleStartColor);
      detectionRangeRenderer.material.SetVector("_Position", transform.position);
   }

   private void Start () {
      PlayerShipEntity globalPlayerShip = getGlobalPlayerShip();
      SeaEntity sourceEntity = SeaManager.self.getEntity(sourceEntityNetId);
      
      if (globalPlayerShip != null && sourceEntity != null) {
         Color outlineColor = (sourceEntity.isEnemyOf(globalPlayerShip)) ? Color.red : Color.green;
         spriteOutline.setNewColor(outlineColor);
      }

      // Playing the sea mine deploy sound effect for all clients
      if(NetworkServer.active) {
         Rpc_PlayDeploySound();
      }
   }

   public void init (int instanceId, uint sourceEntityNetId, float explosionRadius, float explosionForce) {
      this.instanceId = instanceId;
      this.sourceEntityNetId = sourceEntityNetId;

      _collider.radius = explosionRadius;
      explosionEffectorCollider.radius = explosionRadius;
      _explosionRadius = explosionRadius;
      explosionEffector.forceMagnitude = explosionForce;

      StartCoroutine(CO_ArmAfterDelay(ARMING_DELAY));
      StartCoroutine(CO_DestroyAtEndOfLifetime());
   }

   private void Update () {
      if (_previousState != _state) {
         updateVisualsForState(_state);
      }

      float lerpTargetAlpha = 0.0f;

      if (_state == MineState.Armed) {
         PlayerShipEntity playerShipEntity = getGlobalPlayerShip();
         SeaEntity sourceEntity = SeaManager.self.getEntity(sourceEntityNetId);

         // If this is an enemy's mine, show its detection radius
         if (playerShipEntity != null && sourceEntity != null && playerShipEntity.isEnemyOf(sourceEntity)) {
            float distanceFromMine = (playerShipEntity.transform.position - transform.position).magnitude;
            bool shouldShowRadius = (distanceFromMine <= _explosionRadius * 1.5f);
            lerpTargetAlpha = (shouldShowRadius) ? 0.5f : 0.0f;
         }
      } else if (_state == MineState.Triggered) {
         // Update material flash
         float timeSinceTriggered = (float)(NetworkTime.time - _timeTriggered);
         int frameConversion = Mathf.Clamp(Mathf.FloorToInt((timeSinceTriggered / EXPLOSION_DELAY) * _mineFlashAmount.Length), 0, _mineFlashAmount.Length - 1);
         float flashAmount = _mineFlashAmount[frameConversion];
         barrelSpriteRenderer.material.SetFloat("_FlashAmount", flashAmount);
      }

      _detectionRangeCircleAlpha = Mathf.Lerp(_detectionRangeCircleAlpha, lerpTargetAlpha, Time.deltaTime * 1.5f);
      detectionRangeColor.a = _detectionRangeCircleAlpha;
      detectionRangeRenderer.material.SetColor("_Color", detectionRangeColor);

      _previousState = _state;
   }

   private void updateVisualsForState (MineState newState) {
      if (Util.isBatch()) {
         return;
      }
      
      if (newState == MineState.Triggered) {
         barrelAnimator.SetTrigger("Trigger");
         waterAnimator.SetTrigger("Trigger");
      } else if (newState == MineState.Armed) {
         spriteOutline.setVisibility(true);
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      checkCollisions(collision);
   }

   private void OnTriggerStay2D (Collider2D collision) {
      checkCollisions(collision);
   }

   private void checkCollisions (Collider2D collision) {
      // We will only check for collisions on the server
      if (!NetworkServer.active) {
         return;
      }

      // Don't check for collisions if we aren't armed, or if we are already counting down to explode
      if (_state == MineState.None || _state == MineState.Triggered) {
         return;
      }

      // We need information from the source entity to interact with other entities, so if they don't exist, this mine will be destroyed
      SeaEntity sourceEntity = SeaManager.self.getEntity(sourceEntityNetId);
      if (sourceEntity == null) {
         NetworkServer.Destroy(gameObject);
         return;
      }

      SeaEntity detectedEntity = collision.GetComponent<SeaEntity>();
      if (detectedEntity != null && detectedEntity.instanceId == instanceId && detectedEntity.isEnemyOf(sourceEntity, false) && !detectedEntity.isDead()) {
         StartCoroutine(CO_TriggerExplosionAfterDelay(EXPLOSION_DELAY));
      }
   }

   private IEnumerator CO_ArmAfterDelay (float delay) {
      yield return new WaitForSeconds(delay);
      _state = MineState.Armed;
   }

   private IEnumerator CO_TriggerExplosionAfterDelay (float delay) {
      // We can only trigger explosions on armed mines
      if (_state != MineState.Armed) {
         yield break;
      }
      
      _state = MineState.Triggered;
      Rpc_NotifyTriggered();

      yield return new WaitForSeconds(delay);

      explode();
   }

   private IEnumerator CO_DestroyAtEndOfLifetime () {
      yield return new WaitForSeconds(LIFETIME);

      if (_state == MineState.None || _state == MineState.Armed) {
         NetworkServer.Destroy(gameObject);
      }
   }

   private void explode () {
      // We need information from the source entity to interact with other entities, so if they don't exist, this mine will be destroyed
      SeaEntity sourceEntity = SeaManager.self.getEntity(sourceEntityNetId);
      if (sourceEntity == null) {
         NetworkServer.Destroy(gameObject);
         return;
      }

      // Apply damage to all enemies hit
      List<SeaEntity> enemiesHit = Util.getEnemiesInCircle(sourceEntity, transform.position, _explosionRadius);
      foreach (SeaEntity enemyHit in enemiesHit) {
         float distanceToEnemy = (enemyHit.transform.position - transform.position).magnitude;
         float damageQuotient = Mathf.InverseLerp(0.0f, _explosionRadius, distanceToEnemy);
         int damageAmount = (int) Mathf.Lerp(minDamage, maxDamage, damageQuotient);
         int finalDamage = enemyHit.applyDamage(damageAmount, sourceEntityNetId);

         enemyHit.Rpc_ShowDamageTaken(finalDamage, false);
      }

      // If our explosion hits another sea mine, detonate it
      Collider2D[] entitiesHit = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, LayerMask.GetMask(LayerUtil.SHIPS));
      foreach (Collider2D hit in entitiesHit) { 
         if (hit.attachedRigidbody != null) {
            SeaMine hitSeaMine = hit.attachedRigidbody.GetComponent<SeaMine>();
            if (hitSeaMine != null) {
               hitSeaMine.StartCoroutine(hitSeaMine.CO_TriggerExplosionAfterDelay(EXPLOSION_DELAY));
            }
         }
      }

      _state = MineState.Exploded;
      explosionEffector.gameObject.SetActive(true);
      Rpc_ShowExplosion();
      StartCoroutine(CO_DestroyDelayed());
   }

   private IEnumerator CO_DestroyDelayed () {
      yield return new WaitForSeconds(0.5f);
      explosionEffector.gameObject.SetActive(false);

      yield return new WaitForSeconds(1.0f);
      NetworkServer.Destroy(gameObject);
   }

   [ClientRpc]
   private void Rpc_ShowExplosion () {
      explosionVisualEffect.transform.localScale = Vector3.one * _explosionRadius;
      explosionVisualEffect.SetActive(true);

      Sequence fadeSequence = DOTween.Sequence();
      fadeSequence.AppendInterval(0.2f);
      fadeSequence.Append(DOTween.To(() => explosionSpriteGroup.alpha, (x) => explosionSpriteGroup.alpha = x, 0.0f, 0.75f));
      fadeSequence.AppendCallback(() => Destroy(explosionVisualEffect.gameObject));

      SoundEffectManager.self.playSeaMineExplosionSfx(explosionVisualEffect.transform.position);

      barrelSpriteRenderer.enabled = false;
      waterSpriteRenderer.enabled = false;
      _collider.enabled = false;
   }

   [ClientRpc]
   private void Rpc_NotifyTriggered () {
      _timeTriggered = (float)NetworkTime.time;
   }

   [ClientRpc]
   public void Rpc_PlayDeploySound () {
      SoundEffectManager.self.playProjectileTerrainHitSound(false,false, SoundEffectManager.ProjectileType.Sea_Mine, this.transform, null);
   }

   private PlayerShipEntity getGlobalPlayerShip () {
      if (!_globalPlayerShip && Global.player) {
         _globalPlayerShip = Global.player.getPlayerShipEntity();
      }
      return _globalPlayerShip;
   }

   public MineState getMineState () {
      return _state;
   }

   public int getInstanceId () {
      return instanceId;
   }

   #region Private Variables

   // The trigger collider
   protected CircleCollider2D _collider;

   // Indicates what state the mine is currently in
   [SyncVar]
   protected MineState _state = MineState.None;

   // What state we last recorded the mine as being
   protected MineState _previousState = MineState.None;

   [SyncVar]
   // The range in which this explosion will effect entities
   protected float _explosionRadius = 0.5f;

   // How long the mine takes to arm, after being created
   protected const float ARMING_DELAY = 1.0f;

   // How long the mine takes to explode, after being triggered
   protected const float EXPLOSION_DELAY = 0.6f;

   // How long the mine will last for, before being destroyed
   protected const float LIFETIME = 30.0f;

   // A timestamp indicating when this mine was triggered to explode
   protected float _timeTriggered = 0.0f;

   // The current alpha for our detection range circle
   protected float _detectionRangeCircleAlpha = 0.0f;

   // A reference to the global player's ship
   private PlayerShipEntity _globalPlayerShip = null;

   // The 'FlashAmount' value we will pass to the sea mine shader over time, while it is counting down to detonate
   private readonly float[] _mineFlashAmount = { 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0,0f, 1.0f, 1.0f };

   #endregion
}
