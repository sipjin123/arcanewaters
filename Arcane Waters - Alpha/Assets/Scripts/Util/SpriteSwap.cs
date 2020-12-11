using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class SpriteSwap : ClientMonoBehaviour
{
   #region Public Variables

   // The Texture we want to swap to
   public Texture2D newTexture;

   #endregion

   protected void Start () {
      // Look up components
      _renderer = GetComponent<SpriteRenderer>();
      _image = GetComponent<Image>();

      // Load the sprites from the new Texture
      if (newTexture != null) {
         this.loadSprites(newTexture);
      }
   }

   protected void LateUpdate () {
      if (newTexture == null) {
         return;
      }

      // If our Texture changes, reload our sprites
      if (newTexture.name != _loadedTextureName) {
         this.loadSprites(newTexture);
      }

      // If we don't have any swap sprites defined, we don't have to do anything
      if (_spritesToSwapIn.Count == 0) {
         return;
      }

      // Swap in the replacement sprite using our Dictionary indexed by frame number (supports up to 99 frames)
      Sprite oldSprite = getSprite();
      string currentFrameNumber = Util.getFrameNumber(oldSprite);
      Sprite newSprite = null;
      _spritesToSwapIn.TryGetValue(currentFrameNumber, out newSprite);
      if (newSprite != null) {
         setNewSprite(newSprite);
      } else {
         if (!Util.isBatch()) {
            D.debug("Sprite Frame '" + currentFrameNumber + "' did not exist in the Sheet '" + newTexture.name + "'. Please update the Sprite Sheet for game object: " + gameObject.name);
         }
      }
   }
  
   protected void loadSprites (Texture2D newTexture) {
      _spritesToSwapIn.Clear();

      if (newTexture != null) {
         // Get the array of sprites associated with the new texture
         Sprite[] newSprites = ImageManager.getSprites(newTexture);
         if (newSprites != null) {
            if (newSprites.Length > 1) { 
               // Store the sprites associated with our Texture, indexed by their frame number
               foreach (Sprite newSprite in newSprites) {
                  if (newSprite != null) {
                     string index = newSprite.name.Substring(newSprite.name.Length - 2);
                     string cleanIndex = index.Replace("_", "");
                     _spritesToSwapIn[cleanIndex] = newSprite;
                  } 
               }

               // Remember the name of the current Texture in case it is changed later
               _loadedTextureName = newTexture.name;
            } else {
               transform.gameObject.SetActive(false);
               D.debug("Sprite loaded was incorrect: " + newTexture.name + " : " + newSprites.Length);
            }
         } else {
            transform.gameObject.SetActive(false);
            D.debug("Problem with loading sprite: " + newTexture.name);
         }
      } else {
         D.debug("Texture is null!");
      }
   }

   protected void setNewSprite (Sprite newSprite) {
      // Some objects will be using Sprite Renderers, others will be using GUI images
      if (_renderer != null) {
         _renderer.sprite = newSprite;
      } else if (_image != null) {
         _image.sprite = newSprite;
      }
   }

   protected Sprite getSprite () {
      if (_renderer != null) {
         return _renderer.sprite;
      }
      if (_image != null) {
         return _image.sprite;
      }

      return null;
   }

   #region Private Variables

   // Our Sprite Renderer (if any)
   protected SpriteRenderer _renderer;

   // Our Image (if any)
   protected Image _image;

   // The name of the currently loaded Texture
   protected string _loadedTextureName;

   // The dictionary containing all the sliced up sprites, indexed by the last 2 characters of their animation frame
   protected Dictionary<string, Sprite> _spritesToSwapIn = new Dictionary<string, Sprite>();

   #endregion
}