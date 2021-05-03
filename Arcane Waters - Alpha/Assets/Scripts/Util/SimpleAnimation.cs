using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class SimpleAnimation : ClientMonoBehaviour {
   #region Public Variables

   // An initial delay before the animation starts playing
   public float initialDelay = 0f;

   // A custom defined speed we set in the editor
   public float frameLengthOverride = -1f;

   // Whether we should delay starting the animation by half the animation length
   public bool delayStart = false;

   // Whether we should be destroyed after the animation finishes
   public bool destroyAtEnd = false;

   // Whether we should stay at the last frame
   public bool stayAtLastFrame = false;

   // How long we should wait after the animation ends before looping
   public float loopDelay = 0f;

   // Whether we want to temporarily pause the animation
   public bool isPaused = false;

   // The minimum index we want to animate
   public int minIndex = 0;

   // The maximum index we want to animate
   public int maxIndex = 1000;

   // The name of the currently playing animation, if any
   public Anim.Type currentAnimation;

   // The Group that defines our animations, if any
   public Anim.Group group = Anim.Group.None;

   // Disables this scripts capability to alter the alpha of the sprite
   public bool freezeAlpha;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      _renderer = GetComponent<SpriteRenderer>();
      _image = GetComponent<Image>();
   }

   public void modifyAnimSpeed (float speed) {
      CancelInvoke();
      frameLengthOverride = speed;
      Start();
   }

   public void resetAnimation () {
      CancelInvoke();
      Start();
   }

   private void Start () {
      if (!Util.isBatch()) {
         // Load our sprites
         reloadSprites(getCurrentTexture());

         // Routinely change our sprite
         float delay = this.delayStart ? (getTimePerFrame() * _sprites.Length / 2f) : initialDelay;
         InvokeRepeating(nameof(changeSprite), delay, getTimePerFrame());
      }
   }

   private void Update () {
      if (!Util.isBatch()) {
         Texture2D currentTexture = getCurrentTexture();

         // If the Texture associated with our sprites has changed, we need to reload
         if (_lastLoadedTexture != currentTexture) {
            setNewTexture(currentTexture);
         }
      }
   }

   public bool isWaitingForLoop () {
      if (_sprites == null) {
         return false;
      }

      float timeSinceLastFrameChange = Time.time - _lastFrameChangeTime;
      if (_index == (_sprites.Length - 1) && timeSinceLastFrameChange < loopDelay) {
         return true;
      }
      return false;
   }

   public void playAnimation (Anim.Type newAnimType) {
      // If we're already playing that animation type, don't do anything
      if (this.currentAnimation == newAnimType) {
         return;
      }

      // Look up the animation info for this group and type
      AnimInfo info = AnimUtil.getInfo(this.group, newAnimType);

      if (info.animType == Anim.Type.None) {
         D.debug("Couldn't find animation for type: " + newAnimType);
         return;
      }

      // TODO: Remove after confirming animation does not break after this update
      CancelInvoke();

      // Update the sprite indexes accordingly
      updateIndexMinMax(info.minIndex, info.maxIndex);
      this.currentAnimation = newAnimType;

      // TODO: Remove after confirming animation does not break after this update
      float delay = this.delayStart ? (getTimePerFrame() * _sprites.Length / 2f) : initialDelay;
      InvokeRepeating(nameof(changeSprite), delay, getTimePerFrame());
   }

   public void setNewTexture (Texture2D newTexture) {
      if (newTexture == ImageManager.self.blankTexture || newTexture == null) {
         setSprite(ImageManager.self.blankSprite);
         reloadSprites(newTexture);
         toggleRenderers(false);
         return;
      }
      toggleRenderers(true);

      _lastLoadedTexture = newTexture;

      // We have to store the individual sprites from the new texture
      reloadSprites(newTexture);

      // Update the sprite renderer
      int currentIndex = getIndex();

      if (_sprites.Length > currentIndex) {
         setSprite(_sprites[currentIndex]);
      }
   }

   public void toggleRenderers (bool isOn) {
      if (_renderer != null) {
         _renderer.enabled = isOn;
      }
      if (_image != null) {
         _image.enabled = isOn;
      }
   }

   public Sprite getInitialSprite () {
      if (_sprites == null) {
         return null;
      }

      return _sprites[0];
   }

   public int getIndex () {
      return _index;
   }

   public void setIndex (int index) {
      _index = index;
   }

   public void initialize () {
      _image = GetComponent<Image>();
      _renderer = GetComponent<SpriteRenderer>();

      reloadSprites(getCurrentTexture());
   }

   public void updateIndexMinMax (int min, int max) {
      minIndex = min;
      maxIndex = max;

      _index = minIndex;

      // Change the sprite
      if (_sprites != null) {
         if (_index > _sprites.Length) {
            // Don't print the warning if the layer isn't using a valid texture
            if (getCurrentTexture() != null && getCurrentTexture() != ImageManager.self.blankTexture) {
               D.debug("Index out of range for object: " + _index + " / " + _sprites.Length + " : " + gameObject.name);
            }
            return;
         }

         setSprite(_sprites[_index]);
      }

      // Make note of the time
      _lastFrameChangeTime = Time.time;
   }

   protected void reloadSprites (Texture2D newTexture) {
      if (newTexture == ImageManager.self.blankTexture) {
         toggleRenderers(false);
         _sprites = new Sprite []{
            ImageManager.self.blankSprite
         };
         return;
      }
      enabled = true;
      toggleRenderers(true);

      // Load our sprites
      _sprites = ImageManager.getSprites(newTexture);
   }

   protected void changeSprite () {
      // If we've been temporarily paused, don't do anything
      if (isPaused || !enabled || _sprites == null || getCurrentTexture() == null || getCurrentTexture() == ImageManager.self.blankTexture) {
         return;
      }

      // Check how long it's been since we last updated the frame
      float timeSinceLastFrameChange = Time.time - _lastFrameChangeTime;

      // If we hit the end, check if we need to pause for a bit
      if (_index == (_sprites.Length - 1) && timeSinceLastFrameChange < loopDelay) {
         setVisible(false);
         return;
      } else {
         setVisible(true);
      }

      // Update the index
      if (_sprites.Length != 0) {
         _index = (_index + 1) % _sprites.Length;

         // If we looped back to the beginning, maybe destroy
         if (_index == 0 && destroyAtEnd) {
            Destroy(this.gameObject);
         }
      }

      // Keep the index within our specified min/max
      if (_index < minIndex || _index > maxIndex) {
         _index = minIndex;
      }

      if (_sprites.Length == 1) {
         setSprite(_sprites[0]);
      } else if (_index > _sprites.Length) {
         string msg = string.Format("For animating {0}, the index {1} is greater than the sprite array length of {2}.", _lastLoadedTexture, _index, _sprites.Length);
         Debug.LogWarning(msg);
         return;
      } else {
         // Change the sprite
         setSprite(_sprites[_index]);
      }

      // Make note of the time
      _lastFrameChangeTime = Time.time;

      // Check if we need to pause
      if (Anim.pausesAtEnd(this.currentAnimation) && _index >= maxIndex) {
         this.isPaused = true;
      }

      // If we reached the last frame, we might be finished
      if (stayAtLastFrame && _index == _sprites.Length - 1) {
         CancelInvoke();
      }
   }

   protected void setVisible (bool isVisible) {
      // Use the alpha to show or hide, since the renderer 'enabled' property could be controlled by an Observer Manager
      if (_renderer != null) {
         Color color = _renderer.color;
         if (!freezeAlpha) {
            color.a = isVisible ? 1f : 0f;
         }
         _renderer.color = color;
      }

      if (_image != null) {
         _image.enabled = isVisible;
      }
   }

   protected void setSprite (Sprite sprite) {
      if (getRenderer() != null) {
         _renderer.sprite = sprite;
      }

      if (getImage() != null) {
         _image.sprite = sprite;
      }
   }

   protected Image getImage () {
      if (_image == null) {
         _image = GetComponent<Image>();
      }

      return _image;
   }

   protected SpriteRenderer getRenderer () {
      if (_renderer == null) {
         _renderer = GetComponent<SpriteRenderer>();
      }

      return _renderer;
   }

   protected float getTimePerFrame () {
      if (frameLengthOverride > 0f) {
         return frameLengthOverride;
      }

      return .25f;
   }

   protected Texture2D getCurrentTexture () {
      if (_image != null && _image.sprite != null) {
         return _image.sprite.texture;
      } else {
         if (_renderer == null || _renderer.sprite == null) {
            return ImageManager.self.blankTexture;
         } else {
            return _renderer.sprite.texture;
         }
      }
   }

   #region Private Variables

   // Our Renderer
   protected SpriteRenderer _renderer;

   // The Texture we last loaded
   protected Texture2D _lastLoadedTexture;

   // Our Image (if any)
   protected Image _image;

   // Our set of sprites
   protected Sprite[] _sprites;

   // Our current sprite index
   [SerializeField]
   protected int _index;

   // The time at which we last changed frames
   protected float _lastFrameChangeTime;

   #endregion
}
