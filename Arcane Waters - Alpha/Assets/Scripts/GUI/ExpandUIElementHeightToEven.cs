using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ExpandUIElementHeightToEven : MonoBehaviour {
   #region Public Variables

   // Element which is parent and can control alpha
   public CanvasGroup canvasGroup;

   #endregion

   private void OnEnable () {
      StartCoroutine(CO_waitAfterEnabling());
   }

   private IEnumerator CO_waitAfterEnabling () {
      // Wait single frame to get automatic height
      canvasGroup.alpha = 0;
      yield return new WaitForEndOfFrame();

      // Continue executing code
      ContentSizeFitter contentFitter = GetComponent<ContentSizeFitter>();
      if (contentFitter) {
         _fitMode = contentFitter.verticalFit;
         contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      }

      float sizeY = GetComponent<RectTransform>().sizeDelta.y;
      int intSizeY = Mathf.FloorToInt(sizeY);
      if (sizeY != intSizeY || sizeY % 2 == 1) {
         GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, intSizeY + (intSizeY % 2));
      }
      canvasGroup.alpha = 1;
   }

   private void OnDisable () {
      ContentSizeFitter contentFitter = GetComponent<ContentSizeFitter>();
      if (contentFitter) {
         contentFitter.verticalFit = _fitMode;
      }
   }

   #region Private Variables

   // Saved fit mode to revert after disabling object
   private ContentSizeFitter.FitMode _fitMode;

   #endregion
}
