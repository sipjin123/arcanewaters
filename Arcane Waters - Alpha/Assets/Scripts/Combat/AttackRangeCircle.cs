using UnityEngine;

public class AttackRangeCircle : MonoBehaviour
{
   #region Public Variables

   // The line renderer component
   public LineRenderer lineRenderer;

   #endregion

   public void initialize (float angleStep) {
      _angleStep = angleStep;

      // Calculates the number of vertices
      _vertexCount = (int) (360 / angleStep) + 1;

      // Initialize the vertex array
      _vertices = new Vector3[_vertexCount];

      // Initialize the line renderer
      lineRenderer.positionCount = _vertexCount;
   }

   public void draw (float radius) {
      // Set the first vertex
      Vector2 vertex = new Vector2(0, radius);

      // The rotation to be performed at each step
      Quaternion rotationStep = Quaternion.AngleAxis(_angleStep, Vector3.forward);

      // Rotate the first vertex to determine the coordinates of all the others
      for (int i = 0; i < _vertexCount; i++) {
         _vertices[i] = new Vector2(vertex.x, vertex.y);

         // Apply the rotation for the next vertex
         vertex = rotationStep * vertex;
      }

      // Draw the circle
      lineRenderer.SetPositions(_vertices);
   }

   public void setColor(Gradient gradient) {
      lineRenderer.colorGradient = gradient;
   }

   public void show () {
      gameObject.SetActive(true);
   }

   public void hide () {
      gameObject.SetActive(false);
   }

   #region Private Variables

   // The precision of the circle, in degrees
   private float _angleStep;

   // The array of vertices
   private Vector3[] _vertices;

   // The number of vertices
   private int _vertexCount;

   #endregion
}