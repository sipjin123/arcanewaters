using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaPlayerDashEffect : ClientMonoBehaviour
{
   #region Public Variables

   // Possible particles that can be used by effects
   public Sprite[] possibleParticles = new Sprite[0];

   // Parent transform that holds the charge effect
   public Transform chargeEffectParent;

   // How many particles are in the charge animation
   public int chargeParticleCount = 5;

   // Charge animation interval
   public float chargeAnimationInterval = 1f;

   // Range of charge particles in an interval
   public int minChargeParticles = 5;
   public int maxChargeParticles = 10;

   // Distance of charge particles from the center of the ship
   public float chargeParticleDistance = 0.15f;

   // Template of the charge particle
   public SimpleAnimation chargeParticleTemplate;

   #endregion

   private void Start () {
      _chargeEffectActive = false;
      chargeEffectParent.gameObject.SetActive(false);

      chargeParticleTemplate.gameObject.SetActive(false);
      // Create a bunch of charge particles
      for (int i = 0; i < maxChargeParticles; i++) {
         SimpleAnimation chargeParticle = Instantiate(chargeParticleTemplate, chargeEffectParent);
         chargeParticle.gameObject.SetActive(true);
         _chargeParticles.Add(chargeParticle);
      }
   }

   private void Update () {
      if (_chargeEffectActive) {
         if (Time.time - _lastChargeAnimationInterval > chargeAnimationInterval) {
            playChargeAnimationInterval();
         }
      }
   }

   private void playChargeAnimationInterval () {
      randomizeChargeParticles();

      foreach (SimpleAnimation a in _chargeParticles) {
         a.resetAnimation();
      }

      _lastChargeAnimationInterval = Time.time;
   }

   private void randomizeChargeParticles () {
      // Activate a random amount of charge particles
      int count = Random.Range(minChargeParticles, maxChargeParticles + 1);

      // Make it so they space out in a circle without big gaps, but slightly randomize
      float angleDelta = 360f / count;
      float initialOffset = Random.Range(0, angleDelta);

      for (int i = 0; i < _chargeParticles.Count; i++) {
         _chargeParticles[i].gameObject.SetActive(i < count);

         if (i < count) {
            float angle = initialOffset + i * angleDelta + Random.Range(angleDelta * 0.1f, angleDelta * 0.9f);
            _chargeParticles[i].transform.rotation = Quaternion.Euler(0, 0, angle);

            // For each particle, assign a random sprite
            SpriteRenderer sr = _chargeParticles[i].GetComponent<SpriteRenderer>();
            sr.sprite = possibleParticles[Random.Range(0, possibleParticles.Length)];
            sr.flipX = Random.value > 0.5f;

            // Pull it away from the center
            _chargeParticles[i].transform.localPosition = _chargeParticles[i].transform.up.normalized * chargeParticleDistance;
         }
      }
   }

   public void setChargeEffectActive (bool active) {
      if (active == _chargeEffectActive) {
         return;
      }

      _chargeEffectActive = active;
      chargeEffectParent.gameObject.SetActive(active);

      if (active) {
         playChargeAnimationInterval();
      }
   }

   #region Private Variables

   // Is the charge effect active
   private bool _chargeEffectActive = false;

   // When was the last time we played charge animation interval
   private float _lastChargeAnimationInterval;

   // All the charge particles
   private List<SimpleAnimation> _chargeParticles = new List<SimpleAnimation>();

   #endregion
}
