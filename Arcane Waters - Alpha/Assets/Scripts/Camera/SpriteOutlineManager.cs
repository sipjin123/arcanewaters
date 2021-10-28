using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class SpriteOutlineManager : GenericGameManager {
   #region Public Variables

   // Self
   public static SpriteOutlineManager self;

   // The prefab we use for outline quads
   public SpriteOutlineRenderer outlineQuadPrefab;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      _renderersPool = new Pool<SpriteOutlineRenderer>(outlineQuadPrefab);
   }

   private bool registerOutline (SpriteOutline outline, out SpriteOutlineRenderer renderer) {
      if (outline == null) {
         renderer = null;
         return false;
      }

      if (_outlineRegistry.ContainsKey(outline)) {
         renderer = _outlineRegistry[outline];
         return true;
      }

      renderer = _renderersPool.pop();
      renderer.reset();
      _outlineRegistry.Add(outline, renderer);
      return true;
   }

   private bool unregisterOutline (SpriteOutline outline) {
      if (outline == null || !_outlineRegistry.ContainsKey(outline)) {
         return true;
      }

      SpriteOutlineRenderer renderer = _outlineRegistry[outline];
      renderer.reset();
      _outlineRegistry.Remove(outline);
      _renderersPool.push(renderer);
      return true;
   }

   public void onOutlineWillRenderObject (SpriteOutline outline, bool show) {
      if (show) {
         if (registerOutline(outline, out SpriteOutlineRenderer renderer)) {
            renderer.updateDepth(outline.transform.position.z);
            renderer.updateColor(outline.getColor());
            renderer.updateRenderBuffer(outline.getRenderers());
         }
      } else {
         unregisterOutline(outline);
      }
   }

   public void onOutlineDestroyed(SpriteOutline outline) {
      unregisterOutline(outline);
   }

   #region Private Variables

   // A pool of all the quads we already instantiated
   private Pool<SpriteOutlineRenderer> _renderersPool;

   // Sprite Outline Registry
   private Dictionary<SpriteOutline, SpriteOutlineRenderer> _outlineRegistry = new Dictionary<SpriteOutline, SpriteOutlineRenderer>();

   #endregion
}
