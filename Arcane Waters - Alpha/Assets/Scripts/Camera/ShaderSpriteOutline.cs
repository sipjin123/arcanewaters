﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class ShaderSpriteOutline : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      // Make sure things are drawn in the right Z order (back to front, just like Unity does)
      _renderers = _renderers.OrderByDescending(x => x.transform.position.z).ToArray();   
   }

   private void OnWillRenderObject () {
      if (_isVisible) {
         PostSpriteOutline.self.onWillRenderObject(this);
      }
   }

   private void Update () {
      // Ensure the outline is being drawn
      if (_isVisible) {
         PostSpriteOutline.self.setOutlinedSprite(this);         
      }

      _wasVisible = _isVisible;
   }

   public void setVisibility (bool isVisible) {
      // Only change visibility if it needs to be changed
      if (isVisible == _isVisible) {
         return;
      }

      if (isVisible) {
         PostSpriteOutline.self.setOutlinedSprite(this);         
      } else {
         PostSpriteOutline.self.removeOutlinedSprite(this);
      }

      _isVisible = isVisible;
   }

   public SpriteRenderer[] getRenderers () {
      return _renderers;
   }

   #region Private Variables

   // The renderers of this sprite
   [SerializeField]
   private SpriteRenderer[] _renderers;

   // Whether this outline is visible
   private bool _isVisible;

   // Whether this outline visible last frame
   private bool _wasVisible;

   #endregion
}