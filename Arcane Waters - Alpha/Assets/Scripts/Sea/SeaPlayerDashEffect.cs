using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaPlayerDashEffect : ClientMonoBehaviour
{
   #region Public Variables

   // Possible particles that can be used by effects
   public Texture2D[] possibleParticles = new Texture2D[0];

   // Parent transform that holds the charge effect
   public Transform chargeEffectParent;

   // Range of charge particles in an interval
   public int minChargeParticles = 5;
   public int maxChargeParticles = 10;

   // Range of alpha of charge particles
   public float minChargeParticlesScale = 1f;
   public float maxChargeParticlesScale = 1.25f;

   // Range of charge particles in an interval
   public float minChargeParticlesAlpha = 0.25f;
   public float maxChargeParticlesAlpha = 1f;

   // Distance of charge particles from the center of the ship
   public float chargeParticleDistance = 0.0f;

   // Template of the charge particle
   public SpriteRenderer chargeParticleTemplate;

   #endregion

   private void Start () {
      chargeEffectParent.gameObject.SetActive(false);

      chargeParticleTemplate.gameObject.SetActive(false);
      // Create a bunch of charge particles
      for (int i = 0; i < maxChargeParticles; i++) {
         SpriteRenderer chargeParticle = Instantiate(chargeParticleTemplate, chargeEffectParent);
         chargeParticle.gameObject.SetActive(true);
         _chargeParticles.Add(chargeParticle);
         _particleTextureIndexes.Add(0);
      }

      // Cache particle frames
      foreach (Texture2D t in possibleParticles) {
         _possibleParticlesAnims.Add(ImageManager.getSprites(t));
      }
   }

   private void playChargeAnimation (float time) {
      for (int i = 0; i < _chargeParticles.Count; i++) {
         if (_chargeParticles[i].gameObject.activeSelf) {
            Sprite[] frames = _possibleParticlesAnims[_particleTextureIndexes[i]];
            _chargeParticles[i].sprite = frames[Mathf.Clamp((int) (time * frames.Length), 0, frames.Length - 1)];
         }
      }
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
            _chargeParticles[i].enabled = true;

            // For each particle, assign a random sprite
            SpriteRenderer sr = _chargeParticles[i].GetComponent<SpriteRenderer>();
            _particleTextureIndexes[i] = Random.Range(0, possibleParticles.Length);
            sr.flipX = Random.value > 0.5f;

            // Pull it away from the center
            _chargeParticles[i].transform.localPosition = _chargeParticles[i].transform.up.normalized * chargeParticleDistance;

            _chargeParticles[i].transform.localScale = Vector3.one * Random.Range(minChargeParticlesScale, maxChargeParticlesScale);
            Util.setAlpha(_chargeParticles[i], Random.Range(minChargeParticlesAlpha, maxChargeParticlesAlpha));
         }
      }
   }

   public void setChargeEffect (float amount) {
      if (amount >= 1f || amount <= 0f) {
         chargeEffectParent.gameObject.SetActive(false);
         randomizeChargeParticles();
      } else {
         chargeEffectParent.gameObject.SetActive(true);
         playChargeAnimation(amount);
      }
   }

   public void playDashEffect (Vector2 boostDirection) {
      _dashEffect.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(boostDirection.y, boostDirection.x) * Mathf.Rad2Deg);
      _dashEffect.Play();
   }

   #region Private Variables

   // All the charge particles
   private List<SpriteRenderer> _chargeParticles = new List<SpriteRenderer>();

   // Index of texture we are using for the corresponding charge particle
   private List<int> _particleTextureIndexes = new List<int>();

   // Particles with all their frames
   private List<Sprite[]> _possibleParticlesAnims = new List<Sprite[]>();

   // Reference to the dash wind gust effect
   [SerializeField]
   private ParticleSystem _dashEffect = default;

   #endregion
}
