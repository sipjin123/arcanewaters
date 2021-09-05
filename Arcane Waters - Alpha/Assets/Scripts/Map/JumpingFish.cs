using UnityEngine;
using System.Collections;

public class JumpingFish : ClientMonoBehaviour
{
   #region Public Variables

   // Minimum delay between jump animations
   public float minAnimDelta = 1f;

   // Maximum delay between jump animations
   public float maxAnimDelta = 1f;

   #endregion

   private IEnumerator Start () {
      SimpleAnimation anim = GetComponent<SimpleAnimation>();
      while (true) {
         anim.loopDelay = Random.Range(minAnimDelta, maxAnimDelta);
         yield return new WaitUntil(() => !anim.isWaitingForLoop());
         // Play fish sound only if player is in game
         if (Global.player != null) {
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.FISH_SURFACING, transform);
            //SoundManager.playClipAtPoint(SoundManager.Type.Fish_Jump, transform.position);
         }
         yield return new WaitForSeconds(anim.loopDelay);
      }
   }

   #region Private Variables

   #endregion
}
