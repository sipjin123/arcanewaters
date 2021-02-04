using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Rendering;

public class HatLayer : SpriteLayer
{
   #region Public Variables

   // The equipment id of the hat
   public int equipmentId = 0;

   #endregion

   public void setType (Gender.Type gender, int newType, bool immediate = true) {
      _type = newType;

      // Update our Animated Sprite
      string path = (newType == 0) ? "Empty_Layer" : "Hats/" + "hat_" + (int) newType;
      Texture2D result = ImageManager.getTexture(path);

      if (immediate) {
         GetComponent<SpriteSwap>().newTexture = result;
      } else {
         StartCoroutine(CO_SwapTexture(result));
      }

      _material = getMaterial();
            
      _material.SetInt("_StencilOp", newType != 0 ? (int) StencilOp.Replace : (int) StencilOp.Keep);
      _material.SetInt("_StencilComp", (int) CompareFunction.Always);
      _material.SetInt("_Stencil", HAT_STENCIL_ID);
      _material.SetFloat("_UseHatStencil", 1);

      if (newType != 0) {
         _material.EnableKeyword("USE_HAT_STENCIL");
      } else {
         _material.DisableKeyword("USE_HAT_STENCIL");
      }
   }

   public int getType () {
      return _type;
   }

   #region Private Variables

   // Our current type
   protected int _type;

   // The material used by this layer
   protected Material _material;

   #endregion
}
