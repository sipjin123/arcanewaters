using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DottedLine : MonoBehaviour {
   #region Public Variables

   // Transform defining where the line will be drawn from
   public Transform lineStart;

   // Transform defining where the line will be drawn to
   public Transform lineEnd;

   // Prefabs spawned to to represent each segment ("dot") of the line
   public GameObject segmentPrefab;

   // How many segments will make up the line
   public int numSegments;

   // Offsets the rotation of the line sprites when they are rotated - used for sprites that aren't facing upwards
   public float spriteAngleOffset;

   // If populated, all segments of the dotted line will have their sprites replaced with this. (Start and End overrides will override this)
   public Sprite dotOverride;

   // If populated, the starting segment of the dotted line will have its sprite replaced with this.
   public Sprite startDotOverride;

   // If populated, the end segment of the dotted line will have its sprite replaced with this.
   public Sprite endDotOverride;

   // When set to true, the images that make up the dotted line will rotate to match the direction of this line
   public bool rotateSegmentSprites;

   #endregion

   private void Start () {
      checkInit();
   }

   private void checkInit () {
      // If we have already initialised, return
      if (_lineSegments.Count > 0) {
         return;
      }
      
      createSegments();
      updateLine();
   }

   // Creates objects for the line segments, and stores them in a list
   private void createSegments () {
      for (int i = 0; i < numSegments; i++) {
         GameObject newSegment = Instantiate(segmentPrefab, transform);
         SpriteRenderer renderer = newSegment.GetComponent<SpriteRenderer>();

         // Override sprite if an override is provided
         if (dotOverride) {
            renderer.sprite = dotOverride;
         }

         _lineSegments.Add(newSegment);
         _lineSegmentRenderers.Add(renderer);
      }

      // Override starting dot
      if (startDotOverride && _lineSegments.Count > 0) {
         _lineSegments[0].GetComponent<SpriteRenderer>().sprite = startDotOverride;
      }

      // Override ending dot
      if (endDotOverride && _lineSegmentRenderers.Count > 1) {
         _lineSegmentRenderers[_lineSegmentRenderers.Count - 1].sprite = endDotOverride;
      }
   }

   private void rotateSegments () {
      float rotationAngle = Util.angle(lineEnd.position - lineStart.position);

      foreach(GameObject segment in _lineSegments) {
         segment.transform.rotation = Quaternion.Euler(0.0f, 0.0f, -rotationAngle + spriteAngleOffset);
      }
   }

   public void updateLine () {
      checkInit();

      // Position the start dot
      if (_lineSegments.Count > 0) {
         _lineSegments[0].transform.position = lineStart.position;
      }

      // Position the end dot
      if (_lineSegments.Count > 1) {
         _lineSegments[_lineSegments.Count - 1].transform.position = lineEnd.position;
      }

      // Position the middle dots
      if (_lineSegments.Count > 2) {
         Vector3 offsetVector = (lineEnd.position - lineStart.position) / (_lineSegments.Count - 1);
         offsetVector.z = 0.0f;

         for (int i = 1; i < _lineSegments.Count - 1; i++) {
            _lineSegments[i].transform.position = lineStart.position + (float)i * offsetVector;
         }
      }

      if (rotateSegmentSprites) {
         rotateSegments();
      }
   }

   public void setLineColor (Color newColor) {
      checkInit();
      
      foreach(SpriteRenderer renderer in _lineSegmentRenderers) {
         renderer.color = newColor;
      }
   }

   #region Private Variables

   // A list of the objects that represent each segment of this dotted line
   private List<GameObject> _lineSegments = new List<GameObject>();

   // A list of the renderers of the segments of this dotted line
   private List<SpriteRenderer> _lineSegmentRenderers = new List<SpriteRenderer>();

   #endregion
}
