using UnityEngine;
using UnityEngine.UI;

public class MouseTrailParticle : MonoBehaviour
{
   #region Public Variables

   // The set of frames
   public Sprite[] frames;

   // The duration of a frame
   [Min(0)]
   public float frameDuration;

   // Reference to the Image control that displays the frame
   public Image image;

   // Does the animation loop?
   public bool isLooping;

   #endregion

   private void Update () {
      int frame = computeFrame(_currentTime, frameDuration);

      if (frame != _currentFrame) {
         updateFrame(frame);
         _currentFrame = frame;
      }

      image.rectTransform.Translate(.0f, _particleSpeed, .0f, Space.Self);
      _currentTime += Time.deltaTime;

      if (isLooping && hasReachedEnd()) {
         _currentTime = 0;
      }
   }

   public bool hasReachedEnd () {
      return _currentTime > computeTotalAnimationDuration();
   }

   private int computeFrame (float currentTime, float frameDuration) {
      return Mathf.Max(0, (int) (currentTime / frameDuration));
   }

   private void updateFrame (int frame) {
      if (frames == null || frame < 0 || image == null || frame >= frames.Length) {
         return;
      }

      image.sprite = frames[frame];
   }

   public float computeTotalAnimationDuration () {
      if (frames == null || frames.Length == 0) {
         return 0;
      }

      return frames.Length * frameDuration;
   }

   public void setPosition(Vector2 position) {
      _particleStartingPosition = position;
      image.rectTransform.anchoredPosition = position;
   }

   public void setSpeed(float speed) {
      _particleSpeed = speed;
   }

   public void restart () {
      _currentTime = 0;
      _currentFrame = -1;
   }

   #region Private Variables

   // Current time
   private float _currentTime;

   // Current frame
   private int _currentFrame = -1;

   // The speed of the particle in the Y axis
   private float _particleSpeed;

   // The starting position of the particle
   private Vector2 _particleStartingPosition;

   #endregion
}
