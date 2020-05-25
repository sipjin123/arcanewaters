﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PaletteToolColorUnderCursor : MonoBehaviour {
   #region Public Variables

   // Singleton reference
   public static PaletteToolColorUnderCursor self;

   // Reference to manager
   public PaletteToolManager paletteToolManager;

   #endregion

   private void Awake () {
      if (self == null) {
         self = this;
      } else {
         Destroy(this);
      }
   }

   public void activate (Color startingColor) {
      _isActive = true;
      _colorBeforeChanges = startingColor;
   }

   private void OnRenderImage (RenderTexture source, RenderTexture destination) {
      if (_isActive) {
         Texture2D tex = new Texture2D(source.width, source.height);
         tex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
         tex.Apply();

         Color color = tex.GetPixel((int) Input.mousePosition.x, (int) Input.mousePosition.y);
         Destroy(tex);

         paletteToolManager.updatePickingColorFromSprite(color);

         if (Input.GetKey(KeyCode.Mouse0)) {
            paletteToolManager.finalizePickingColorFromSprite();
            _isActive = false;
         } else if (Input.GetKey(KeyCode.Mouse1)) {
            paletteToolManager.finalizePickingColorFromSprite(true, _colorBeforeChanges);
            _isActive = false;
         }
      }

      Graphics.Blit(source, destination);
   }

   #region Private Variables
   
   // Is currently active - can pick new color from preview
   private bool _isActive = false;

   // Color before any color picking changes were applied
   private Color _colorBeforeChanges;

   #endregion
}