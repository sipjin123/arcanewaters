using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteSwapper : ClientMonoBehaviour {
   public Texture2D newTexture = null;

   protected override void Awake () {
      base.Awake();

      // Look up components
      _renderer = GetComponent<SpriteRenderer>();
      _image = GetComponent<Image>();
      RecoloredSprite recoloredSprite = GetComponent<RecoloredSprite>();

      // If we're working with GUI images, we need to manually create material instances
      if (_image != null && recoloredSprite != null) {
         recoloredSprite.setNewMaterial(_image.material);
      }
   }

   public void Update () {
      Material material = getMaterial();

      if (newTexture != material.mainTexture) {
         material.EnableKeyword("SWAP_TEXTURE");
         material.SetTexture("_MainTex2", newTexture);

         if (_renderer != null) {
            _renderer.size = new Vector2(.32f, .32f);

            if (newTexture.width == 80) {
               _renderer.size = new Vector2(.2f, .2f);
            } else if (newTexture.width == 1024) {
               _renderer.size = new Vector2(.85f, .85f);
            }
         }

         // Apply the alpha setting from the Renderer to the instanced Material
         Util.setAlpha(material, getColor().a);
      }
   }

   protected Material getMaterial () {
      return _image != null ? _image.material : _renderer.material;
   }

   protected Color getColor () {
      return _image != null ? _image.color : _renderer.color;
   }

   // Our Sprite Renderer (if any)
   protected SpriteRenderer _renderer;

   // Our Image (if any)
   protected Image _image;
}
