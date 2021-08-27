using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(RectTransform))]
public class FixedScaler : MonoBehaviour {
   #region Public Variables

   // The scale
   public Vector3 fixedScale = Vector3.one;

   #endregion

   public void Start () {
      _rectTransform = GetComponent<RectTransform>();
   }

   public void Update () {
      _rectTransform.localScale = fixedScale;
   }

   #region Private Variables

   // Reference to the managed RectTransform instance
   private RectTransform _rectTransform;

   #endregion
}
