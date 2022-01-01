using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class CompositeAnimationPlayer : ClientMonoBehaviour
{
   #region Public Variables

   // The status of the composite animation player
   public enum CompositeAnimationStatus
   {
      // None
      None = 0,

      // Running
      Running = 1,

      // Paused
      Paused = 2,

      // Stopped
      Stopped = 3
   }

   #endregion

   private void Start () {
      _image = GetComponent<Image>();
      _renderer = GetComponent<SpriteRenderer>();
   }

   private void Update () {
      if (_status == CompositeAnimationStatus.Paused || _status == CompositeAnimationStatus.Stopped) {
         return;
      }

      advanceTime();
      CompositeAnimationFrame frame = computeFrameByTime(_time);
      applyFrame(frame);
      checkStatus(frame);
   }

   #region Animation Controls

   public void play (CompositeAnimation animation) {
      // If the animation is already playing just skip
      if (_status == CompositeAnimationStatus.Running) {
         return;
      }

      if (animation.frames == null || animation.frames.Length == 0) {
         D.debug("The procedural animation can't play because the animation has no frames.");
         return;
      }

      // Store the flip state of the sprite rendeer
      if (getRenderer()) {
         bool _prevFlipXStatus = getRenderer().flipX;
      }

      _currentAnimation = animation;
      _lastFrame = null;
      _time = 0;
      _status = CompositeAnimationStatus.Running;
      enabled = true;
   }

   public void stop () {
      // Revert the flipX flag
      if (getRenderer()) {
         getRenderer().flipX = _prevFlipXStatus;
      }

      _status = CompositeAnimationStatus.Stopped;
      enabled = false;
   }

   public void resume () {
      _status = CompositeAnimationStatus.Running;
   }

   public void pause () {
      _status = CompositeAnimationStatus.Paused;
   }

   #endregion

   private void advanceTime () {
      _time += Time.deltaTime;

      if (_time >= computeTotalDuration()) {
         if (_currentAnimation.isLooping) {
            _time = 0.0f;
         }
      }
   }

   public CompositeAnimationStatus getStatus () {
      return _status;
   }

   private float computeTotalDuration () {
      if (_currentAnimation.frames == null || _currentAnimation.frames.Length == 0) {
         return 0.0f;
      }

      return _currentAnimation.frames.Sum(_ => _.duration);
   }

   private CompositeAnimationFrame computeFrameByTime (float time) {
      if (_currentAnimation.frames == null || _currentAnimation.frames.Length == 0) {
         return null;
      }

      float frameTimeAccumulator = 0.0f;
      float prevFrameTimeAccumulator = 0.0f;

      foreach (CompositeAnimationFrame frame in _currentAnimation.frames) {
         frameTimeAccumulator += frame.duration;

         if (prevFrameTimeAccumulator <= time && time < frameTimeAccumulator) {
            return frame;
         }

         prevFrameTimeAccumulator = frameTimeAccumulator;
      }

      return _currentAnimation.frames.First();
   }

   private void applyFrame (CompositeAnimationFrame frame) {
      if (frame == null || frame == _lastFrame) {
         return;
      }

      if (getCurrentTexture() != _lastLoadedTexture) {
         _lastLoadedTexture = getCurrentTexture();
         _sprites = extractSpritesFromTexture(_lastLoadedTexture);
      }

      switch (frame.type) {
         case CompositeAnimationFrame.FrameTypes.None:
            break;
         case CompositeAnimationFrame.FrameTypes.Index:
            if (isValidFrame(frame)) {
               if (getRenderer()) {
                  getRenderer().sprite = _sprites[frame.index];
               }

               if (getImage()) {
                  getRenderer().sprite = _sprites[frame.index];
               }
            }
            break;
         case CompositeAnimationFrame.FrameTypes.Flip:
            if (getRenderer()) {
               getRenderer().flipX = !getRenderer().flipX;
            }
            break;
      }

      _lastFrame = frame;
   }

   private void checkStatus (CompositeAnimationFrame frame) {
      if (frame == null || _currentAnimation == null || _currentAnimation.isLooping) {
         return;
      }

      if (frame == _currentAnimation.frames.Last()) {
         _status = CompositeAnimationStatus.Stopped;
      }
   }

   private bool isValidFrame (CompositeAnimationFrame frame) {
      return _sprites != null && _sprites.Length > 0 && 0 <= frame.index && frame.index < _sprites.Length;
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

   protected Texture2D getCurrentTexture () {
      if (_image != null && _image.sprite != null) {
         return _image.sprite.texture;
      }

      if (_renderer != null && _renderer.sprite != null) {
         return _renderer.sprite.texture;
      }

      return ImageManager.self.blankTexture;
   }

   private Sprite[] extractSpritesFromTexture (Texture2D texture) {
      // We have to store the individual sprites from the new texture
      if (texture == ImageManager.self.blankTexture) {
         return new Sprite[]{
            ImageManager.self.blankSprite
         };
      }

      // Load our sprites
      return ImageManager.getSprites(texture);
   }

   public void replaceTexture (Texture2D texture) {
      _lastLoadedTexture = texture;
      extractSpritesFromTexture(_lastLoadedTexture);
   }

   #region Private Variables

   // The frames played by this animation player
   private CompositeAnimation _currentAnimation;

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

   // Start time
   protected int _startTime;

   // The status of the flipx flag on the sprite renderer before the animation started
   protected bool _prevFlipXStatus;

   // The status of the animation
   protected CompositeAnimationStatus _status = CompositeAnimationStatus.Stopped;

   // Time accumulator
   private float _time;

   // Reference to the last frame
   private CompositeAnimationFrame _lastFrame;

   #endregion
}
