using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericSeaProjectile : MonoBehaviour {
   #region Public Variables

   // The Type of projectile this is
   public Attack.Type attackType;

   // Our projectile object
   public GameObject projectileObj;

   // The ability data of this projectile
   public ShipAbilityData shipAbilitydata;

   // How high the projectile should arch upwards
   public float archHeight = .20f;

   // Reference to the trail of the projectile
   public TrailRenderer trail;

   // Projectile Sprite
   public SpriteRenderer[] staticSprites;

   // Animated sprite
   public SpriteRenderer[] animatedSprites;

   #endregion

   private void Start () {
      if (shipAbilitydata.hasTrail) {
         trail.enabled = true;
      }
   }

   public void init (float startTime, float endTime, Vector2 startPos, Vector2 endPos, SeaEntity creator, Attack.ImpactMagnitude impactMagnitude, int abilityId, GameObject targetObj = null) {
      if (targetObj != null) {
         _targetObject = targetObj;
      }

      this._impactMagnitude = impactMagnitude;

      _startTime = startTime;
      _endTime = endTime;

      _startPos = startPos;
      _endPos = endPos;

      _creator = creator;

      ShipAbilityData newShipAbilityData = ShipAbilityManager.self.getAbility(abilityId);
      shipAbilitydata = newShipAbilityData;
      attackType = newShipAbilityData.selectedAttackType;

      if (newShipAbilityData.projectileSpritePath != "") {
         Sprite[] spriteArray = ImageManager.getSprites(newShipAbilityData.projectileSpritePath);
         if (spriteArray.Length == 1) {
            foreach (SpriteRenderer spriteRender in staticSprites) {
               spriteRender.sprite = spriteArray[0];
               spriteRender.gameObject.SetActive(true);
            }
         } else if (spriteArray.Length > 1) {
            foreach (SpriteRenderer spriteRender in animatedSprites) {
               spriteRender.sprite = spriteArray[0];
               spriteRender.gameObject.SetActive(true);
            }
            foreach (SpriteRenderer spriteRender in staticSprites) {
               spriteRender.gameObject.SetActive(false);
            }
         } else {
            foreach (SpriteRenderer spriteRender in animatedSprites) {
               spriteRender.gameObject.SetActive(false);
            }
            foreach (SpriteRenderer spriteRender in staticSprites) {
               spriteRender.gameObject.SetActive(false);
            }
         }
      }
   }

   public void setDirection (Direction direction) {
      transform.LookAt(_endPos);
   }

   private void Update () {
      // If a target object has been specified, update our end position
      if (_targetObject != null) {
         _endPos = _targetObject.transform.position;
      }

      // Move from the start to the end point
      float totalLifetime = _endTime - _startTime;
      float lerpTime = (TimeManager.self.getSyncedTime() - _startTime) / totalLifetime;
      Util.setXY(this.transform, Vector2.Lerp(_startPos, _endPos, lerpTime));

      if (shipAbilitydata.hasArch) {
         if (shipAbilitydata.selectedAttackType == Attack.Type.Tentacle_Range) {
            archHeight = .60f;
         }

         // Adjusts the height of the projectile sprite based in an arch
         float angleInDegrees = lerpTime * 180f;
         float ballHeight = Util.getSinOfAngle(angleInDegrees) * archHeight;
         Util.setLocalY(projectileObj.transform, ballHeight);
      }

      if (shipAbilitydata.syncHeightToArch) {
         // Adjusts the height of the projectile sprite based in an arch
         Util.setLocalY(projectileObj.transform, AttackManager.getArcHeight(_startPos, _endPos, lerpTime, false));
      }

      // If we've been alive long enough, destroy ourself
      if (TimeManager.self.getSyncedTime() > this._endTime) {
         bool hitEnemy = false;

         // Create an explosion effect on the client if any targets were hit
         if (Global.player != null) {
            foreach (Collider2D hit in SeaEntity.getHitColliders(this.transform.position)) {
               if (hit != null) {
                  NetEntity entity = hit.GetComponent<NetEntity>();
                  if (entity == null) {
                     continue;
                  }

                  // Make sure we don't hit the creator of the cannon ball
                  if (entity == _creator) {
                     continue;
                  }

                  // Check if the creator and the target are allies
                  if (_creator.isAllyOf(entity)) {
                     continue;
                  }

                  // Make sure the target is in our same instance
                  if (entity.instanceId == Global.player.instanceId) {
                     // If we hit a ship, show some flying particles
                     if (entity is ShipEntity && attackType != Attack.Type.Ice) {
                        ExplosionManager.createExplosion(entity.transform.position);
                     }

                     break;
                  }

                  // Make sure the target is in our same instance
                  if (entity != null && entity.instanceId == Global.player.instanceId) {
                     hitEnemy = true;

                     // If we hit a ship, show some flying particles
                     if (entity is ShipEntity && attackType != Attack.Type.Ice) {
                        switch (attackType) {
                           case Attack.Type.Boulder:
                           case Attack.Type.Mini_Boulder:
                              ExplosionManager.createRockExplosion(entity.transform.position);
                              break;
                           case Attack.Type.Shock_Ball:
                              EffectManager.self.create(Effect.Type.Shock_Collision, entity.transform.position);
                              break;
                           default:
                              ExplosionManager.createExplosion(entity.transform.position);
                              break;
                        }
                        SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
                     }
                     break;
                  }

                  Boss boss = hit.GetComponent<Boss>();
                  if (boss != null) {
                     hitEnemy = true;
                  }
               }
            }
         }

         // If we didn't hit an enemy, then show an effect based on whether we hit land or water
         if (!hitEnemy) {
            // Was there a Land collider where the projectile hit?
            if (Util.hasLandTile(_endPos)) {
               Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), this.transform.position, Quaternion.identity);
               SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
            } else {
               Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), this.transform.position + new Vector3(0f, -.1f), Quaternion.identity);
               SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
            }
         }

         // Detach the Trail Renderer so that it contains to show up a little while longer
         TrailRenderer trail = this.gameObject.GetComponentInChildren<TrailRenderer>();
         trail.transform.parent = null;
         trail.autodestruct = true;

         // Now destroy
         Destroy(this.gameObject);
         return;
      }
   }

   #region Private Variables

   // The creator of this Attack Circle
   protected SeaEntity _creator;

   // The target of the attack, if any
   protected GameObject _targetObject;

   // Our Start Point
   protected Vector2 _startPos;

   // Our End Point
   protected Vector2 _endPos;

   // Our Start Time
   protected float _startTime;

   // Our End Time
   protected float _endTime;

   // Determines the impact level of this projectile
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   #endregion
}
