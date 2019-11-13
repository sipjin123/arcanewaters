using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackTrajectory : MonoBehaviour
{
   #region Public Variables

   // The number of points used to draw the line
   public static int POSITIONS_COUNT = 15;

   // The line renderer component
   public LineRenderer lineRenderer;

   // The animator component
   public Animator animator;

   #endregion

   private void Awake () {
      lineRenderer = GetComponent<LineRenderer>();

      // Initialize the line renderer
      lineRenderer.positionCount = POSITIONS_COUNT + 1;
      
      // Initialize the line points array
      _positions = new Vector3[POSITIONS_COUNT + 1];

      // Save the base width
      _baseLineWidth = lineRenderer.widthMultiplier;
   }

   public void draw (Vector2 startPos, Vector2 endPos, float widthModifier, Color lineColor) {
      // Calculate the positions relative to the start position
      endPos = endPos - startPos;
      startPos = new Vector2(0, 0);

      // Calculate the normalized distance between each point
      float step = 1f / POSITIONS_COUNT;

      // Determine the position of each line point
      for (int i = 0; i < POSITIONS_COUNT; i++) {
         _positions[i] = Vector2.Lerp(startPos, endPos, step * i);
         _positions[i].y += AttackManager.getArcHeight(startPos, endPos, step * i, true);
      }

      // Set the last position
      _positions[_positions.Length - 1] = endPos;

      // Set the width
      lineRenderer.widthMultiplier = _baseLineWidth * widthModifier;

      // Set the color of the line by updating its gradient color keys
      Color c = lineColor;
      Gradient gradient = lineRenderer.colorGradient;
      GradientColorKey[] colorKey = new GradientColorKey[2];
      colorKey[0].color = c;
      colorKey[0].time = 0.0f;
      colorKey[1].color = c;
      colorKey[1].time = 1.0f;
      gradient.colorKeys = colorKey;
      lineRenderer.colorGradient = gradient;

      // Draw the line
      lineRenderer.SetPositions(_positions);
   }

   public void show () {
      animator.SetBool("visible", true);
   }

   public void hide () {
      animator.SetBool("visible", false);
   }

   #region Private Variables

   // The array of points
   private Vector3[] _positions;

   // The base width of the line
   private float _baseLineWidth;

   #endregion
}
