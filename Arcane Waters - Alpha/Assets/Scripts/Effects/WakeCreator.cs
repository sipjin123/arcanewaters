using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WakeCreator : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating the wake
   public WakeParticle wakePrefab;

   #endregion

   void Start () {
      // Look up references
      _entity = GetComponent<NetEntity>();

      // Create particles every now and then
      InvokeRepeating("createParticles", 0f, .08f);
   }

   protected void createParticles () {
      // Don't do it if we're not moving
      if (_entity.getRigidbody().velocity.magnitude < .2f) {
         return;
      }

      // Create a particle
      Vector3 pos = this.transform.position;
      pos.y -= .06f;
      pos.z = 100.45f;
      WakeParticle particle = Instantiate(wakePrefab, pos, Quaternion.identity);
      particle.transform.SetParent(EffectManager.self.transform, true);
   }

   #region Private Variables

   // Our associated entity
   protected NetEntity _entity;

   #endregion
}
