using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class RandomTexture : MonoBehaviour {
   #region Public Variables

   // The textures we can choose from
   public List<Texture2D> textures;

   // The colors we can choose from
   public List<Color> colors;

   #endregion

   void Start () {
      NetEntity player = GetComponentInParent<NetEntity>();

      // Swap in a texture
      SpriteSwap swapper = GetComponent<SpriteSwap>();
      swapper.newTexture = textures.ChooseRandom(player.userId);

      if (colors.Count > 0) {
         SpriteRenderer renderer = GetComponent<SpriteRenderer>();
         renderer.material.SetColor("_NewColor", colors.ChooseRandom(player.userId));
         renderer.material.SetColor("_NewColor2", colors.ChooseRandom(player.userId));
      }
   }

   #region Private Variables

   #endregion
}
