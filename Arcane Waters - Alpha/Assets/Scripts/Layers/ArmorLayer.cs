using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ArmorLayer : SpriteLayer {
   #region Public Variables

   // The equipment id of the armor
   public int equipmentId = 0;

   #endregion

   public void setType (Gender.Type gender, int newType, bool immediate = true) {
      _type = newType;

      // Update our Animated Sprite
      string path = "Armor/" + gender + "/" + gender + "_armor_" + (int) newType;
      Texture2D result = newType == 0 ? ImageManager.self.blankTexture : ImageManager.getTexture(path);

      if (immediate) {
         setTexture(result);
      } else {
         StartCoroutine(CO_SwapTexture(result));
      }
   }

   public int getType () {
      return _type;
   }

   #region Clipmask

   private Texture2D getDefaultClipmask () {
      return ImageManager.getTexture($"Masks/Sitting/sitting_clipmask");
   }

   public void toggleClipmask (bool enable = true) {
      // Skip update for server in batch mode, or if the state of the clipmask hasn't changed
      if (Util.isBatch() || getMaterial() == null || _isClipmaskEnabled == enable) {
         return;
      }

      if (_lastClipTexture == null) {
         Texture2D defaultClipmask = getDefaultClipmask();

         if (defaultClipmask == null || defaultClipmask == ImageManager.self.blankTexture) {
            return;
         }

         overrideClipmask(defaultClipmask);
      }

      getMaterial().SetFloat("_EnableClipping", enable ? 1.0f : 0.0f);
      _isClipmaskEnabled = enable;
   }

   public bool isClipmaskEnabled () {
      return _isClipmaskEnabled;
   }

   public void overrideClipmask (Texture2D texture) {
      if (Util.isBatch() || getMaterial() == null || texture == _lastClipTexture) {
         return;
      }

      if (texture != null) {
         getMaterial().SetTexture("_ClipTex", texture);
      }

      _lastClipTexture = texture;
   }

   #endregion

   #region Private Variables

   // Our current type
   protected int _type;

   // Current clip state
   private bool _isClipmaskEnabled = false;

   // Reference to the current clip texture
   private Texture2D _lastClipTexture;

   #endregion
}
