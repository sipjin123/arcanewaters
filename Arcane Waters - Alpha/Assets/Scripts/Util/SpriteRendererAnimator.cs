using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererAnimator : ClientMonoBehaviour, IBiomable
{
   #region Public Variables

   [Tooltip("List of sprites we will use for the animation")]
   public List<Sprite> sprites = new List<Sprite>();

   [Tooltip("How much time it takes for each frame")]
   public float timePerFrame = 0.25f;

   [Tooltip("Should the animation go backwards")]
   public bool playBackwards = false;

   [Tooltip("Should the animation stop at last frame")]
   public bool stayAtLastFrame = false;

   [Tooltip("Is the animation paused currently")]
   public bool paused = false;

   [Header("Biomable"), Tooltip("Whether to react to biome changes")]
   public bool reactToBiomeChanges = false;

   [Tooltip("The current set biome")]
   public Biome.Type currentBiome = Biome.Type.Forest;

   #endregion

   protected override void Awake () {
      base.Awake();

      _sr = GetComponent<SpriteRenderer>();
   }

   private void Update () {
      if (Time.time > _nextFrameTime && !paused) {
         performAnimationFrame();
      }
   }

   private void performAnimationFrame () {
      _nextFrameTime = Time.time + timePerFrame;

      if (sprites.Count == 0) return;

      int delta = playBackwards ? -1 : 1;
      int nextIndex = _previousIndex + delta;

      // If we should stay at last frame and the index is out of bounds, do nothing
      if (stayAtLastFrame && (nextIndex < 0 || nextIndex >= sprites.Count)) {
         return;
      }

      // Clamp index in bounds
      while (nextIndex < 0) {
         nextIndex += sprites.Count;
      }
      _previousIndex = nextIndex % sprites.Count;

      _sr.sprite = sprites[_previousIndex];
   }

   public void setAnimationSprites (List<Sprite> newSprites) {
      if (newSprites == null) {
         newSprites = new List<Sprite>();
      }

      sprites = newSprites;

      // Set previous index to -1 so next frame makes it 0 - the first frame
      _previousIndex = -1;
      _nextFrameTime = 0;

      performAnimationFrame();
   }

   public void setBiome (Biome.Type biomeType, bool skipClientOnlyFunctionality) {
      if (reactToBiomeChanges && !Util.isBatch() && !skipClientOnlyFunctionality) {
         List<Sprite> newSprites = new List<Sprite>();

         // Take current sprites and switch their biomes
         foreach (Sprite sprite in sprites) {
            newSprites.Add(Util.switchSpriteBiome(sprite, currentBiome, biomeType));
         }

         // Apply newly switched sprites as the current animation
         setAnimationSprites(newSprites);
      }

      currentBiome = biomeType;
   }

   #region Private Variables

   // Sprite renderer component
   private SpriteRenderer _sr = null;

   // Index of previous frame
   private int _previousIndex = -1;

   // Time when next frame has to be executed
   private float _nextFrameTime = 0;

   #endregion
}
