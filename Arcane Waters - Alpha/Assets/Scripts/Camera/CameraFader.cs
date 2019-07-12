using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CameraFader : MonoBehaviour {
   #region Public Variables

   #endregion

   void Start () {
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   public void fadeInAndOut () {
      StopAllCoroutines();
      StartCoroutine(CO_FadeInAndOut());
   }

   protected IEnumerator CO_FadeInAndOut () {
      _canvasGroup.alpha = 1f;

      // Fade out
      /*while (_canvasGroup.alpha < 1f) {
         yield return new WaitForSeconds(.01f);
         _canvasGroup.alpha += .1f;
      }*/

      // Fade in
      while (_canvasGroup.alpha > 0f) {
         yield return new WaitForSeconds(.01f);
         _canvasGroup.alpha -= .1f;
      }

      _canvasGroup.alpha = 0f;
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
