using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CanvasCurrentSizeScaler : MonoBehaviour {
   #region Public Variables

   // Canvas which will be scaled
   public Canvas canvas;

   #endregion

   private void Awake () {
      _canvasScaler = canvas.GetComponent<CanvasScaler>();
      _aspectRatioFitter = canvas.GetComponent<AspectRatioFitter>();
      _rectTransform = canvas.GetComponent<RectTransform>();
      if (_canvasScaler == null || _aspectRatioFitter == null) {
         this.enabled = false;
      }
   }

   private void Update () {
      float sizeX = _rectTransform.sizeDelta.x * _rectTransform.localScale.x;
      float sizeY = _rectTransform.sizeDelta.y * _rectTransform.localScale.y;
      float ratio = _aspectRatioFitter.aspectRatio;

      if ((Screen.width * ratio < sizeX) || (Screen.height * ratio < sizeY)) {
         if (_aspectRatioFitter.aspectMode == AspectRatioFitter.AspectMode.WidthControlsHeight) {
            _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            _canvasScaler.matchWidthOrHeight = 1;
         } else if (_aspectRatioFitter.aspectMode == AspectRatioFitter.AspectMode.HeightControlsWidth) {
            _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            _canvasScaler.matchWidthOrHeight = 0;
         }
      } else if ((float)Screen.width / (float)Screen.height < _aspectRatioFitter.aspectRatio) {
         _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
         _canvasScaler.matchWidthOrHeight = 0;
      } else if ((float) Screen.height / (float) Screen.width < _aspectRatioFitter.aspectRatio) {
         _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
         _canvasScaler.matchWidthOrHeight = 1;
      }
   }

   #region Private Variables

   // CanvasScaler attached to Canvas - given Canvas must use this component
   private CanvasScaler _canvasScaler;

   // AspectRatioFitter attached to Canvas - given Canvas must use this component
   private AspectRatioFitter _aspectRatioFitter;

   // Cache transform of the canvas
   private RectTransform _rectTransform;

   #endregion
}
