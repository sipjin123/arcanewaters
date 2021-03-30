using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackTrajectory : MonoBehaviour
{
   #region Public Variables

   // The number of points used to draw the line
   public static int POSITIONS_COUNT = 20;

   // The speed of the dot movement
   public float DOT_MIN_SPEED = 0.1f;
   public float DOT_MAX_SPEED = 0.5f;

   // The prefab we use for creating dots
   public AttackTrajectoryDot dotPrefab;

   #endregion

   private void Awake () {
      D.adminLog("AttackTrajectory.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      // Instantiates the dots
      _allDots = new AttackTrajectoryDot[POSITIONS_COUNT + 1];
      for (int i = 0; i < _allDots.Length; i++) {
         _allDots[i] = Instantiate(dotPrefab, transform, false);
      }
      D.adminLog("AttackTrajectory.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   public void draw (Vector2 startPos, Vector2 endPos, Color lineColor, float maxRange) {
      // Hide all the dots
      foreach (AttackTrajectoryDot dot in _allDots) {
         dot.hide();
      }

      // Calculate the distance between the dots
      float dotDistance = maxRange / POSITIONS_COUNT;

      // Calculate the distance between the ship and the cursor
      float distance = Vector2.Distance(startPos, endPos);

      // Calculate the distance between the ship and the cursor, normalized to the max range
      float normalizedDistance = distance / maxRange;

      // When aiming close to the ship, the speed of the dot animation increases
      float dotSpeed = Mathf.Lerp(DOT_MAX_SPEED, DOT_MIN_SPEED, normalizedDistance);

      // Increase the dot offset to animate their movement
      _offset += dotSpeed * Time.deltaTime;
      _offset %= dotDistance;

      // Distribute all the dots along the straight line between the ship and the cursor
      float dotLinearPos = 0f;
      foreach (AttackTrajectoryDot dot in _allDots) {
         // If we reached the end of the line, the rest of the dots remain hidden
         if (dotLinearPos + _offset > distance) {
            break;
         }

         // Calculate the normalized position of the dot
         float normalizedLinearPos = (dotLinearPos + _offset) / distance;

         // Show the dot and set its position
         dot.show();
         dot.setPosition(startPos, endPos, lineColor, normalizedLinearPos);

         // Set the position of the next dot
         dotLinearPos += dotDistance;
      }
   }

   public void hide () {
      // Hide all the dots
      foreach (AttackTrajectoryDot dot in _allDots) {
         dot.hide();
      }
   }

   #region Private Variables

   // The array with all the trajectory dots
   private AttackTrajectoryDot[] _allDots;

   // The offset applied to the dot position, for animation
   private float _offset = 0f;

   #endregion
}
