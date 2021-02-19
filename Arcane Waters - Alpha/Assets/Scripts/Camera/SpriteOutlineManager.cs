using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class SpriteOutlineManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SpriteOutlineManager self;

   // The prefab we use for outline quads
   public SpriteOutlineRenderer outlineQuadPrefab;
   
   #endregion

   private void Awake () {
      self = this;

      _quadPool = new Pool<SpriteOutlineRenderer>(outlineQuadPrefab);
   }

   public void addOutlinedSprite (SpriteOutline shaderSpriteOutline) {
      SpriteOutlineRenderer outlineQuad = _quadPool.get();
      shaderSpriteOutline.outlineRenderer = outlineQuad;

      outlineQuad.setOutlinedSprite(shaderSpriteOutline);
   }

   public void removeOutlinedSprite (SpriteOutline shaderSpriteOutline) {
      if (shaderSpriteOutline == null || shaderSpriteOutline.outlineRenderer == null) {
         return;
      }

      shaderSpriteOutline.outlineRenderer.gameObject.SetActive(false);
      shaderSpriteOutline.outlineRenderer = null;
   }

   #region Private Variables

   // A pool of all the quads we already instantiated
   private Pool<SpriteOutlineRenderer> _quadPool;

   #endregion
}
