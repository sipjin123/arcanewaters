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

   // What z-offset will be applied to the z-snap script attached to the dots for this line
   public float dotsZOffset = 0.0f;

   #endregion

   private void Start () {
      checkInit();
   }

   private void checkInit () {
      // If we have already initialised, return
      if (_hasInitialised) {
         return;
      }

      _hasInitialised = true;
      createSegments();
      updateLine();
   }

   // Creates objects for the line segments, and stores them in a list
   private void createSegments () {
      for (int i = 0; i < numSegments; i++) {
         createNewSegment();
      }

      overrideSprites();
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
            Vector3 newPosition = lineStart.position + (float) i * offsetVector;
            _lineSegments[i].transform.position = new Vector3(newPosition.x, newPosition.y, _lineSegments[i].transform.position.z);
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

      _lineColor = newColor;
   }

   public void setNumSegments (int newNumSegments) {
      // If new number of segments is larger, create new segments
      if (newNumSegments > numSegments) {
         int numToCreate = newNumSegments - numSegments;
         for (int i = 0; i < numToCreate; i++) {
            createNewSegment();
         }

      // If new number of segments is smaller, remove some segments
      } else if (newNumSegments < numSegments) {
         int numToRemove = numSegments - newNumSegments;

         for (int i = 0; i < numToRemove; i++) {
            Destroy(_lineSegments[numSegments - 1 - i]);
         }

         _lineSegments.RemoveRange(newNumSegments, numToRemove);
         _lineSegmentRenderers.RemoveRange(newNumSegments, numToRemove);
      }

      numSegments = newNumSegments;
      overrideSprites();
   }

   private void createNewSegment () {
      GameObject newSegment = Instantiate(segmentPrefab, transform);
      SpriteRenderer renderer = newSegment.GetComponent<SpriteRenderer>();
      renderer.color = _lineColor;
      newSegment.GetComponent<ZSnap>().offsetZ = dotsZOffset;

      _lineSegments.Add(newSegment);
      _lineSegmentRenderers.Add(renderer);
   }

   private void overrideSprites () {
      for (int i = 0; i < numSegments; i++) {
         // Override the first dot if appropriate
         if (i == 0 && startDotOverride) {
            _lineSegmentRenderers[i].sprite = startDotOverride;

         // Override the last dot if appropriate
         } else if (i == numSegments - 1 && endDotOverride) {
            _lineSegmentRenderers[i].sprite = endDotOverride;

         // Override other dots if appropriate
         } else if (dotOverride) {
            _lineSegmentRenderers[i].sprite = dotOverride;
         }
      }
   }

   #region Private Variables

   // A list of the objects that represent each segment of this dotted line
   private List<GameObject> _lineSegments = new List<GameObject>();

   // A list of the renderers of the segments of this dotted line
   private List<SpriteRenderer> _lineSegmentRenderers = new List<SpriteRenderer>();

   // Set to true after initialising
   private bool _hasInitialised = false;

   // The color that this line has been set to
   private Color _lineColor = Color.white;

   #endregion
}
