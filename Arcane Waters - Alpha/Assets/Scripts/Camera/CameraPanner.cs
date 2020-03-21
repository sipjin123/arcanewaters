using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CameraPanner : MonoBehaviour
{
   #region Public Variables

   // How long it takes for the GameObject to to reach the next destination
   public float transitionTime = 6.0f;

   // How the transition should behave
   public AnimationCurve transitionCurve;

   // How long the GameObject will stay at the current point
   public float pauseTime = 6.0f;

   // If the points should be looped
   public bool isLooping = true;

   // Points to transition between
   public List<Transform> targetPausePositions;

   #endregion

   private void Awake () {
      _pausing = true;
   }

   private void Update () {
      _currentTime += Time.deltaTime;

      if (!_pausing) {
         transform.position = Vector3.Lerp(targetPausePositions[_lastPointIndex].position, targetPausePositions[_currentPointIndex].position, transitionCurve.Evaluate(_currentTime / transitionTime));

         if (_currentTime >= transitionTime) {
            _pausing = true;
            _currentTime = 0.0f;
         }
      } else {
         if (_currentTime >= pauseTime) {
            _pausing = false;
            _lastPointIndex = _currentPointIndex;
            _currentPointIndex = calculateNewPointIndex();
            _currentTime = 0.0f;
         }
      }
   }

   private int calculateNewPointIndex () {
      if (isLooping) {
         return ++_currentPointIndex % targetPausePositions.Count;
      }
      _currentPointIndex = Mathf.Min(++_currentPointIndex, targetPausePositions.Count - 1);
      return _currentPointIndex;
   }

   #region Private Variables

   // Current point index into the 'points' array
   private int _currentPointIndex;

   // Last point index into the 'points' array
   private int _lastPointIndex;

   // How much time has passed since the current state initiated
   private float _currentTime;

   // Are we currently pausing at a point?
   private bool _pausing;

   #endregion
}
