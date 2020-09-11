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

   // Reference to the trail of the projectile (as trailrenderer)
   public TrailRenderer trailRenderer;

   // Reference to the trail of the projectile (as particle system)
   public ParticleSystem particleSystemTrail;

   // Projectile Sprite
   public SpriteRenderer[] staticSprites;

   // Animated sprite
   public SpriteRenderer[] animatedSprites;

   #endregion

   public void init (float startTime, float endTime, Vector2 startPos, Vector2 endPos, SeaEntity creator, int abilityId, GameObject targetObj = null) {
      if (targetObj != null) {
         _targetObject = targetObj;
      }

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

      trailRenderer.enabled = false;
      particleSystemTrail.Stop();

      if (attackType == Attack.Type.Cannon) {
         particleSystemTrail.Play();
      } else if (shipAbilitydata.hasTrail) {
         trailRenderer.enabled = true;
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

      // If we've been alive long enough, destroy ourself
      if (TimeManager.self.getSyncedTime() > this._endTime) {
         // Detach the trails so that they continue to show up a little while longer
         if (particleSystemTrail != null && particleSystemTrail.isPlaying) {
            particleSystemTrail.transform.parent = null;
            particleSystemTrail.Stop();
         }

         if (trailRenderer != null && trailRenderer.enabled) {
            trailRenderer.transform.parent = null;
            trailRenderer.autodestruct = true;
         }

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

   #endregion
}
