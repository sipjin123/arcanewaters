using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MakeSizeEven : MonoBehaviour {
   #region Public Variables

   #endregion

   void Start () {
      makeSizeEven();
   }

   void OnEnable () {
      makeSizeEven();
   }

   public void makeSizeEven () {
      // Fix the size in a coroutine, so that we can wait until the panel size is calculated
      StartCoroutine(CO_FixSize());
   }

   protected IEnumerator CO_FixSize () {
      LayoutElement layout = GetComponent<LayoutElement>();

      // If we already had one, we need to get rid of it and let our size be recalculated
      if (layout != null) {
         Destroy(layout);
      }

      // Wait 1 frame so that our size is determined
      yield return null;

      RectTransform rectTransform = GetComponent<RectTransform>();
      Vector2 size = rectTransform.sizeDelta;

      // Check if our width is odd
      if (size.x % 2 == 1) {
         size.x += 1;
      }

      // Check if our height is odd
      if (size.y % 2 == 1) {
         size.y += 1;
      }

      // Create a new layout component
      layout = this.gameObject.AddComponent<LayoutElement>();

      // Apply the fixed size
      layout.preferredWidth = size.x;
      layout.preferredHeight = size.y;

      // Now we can remove the component
      yield return null;

      Destroy(layout);
   }

   #region Private Variables

   #endregion
}
