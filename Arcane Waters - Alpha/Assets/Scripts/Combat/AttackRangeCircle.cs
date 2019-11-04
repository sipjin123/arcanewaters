using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackRangeCircle : MonoBehaviour
{
   #region Public Variables
   
   // The maximum distance between two dots in the circle
   public static float MAX_ARC_LENGTH = 0.2f;

   // The rotation speed
   public static float ROTATION_SPEED = 3f;

   // The prefab we use to create dots
   public AttackRangeDot dotPrefab;

   // The container for the dots
   public GameObject dotContainer;

   #endregion

   public void draw (float radius) {
      // Calculate the circumference of the circle
      float circumference = 2 * Mathf.PI * radius;

      // Calculate the number of dots needed
      int dotCount = Mathf.CeilToInt(circumference / MAX_ARC_LENGTH);

      // Calculate the angle between each dot
      float angleStep = 360f / dotCount;

      // Destroy any existing dot
      dotContainer.DestroyChildren();

      // Clear the dot list
      _dots.Clear();

      // Draw the circle
      float angle = 0f;
      for (int i = 0; i < dotCount; i++) {
         // Instantiate a new dot
         AttackRangeDot dot = Instantiate(dotPrefab, dotContainer.transform);

         // Set the dot position
         dot.setPosition(angle, radius);

         // Add the dot to the list
         _dots.Add(dot);

         // Increase the angle
         angle += angleStep;
      }
   }

   public void Update () {
      // Slowly rotate while active
      transform.Rotate(Vector3.forward, ROTATION_SPEED * Time.deltaTime);
   }

   public void show () {
      gameObject.SetActive(true);
      foreach(AttackRangeDot dot in _dots) {
         dot.show();
      }
   }

   public void hide () {
      foreach (AttackRangeDot dot in _dots) {
         dot.hide();
      }
      gameObject.SetActive(false);
   }

   #region Private Variables

   // A reference to all the dots
   private List<AttackRangeDot> _dots = new List<AttackRangeDot>();

   #endregion
}