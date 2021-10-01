using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class PowerupOrb : ClientMonoBehaviour {
   #region Public Variables

   // A value controlling how far around the SeaEntity this powerup orb is currently rotated
   public float rotationValue;

   // A reference to the particle system that displays the trail for this orb
   public ParticleSystem trailParticles;

   // What type of powerup this orb represents
   public Powerup.Type powerupType;

   #endregion

   public void init (Powerup.Type powerupType, Transform parentTransform, bool isNew) {
      if (isNew) {
         ParticleSystem.MainModule mainModule = trailParticles.main;
         mainModule.customSimulationSpace = parentTransform;

         // Scale up the orb as it is created
         transform.localScale = Vector3.one * 0.1f;
         transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutElastic);
      }
      
      this.powerupType = powerupType;

      // Apply different orb & trail sprites based on powerup type
   }

   #region Private Variables
      
   #endregion
}
