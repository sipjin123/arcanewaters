using UnityEngine;
using System.Collections.Generic;

public class ShipRollingAnimation : MonoBehaviour
{
   #region Public Variables

   // The set of transforms to animate
   public List<Transform> animatedObjects;

   // The maximum offset applied to the animated objects
   public float yOffset;

   // The rolling animation frequency ( rolls per seconds )
   public float rollsPerSecond;

   #endregion

   private void Update () {
      float frameDuration = 1 / rollsPerSecond;
      int frame = computeFrame(_currentTime, frameDuration);

      if (frame != _currentFrame) {
         updateAnimatedObjects(frame);
         _currentFrame = frame;
      }

      _currentTime += Time.deltaTime;

      // Reset the time when the animation ends
      if (_currentTime >= frameDuration * 2) {
         _currentTime = 0;
      }
   }

   private int computeFrame (float currentTime, float frameDuration) {
      return Mathf.Max(0, (int) (currentTime / frameDuration));
   }

   private void updateAnimatedObjects (int frame) {
      bool isNowHigh = frame % 2 == 0;
      Vector3 v;

      foreach (Transform animatedObject in animatedObjects) {
         if (animatedObject == null) {
            continue;
         }

         v = animatedObject.transform.position;
         animatedObject.transform.position = new Vector3(v.x, v.y + (isNowHigh ? yOffset : -yOffset), v.z);
      }
   }

   public void registerAnimatedObject (Transform animatedObject) {
      if (animatedObjects.Contains(animatedObject)) {
         return;
      }

      animatedObjects.Add(animatedObject);
   }

   public void unregisterAnimatedObject (Transform animatedObject) {
      if (!animatedObjects.Contains(animatedObject)) {
         return;
      }

      animatedObjects.Remove(animatedObject);
   }

   #region Private Variables

   // Current time
   private float _currentTime;

   // Current frame
   private int _currentFrame = -1;

   #endregion

}
