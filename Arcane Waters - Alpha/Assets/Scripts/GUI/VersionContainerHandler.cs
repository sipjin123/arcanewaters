﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VersionContainerHandler : MonoBehaviour
{
   #region Public Variables

   // Canvas Group of the minimap
   public CanvasGroup _miniMapCanvasGroup;

   // Rect Transform of the minimap
   public RectTransform mapRectTransform;

   // Rect Transform of the version number container
   public RectTransform verRectTransform;

   // Padding from the edge of the map
   public const int PADDING = 3;

   #endregion

   void Update () {
      // Align version number container to the left side of the minimap
      if (_miniMapCanvasGroup != null) {
         if (_miniMapCanvasGroup.alpha == 0) {
            verRectTransform.anchoredPosition = Vector2.zero;
         } else {
            verRectTransform.anchoredPosition = new Vector2(-mapRectTransform.rect.width * mapRectTransform.localScale.x - PADDING , 0);
         }
      }
   }

   #region Private Variables

   #endregion
}