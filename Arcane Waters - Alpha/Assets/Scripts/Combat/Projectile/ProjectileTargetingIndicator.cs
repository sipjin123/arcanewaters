using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class ProjectileTargetingIndicator : ClientMonoBehaviour {
   #region Public Variables

   // A reference to the game object containing the targeting circle that will show where the projectile is going to land
   public GameObject targetCircle;

   // A reference to the game object containing the circle to be displayed when the projectile impacts
   public GameObject impactCircle;

   // A reference to the sprite renderer for the target circle
   public SpriteRenderer targetCircleRenderer;

   // A reference to the sprite renderer for the impact circle
   public SpriteRenderer impactCircleRenderer;

   #endregion

   protected override void Awake () {
      base.Awake();

      _spriteGroup = GetComponent<SpriteGroup>();
   }

   public void init (float lifetime, float scaleModifier = 1.0f) {
      _creationTime = (float)NetworkTime.time;
      _lifetime = lifetime;

      float startScale = 1.25f * scaleModifier;
      float endScale = 0.75f * scaleModifier;

      // Scale the circle down towards the impact point, then, upon impact, enable the second circle, scale it up, and fade it out.
      targetCircle.transform.localScale = Vector3.one * startScale;
      targetCircle.transform.DOScale(endScale, _lifetime).SetEase(Ease.OutCubic).OnComplete(() => {
         targetCircle.gameObject.SetActive(false);
         impactCircle.gameObject.SetActive(true);
         impactCircle.transform.localScale = Vector3.one * 0.01f;
         impactCircleRenderer.DOColor(new Color(1.0f, 1.0f, 1.0f, 0.0f), 0.3f);
         impactCircle.transform.DOScale(1.25f, 0.3f).OnComplete(() => {
            Destroy(this.gameObject);
         });
      });
   }

   private void Update () {
      if (targetCircle.gameObject.activeInHierarchy) {
         float lifetimeQuotient = ((float) NetworkTime.time - _creationTime) / _lifetime;
         targetCircleRenderer.color = ColorCurveReferences.self.projectileTargetingIndicatorColor.Evaluate(lifetimeQuotient);
      }
   }

   #region Private Variables

   // A reference to the sprite group controlling the alpha for this object's sprites
   private SpriteGroup _spriteGroup;

   // When this indicator was created
   private float _creationTime;

   // How long this indicator will be alive for
   private float _lifetime;

   #endregion
}
