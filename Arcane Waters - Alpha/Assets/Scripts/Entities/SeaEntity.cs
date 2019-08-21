using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaEntity : NetEntity {
   #region Public Variables

   // The amount of damage we do
   [SyncVar]
   public int damage = 25;

   // How long we have to wait to reload
   [SyncVar]
   public float reloadDelay = 1f;

   // The left Attack Box
   public PolygonCollider2D leftAttackBox;

   // The right Attack Box
   public PolygonCollider2D rightAttackBox;

   // The prefab we use for creating Attack Circles
   public AttackCircle attackCirclePrefab;

   // The container for our sprites
   public GameObject spritesContainer;

   // The container for our ripples
   public GameObject ripplesContainer;

   // Convenient object references to the left and right side of our entity
   public GameObject leftSideTarget;
   public GameObject rightSideTarget;

   // Determines if this monster can be damaged
   public bool invulnerable;

   #endregion

   protected override void Start () {
      base.Start();

      // Keep track in our Sea Manager
      SeaManager.self.storeEntity(this);

      // Make sea entities clickable on the client
      if (isClient) {
         ClickTrigger clickTrigger = Instantiate(PrefabsManager.self.clickTriggerPrefab);
         clickTrigger.transform.SetParent(this.transform);
      }

      // Set our sprite sheets according to our types
      StartCoroutine(CO_UpdateAllSprites());
   }

   protected override void Update () {
      base.Update();

      // If we've died, start slowing moving our sprites downward
      if (currentHealth <= 0) {
         Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .03f * Time.smoothDeltaTime);

         // Fade the sprites out
         if (!Application.isBatchMode) {
            foreach (SpriteRenderer renderer in _renderers) {
               float newAlpha = Mathf.Lerp(1f, 0f, spritesContainer.transform.localPosition.y * -10f);
               Util.setMaterialBlockAlpha(renderer, newAlpha);
            }
         }
      }
   }

   public virtual void playAttackSound () {
      // Let other classes override and implement this functionality
   }

   public bool hasRecentCombat () {
      // Did we recently attack?
      if (Time.time - _lastAttackTime < RECENT_COMBAT_COOLDOWN) {
         return true;
      }

      // Did we recently take damage?
      if (Time.time - _lastDamagedTime < RECENT_COMBAT_COOLDOWN) {
         return true;
      }

      return false;
   }

   public bool hasAnyCombat () {
      return (hasAttackers() || _lastAttackTime > 0f);
   }

   [ClientRpc]
   public void Rpc_CreateAttackCircle (Vector2 startPos, Vector2 endPos, float startTime, float endTime, Attack.Type attackType) {
      // Create a new Attack Circle object from the prefab
      AttackCircle attackCircle = Instantiate(attackCirclePrefab, endPos, Quaternion.identity);
      attackCircle.creator = this;
      attackCircle.startPos = startPos;
      attackCircle.endPos = endPos;
      attackCircle.startTime = startTime;
      attackCircle.endTime = endTime;
      attackCircle.hasBeenPlaced = true;

      // Create a cannon smoke effect
      Vector2 direction = endPos - startPos;
      Vector2 offset = direction.normalized * .1f;
      Instantiate(PrefabsManager.self.cannonSmokePrefab, startPos + offset, Quaternion.identity);

      // Create a cannon ball
      CannonBall ball = Instantiate(PrefabsManager.self.getCannonBallPrefab(attackType), startPos, Quaternion.identity);
      ball.creator = this;
      ball.startPos = startPos;
      ball.endPos = endPos;
      ball.startTime = startTime;
      ball.endTime = endTime;

      // Play an appropriate sound
      playAttackSound();

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   public void Rpc_FireHomingCannonBall (GameObject source, GameObject target, float startTime, float endTime) {
      // Create a cannon smoke effect
      Vector2 direction = target.transform.position - source.transform.position;
      Vector3 offset = direction.normalized * .1f;
      Vector3 startPos = source.transform.position + offset;
      Instantiate(PrefabsManager.self.cannonSmokePrefab, startPos, Quaternion.identity);

      // Create a cannon ball
      CannonBall ball = Instantiate(PrefabsManager.self.cannonBallPrefab, startPos, Quaternion.identity);
      ball.creator = this;
      ball.startPos = startPos;
      ball.endPos = target.transform.position;
      ball.targetObject = target;
      ball.startTime = startTime;
      ball.endTime = endTime;

      // Play an appropriate sound
      playAttackSound();
   }

   [ClientRpc]
   public void Rpc_ShowExplosion (Vector2 pos, int damage, Attack.Type attackType) {
      _lastDamagedTime = Time.time;

      if (attackType == Attack.Type.None) {
         List<Effect.Type> effectTypes = EffectManager.getEffects(attackType);
         EffectManager.show(effectTypes, pos);

         // Play the damage sound
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_1, pos);
      } else {
         // Show the explosion
         if (attackType != Attack.Type.Ice) {
            Instantiate(PrefabsManager.self.explosionPrefab, pos, Quaternion.identity);
         }

         // Show the damage text
         ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(attackType), pos, Quaternion.identity);
         damageText.setDamage(damage);
      }

      // Play the damage sound
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_2, pos);
   }

   [ClientRpc]
   public void Rpc_ShowDamageText(int damage, int attackerId, Attack.Type attackType) {
      _lastDamagedTime = Time.time;

      // Note the attacker
      SeaEntity attacker = SeaManager.self.getEntity(attackerId);
      _attackers.Add(attacker);

      // Show some visual effects when the damage occurs
      List<Effect.Type> effectTypes = EffectManager.getEffects(attackType);
      EffectManager.show(effectTypes, this.transform.position);

      // Show the damage text
      ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(attackType), this.transform.position, Quaternion.identity);
      damageText.setDamage(damage);

      // Play the damage sound
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_1, this.transform.position);

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   protected void Rpc_NoteAttack () {
      // Note the time at which we last performed an attack
      _lastAttackTime = Time.time;
   }

   public float getLastAttackTime () {
      return _lastAttackTime;
   }

   public virtual void noteAttacker (NetEntity entity) {
      _attackers.Add(entity);
   }

   public bool hasReloaded () {
      float timeSinceAttack = Time.time - _lastAttackTime;

      return timeSinceAttack > this.reloadDelay;
   }

   protected IEnumerator CO_UpdateAllSprites () {
      // Wait until we receive data
      while (Util.isEmpty(this.entityName)) {
         yield return null;
      }

      // Set the new sprite
      if (this is ShipEntity) {
         ShipEntity ship = (ShipEntity) this;
         ship.spritesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(Ship.getSkinPath(ship.shipType, ship.skinType));
         ship.ripplesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(Ship.getRipplesPath(ship.shipType));
      }

      // Recolor our flags based on our Nation
      ColorKey colorKey = new ColorKey(Ship.Type.Brigantine, Layer.Flags);
      spritesContainer.GetComponent<RecoloredSprite>().recolor(colorKey, Nation.getColor1(nationType), Nation.getColor2(nationType));

      if (!Util.isEmpty(this.entityName)) {
         this.nameText.text = this.entityName;
      }
   }

   [Command]
   public void Cmd_MeleeAtSpot (Vector2 spot, Attack.Type attackType) {
      // We handle the logic in a non-Cmd function so that it can be called directly on the server if needed
      meleeAtSpot(spot, attackType);
   }

   [Server]
   public void meleeAtSpot (Vector2 spot, Attack.Type attackType) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // If the requested spot is not in the allowed area, reject the request
      if (!leftAttackBox.OverlapPoint(spot) && !rightAttackBox.OverlapPoint(spot)) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = Time.time;

      float distance = Vector2.Distance(this.transform.position, spot);
      float delay = Mathf.Clamp(distance, .5f, 1.5f);

      // Have the server check for collisions after the cannonball reaches the target
      StartCoroutine(CO_CheckCircleForCollisions(this, delay, spot, attackType, true));

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   [Command]
   public void Cmd_FireAtSpot (Vector2 spot, Attack.Type attackType) {
      // We handle the logic in a non-Cmd function so that it can be called directly on the server if needed
      fireAtSpot(spot, attackType);
   }

   [Server]
   public void fireAtSpot (Vector2 spot, Attack.Type attackType) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // If the requested spot is not in the allowed area, reject the request
      if (!leftAttackBox.OverlapPoint(spot) && !rightAttackBox.OverlapPoint(spot)) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = Time.time;

      // Tell all clients to display an attack circle at that position
      float distance = Vector2.Distance(this.transform.position, spot);
      float delay = Mathf.Clamp(distance, .5f, 1.5f);
      Rpc_CreateAttackCircle(this.transform.position, spot, Time.time, Time.time + delay, attackType);

      // Have the server check for collisions after the cannonball reaches the target
      StartCoroutine(CO_CheckCircleForCollisions(this, delay, spot, attackType, false));

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   [Server]
   protected IEnumerator CO_CheckCircleForCollisions (SeaEntity attacker, float delay, Vector2 circleCenter, Attack.Type attackType, bool targetPlayersOnly) {
      // Wait until the cannon ball reaches the target
      yield return new WaitForSeconds(delay);

      List<NetEntity> enemyHitList = new List<NetEntity>();
      // Check for collisions inside the circle
      foreach (Collider2D hit in getHitColliders(circleCenter)) {
         if (hit != null) {
            SeaEntity entity = hit.GetComponent<SeaEntity>();

            if (!enemyHitList.Contains(entity)) {
               if (targetPlayersOnly) {
                  if (hit.GetComponent<ShipEntity>() == null) {
                     continue;
                  }
               }

               // Make sure the target is in our same instance
               if (entity != null && entity.instanceId == this.instanceId) {
                  if (!entity.invulnerable) {
                     int damage = (int) (this.damage * Attack.getDamageModifier(attackType));
                     entity.currentHealth -= damage;
                     entity.Rpc_ShowDamageText(damage, attacker.userId, attackType);
                  } else {
                     entity.Rpc_ShowExplosion(entity.transform.position, 0, Attack.Type.None);
                  }
                  entity.noteAttacker(attacker);

                  // Apply any status effects from the attack
                  if (attackType == Attack.Type.Ice) {
                     StatusManager.self.create(Status.Type.Freeze, 2f, entity.userId);
                  } else if (attackType == Attack.Type.Air) {
                     StatusManager.self.create(Status.Type.Slow, 3f, entity.userId);
                  } else if (attackType == Attack.Type.Tentacle) {
                     StatusManager.self.create(Status.Type.Slow, 1f, entity.userId);
                  }
                  enemyHitList.Add(entity);
               }
            }
         }
      }
   }

   public static Collider2D[] getHitColliders (Vector2 circleCenter) {
      // Check for collisions inside the circle
      Collider2D[] hits = new Collider2D[16];
      Physics2D.OverlapCircleNonAlloc(circleCenter, .20f, hits);

      return hits;
   }

   #region Private Variables

   // The time at which we last fired an attack
   protected float _lastAttackTime = float.MinValue;

   // The time at which we last took damage
   protected float _lastDamagedTime = float.MinValue;

   // How far back in time we check to see if this ship was recently involved in some combat action
   protected static float RECENT_COMBAT_COOLDOWN = 5f;

   #endregion
}
