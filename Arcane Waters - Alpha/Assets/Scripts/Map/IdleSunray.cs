using UnityEngine;
using System.Collections;

public class IdleSunray : MonoBehaviour {
   #region Public Variables

   // Curve that controls the opacity during the animation. Max value should be 1, it will be multiplied by maxOpacity+-opacityDelta
   public AnimationCurve opacityCurve;
   public float maxOpacity = 0.5f, opacityDelta = .1f;

   // Duration of the blinking animation. 
   // It's calculated by getting a random value between -delta and +delta and adding it to the base value
   public float durationBase, durationDelta;

   // The Sprite renderer
   SpriteRenderer spriteRenderer;

   #endregion

   private void Awake () {
      spriteRenderer = GetComponent<SpriteRenderer>();
      StartCoroutine(initAnimation());
   }

   private IEnumerator initAnimation () {
      // Animate forever
      while (true) {
         // Calc the animation duration
         float animationDuration = durationBase + Random.Range(-durationDelta, durationDelta);

         // Apply the delta to the max opacity
         float targetOpacity = maxOpacity + Random.Range(-opacityDelta, opacityDelta);

         // Perform the animation
         for (float timer = 0; timer < animationDuration; timer += Time.deltaTime) {
            // If it's not below the top of the screen, it proceeds the animation. If else it dissapears
            // Evaluated opacity based on opacityCurve and timer values
            Util.setAlpha(spriteRenderer, opacityCurve.Evaluate(timer / animationDuration) * targetOpacity);
            yield return new WaitForEndOfFrame();
         }

         // Fade ray out
         while (spriteRenderer.color.a > 0.02f) {
            Util.setAlpha(spriteRenderer, Mathf.Lerp(spriteRenderer.color.a, 0, Time.deltaTime * 3f));
            yield return new WaitForEndOfFrame();
         }

         yield return new WaitForEndOfFrame();
      }
   }

   #region Private Variables

   #endregion
}
