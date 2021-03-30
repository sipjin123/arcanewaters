using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SpriteGroup : ClientMonoBehaviour {
   #region Public Variables

   // The alpha we want for this group of Sprites
   public float alpha = 1f;

   #endregion

   void Start () {
      // Look up all of our sprites
      foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
         if (renderer != null) {
            _sprites.Add(renderer);
         }
      }

      updateAlpha(false);
   }

   void Update () {
      updateAlpha(true);
   }

   private void updateAlpha (bool checkValueChange) {
      // Clamp our alpha
      alpha = Mathf.Clamp(alpha, 0f, 1f);

      // If the alpha setting has changed, apply it to all sprites
      if (alpha != _previousAlpha || !checkValueChange) {
         foreach (SpriteRenderer renderer in _sprites) {
            Util.setAlpha(renderer, alpha);
         }
      }

      // Note the alpha for the next frame
      _previousAlpha = alpha;
   }

   #region Private Variables

   // The Sprites that we manage
   protected List<SpriteRenderer> _sprites = new List<SpriteRenderer>();

   // The alpha in the previous frame
   protected float _previousAlpha;

   #endregion
}
