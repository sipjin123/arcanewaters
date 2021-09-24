using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class ProjectileTargetingIndicator : ClientMonoBehaviour {
   #region Public Variables

   // A reference to the game object containing the inner circle of the projectile indicator, that wil expand
   public GameObject innerCircle;

   #endregion

   protected override void Awake () {
      base.Awake();

      _spriteGroup = GetComponent<SpriteGroup>();
   }

   public void init (float lifetime) {
      // Scale up the inner circle, then fade out the object and destroy it
      innerCircle.transform.DOScale(1.0f, lifetime)
         .OnComplete(() => DOTween.To(() => _spriteGroup.alpha, (x) => _spriteGroup.alpha = x, 0.0f, 0.25f).SetEase(Ease.OutCubic)
         .OnComplete(() => Destroy(gameObject))
         );
   }

   #region Private Variables

   // A reference to the sprite group controlling the alpha for this object's sprites
   private SpriteGroup _spriteGroup;

   #endregion
}
