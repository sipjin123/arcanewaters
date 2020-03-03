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
         yield return new WaitForSeconds(anim.loopDelay);
      }
   }

   #region Private Variables

   #endregion
}
