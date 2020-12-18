using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DottedCircle : MonoBehaviour {
   #region Public Variables

   // The radius of the dotted circle
   public float circleRadius;

   // How many segments will make up the circle
   public int numSegments;

   // Prefabs spawned to to represent each segment ("dot") of the circle
   public GameObject segmentPrefab;

   // If populated, all segments of the dotted circle will have their sprites replaced with this.
   public Sprite dotOverride;

   // When set to true, the circle will update the position of its dots, allowing it to respond to changes in radius at runtime
   public bool updateCircle = false;

   // When set to true, the images that make up the dotted circle will rotate to match the direction of the line
   public bool rotateSegmentSprites;

   #endregion

   private void Start () {
      createSegments();
      updateSegments();
   }

   // Creates objects for the circle segments, and stores them in a list
   private void createSegments () {
      if (numSegments <= 0) {
         return;
      }

      for (int i = 0; i < numSegments; i++) {
         GameObject newSegment = Instantiate(segmentPrefab, transform);
         SpriteRenderer renderer = newSegment.GetComponent<SpriteRenderer>();

         // If an override is provided, override the sprites of each dot
         if (dotOverride) {
            renderer.sprite = dotOverride;
         }

         _circleSegments.Add(newSegment);
         _circleSegmentRenderers.Add(renderer);
      }
   }

   public void updateSegments () {
      if (numSegments <= 0) {
         return;
      }

      float segmentAngle = 0.0f;
      float angleIncrement = 360.0f / numSegments;
      foreach(GameObject segment in _circleSegments) {
         // Calculate offset
         Vector2 segmentPositionOffset = ExtensionsUtil.Rotate(Vector2.up, segmentAngle) * circleRadius;

         // Support hierarchy scaling 
         segmentPositionOffset = new Vector2(segmentPositionOffset.x * transform.lossyScale.x, segmentPositionOffset.y * transform.lossyScale.y);

         segment.transform.position = transform.position + segmentPositionOffset.ToVector3();
         segmentAngle += angleIncrement;
      }

      if (rotateSegmentSprites) {
         rotateSegments();
      }
   }

   private void rotateSegments () {
      float segmentAngle = 0.0f;
      float angleIncrement = 360.0f / numSegments;
      foreach(GameObject segment in _circleSegments) {
         segment.transform.rotation = Quaternion.Euler(0.0f, 0.0f, segmentAngle);
         segmentAngle += angleIncrement;
      }
   }

   public void setCircleColor (Color newColor) {
      foreach(SpriteRenderer renderer in _circleSegmentRenderers) {
         renderer.color = newColor;
      }
   }

   #region Private Variables

   // A list of the objects that represent each segment of this dotted circle
   private List<GameObject> _circleSegments = new List<GameObject>();

   // A list of the renderers of the segments of this dotted circle
   private List<SpriteRenderer> _circleSegmentRenderers = new List<SpriteRenderer>();

   #endregion
}
