using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InteractableBox : InteractableObjEntity {
   #region Public Variables

   // The pair for directional objects
   public List<GenericDirectionObjPair> directionalObjPair;

   // The dust particle effect for movement
   public GameObject dustParticle;

   // If force is being applied
   public bool isForceApplied;

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   public override void interactObject (Vector2 dir, float overrideForce = 0) {
      base.interactObject(dir, overrideForce);
      
      Rpc_PlayInteractSound();
   }

   public override void localInteractTrigger (Vector2 dir, float overrideForce = 0) {
      base.localInteractTrigger(dir, overrideForce);
      if (EffectManager.self.interactCrateEffects != null) {
         GameObject interactVfx = Instantiate(EffectManager.self.interactCrateEffects, transform);
         interactVfx.transform.position = transform.position;
         Destroy(interactVfx, 1);
      } else {
         D.debug("Interact vfx is missing!");
      }
      _startTime = NetworkTime.time;

      dustParticle.SetActive(true);
      isForceApplied = true;
      if (dir.x < 0) {
         directionalObjPair.Find(_ => _.direction == Direction.East).obj.SetActive(true);
      } else {
         directionalObjPair.Find(_ => _.direction == Direction.West).obj.SetActive(true);
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      if (Util.isBatch()) {
         return;
      }

      if (isForceApplied) {
         if (_rigidBody.velocity.magnitude < .05f && (NetworkTime.time - _startTime) > .2f) {
            isForceApplied = false;
            dustParticle.SetActive(false);
            foreach (GenericDirectionObjPair data in directionalObjPair) {
               data.obj.SetActive(false);
            }
         }
      }
   }

   [ClientRpc]
   public void Rpc_PlayInteractSound () {
      SoundEffectManager.self.playAttachedSfx(SoundEffectManager.INTERACTABLE_BOX, this.transform, this._rigidBody);
   }

   #region Private Variables

   #endregion
}
