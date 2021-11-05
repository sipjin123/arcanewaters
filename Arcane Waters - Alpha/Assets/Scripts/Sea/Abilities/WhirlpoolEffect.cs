using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class WhirlpoolEffect : MonoBehaviour {
   #region Public Variables

   // A reference to the gameobject that contains the mask for the whirlpool
   public GameObject maskObject;

   // A reference to the renderer that displays the whirlpool effect
   public SpriteRenderer spriteRenderer;

   #endregion

   public void init (float effectRadius) {
      float scaleFactor = effectRadius / DEFAULT_RADIUS;
      
      // If we are scaling the effect up, just scale up the object
      if (scaleFactor >= 1.0f) {
         transform.localScale = Vector3.one * scaleFactor;

      // If we are scaling the effect down, just scale down the mask objects
      } else {
         maskObject.transform.localScale = Vector3.one * scaleFactor;
      }

      spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
      spriteRenderer.DOColor(new Color(1.0f, 1.0f, 1.0f, 1.0f), FADE_DURATION);

      StartCoroutine(CO_DestroyEffect(EFFECT_DURATION));
   }

   private IEnumerator CO_DestroyEffect (float afterSeconds) {
      yield return new WaitForSeconds(afterSeconds);

      spriteRenderer.DOColor(new Color(1.0f, 1.0f, 1.0f, 0.0f), FADE_DURATION).OnComplete(() => {
         Destroy(this.gameObject);
      });
   }

   #region Private Variables

   // The radius of the whirlpool sprite
   private const float DEFAULT_RADIUS = 0.62f;

   // How long the whirlpool effect takes to fade in or out
   private const float FADE_DURATION = 0.125f;

   // How long the whirlpool effect lasts for
   private const float EFFECT_DURATION = 0.5f;

   #endregion
}
