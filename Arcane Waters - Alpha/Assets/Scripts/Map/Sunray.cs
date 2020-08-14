using System.Collections;
using UnityEngine;

public class Sunray : ClientMonoBehaviour
{
   #region Public Variables

   // Curve that controls the opacity during the animation. Max value should be 1, it will be multiplied by maxOpacity+-opacityDelta
   public AnimationCurve opacityCurve;
   public float maxOpacity = 0.5f, opacityDelta = .1f;

   // Duration of the blinking animation. 
   // It's calculated by getting a random value between -delta and +delta and adding it to the base value
   public float durationBase, durationDelta;

   // The distance from the top where it starts fading
   public float distaceFromTheTop = 0.96f;

   // Z position of the ray
   public float zPos;

   #endregion

   private IEnumerator Start () {
      SpriteRenderer rend = GetComponent<SpriteRenderer>();
      Util.setZ(transform, zPos);

      // Animate forever
      while (true) {
         // Calc the animation duration
         float animationDuration = durationBase + Random.Range(-durationDelta, durationDelta);

         // Apply the delta to the max opacity
         float targetOpacity = maxOpacity + Random.Range(-opacityDelta, opacityDelta);

         // Perform the animation
         for (float timer = 0; timer < animationDuration; timer += Time.deltaTime) {
            // If it's not below the top of the screen, it proceeds the animation. If else it dissapears
            if (Global.player.transform.position.y + distaceFromTheTop < transform.position.y) {
               // Evaluated opacity based on opacityCurve and timer values
               Util.setAlpha(rend, opacityCurve.Evaluate(timer / animationDuration) * targetOpacity);
            } else {
               break;
            }
            yield return new WaitForEndOfFrame();
         }

         // Fade ray out
         while (rend.color.a > 0.02f) {
            Util.setAlpha(rend, Mathf.Lerp(rend.color.a, 0, Time.deltaTime * 3f));
            yield return new WaitForEndOfFrame();
         }

         yield return new WaitForEndOfFrame();
      }
   }

   #region Private variables

   #endregion
}