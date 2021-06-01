using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(RawImage))]
public class LocalPlayerIndicator : MonoBehaviour
{
   #region Public Variables

   #endregion

   public void toggle (bool show) {

      bool hasRawImage = TryGetComponent(out RawImage indicatorImage);
      if (!hasRawImage) return;
      indicatorImage.enabled = show;

   }

   #region Private Variables

   #endregion
}
