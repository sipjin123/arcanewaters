using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DottedParabola : MonoBehaviour {
   #region Public Variables

   // Transform defining where the start of the parabola will be
   public Transform parabolaStart;

   // Transform defining where the end of the parabola will be
   public Transform parabolaEnd;

   // A reference to the prefabs that will be spawned to represent each segment ("dot") of the parabola
   public GameObject segmentPrefab;

   // The height of the apex of the parabola
   public float parabolaHeight = 1.0f;

   // How many segments will make up the line
   public int numSegments;

   // If populated, all segments of the dotted parabola will have their sprites replaced with this. (End override will override this)
   public Sprite dotOverride;

   // If populated, the end segment of the dotted parabola will have its sprite replaced with this.
   public Sprite endDotOverride;

   // What z-offset will be applied to the z-snap script attached to the dots for this line
   public float dotsZOffset = 0.0f;

   // The color of this dotted parabola
   public Color parabolaColor;

   #endregion
   private void Awake () {
      createSegments();
      updateParabola();
      setParabolaColor(parabolaColor);
   }

   private void createSegments () {
      for (int i = 0; i < numSegments; i++) {
         GameObject newSegment = Instantiate(segmentPrefab, transform);
         SpriteRenderer renderer = newSegment.GetComponent<SpriteRenderer>();
         newSegment.GetComponent<ZSnap>().offsetZ = dotsZOffset;

         // Override sprite if an override is provided
         if (dotOverride) {
            renderer.sprite = dotOverride;
         }
         
         _parabolaSegments.Add(newSegment);
         _parabolaSegmentRenderers.Add(renderer);
      }

      // Override end dot
      if (endDotOverride && _parabolaSegmentRenderers.Count > 1) {
         _parabolaSegmentRenderers[_parabolaSegmentRenderers.Count - 1].sprite = endDotOverride;
      }
   }

   public void updateParabola () {
      // Position the start dot
      if (_parabolaSegments.Count > 0) {
         _parabolaSegments[0].transform.position = new Vector3(parabolaStart.position.x, parabolaStart.position.y, _parabolaSegments[0].transform.position.z); 
      }
      
      // Position the end dot
      if (_parabolaSegments.Count > 1) {
         _parabolaSegments[_parabolaSegments.Count - 1].transform.position = new Vector3(parabolaEnd.position.x, parabolaEnd.position.y, _parabolaSegments[_parabolaSegments.Count -1].transform.position.z);
      }

      // Position the middle dots
      if (_parabolaSegments.Count > 2) {
         // Get the vector from the player to their target
         Vector2 toTarget = parabolaEnd.position - parabolaStart.position;
         
         // Calculate the distance between dots
         float segmentDist = 1.0f / (_parabolaSegments.Count - 1);

         for ( int i = 1; i < _parabolaSegments.Count - 1; i++) {
            // Calculate the position of this point on the parabola
            float t = i * segmentDist * toTarget.magnitude;
            float h = Util.getPointOnParabola(parabolaHeight, toTarget.magnitude, t);
            Vector2 offset = t * toTarget.normalized;

            offset += h * Vector2.up;

            Vector3 finalPosition = parabolaStart.position + new Vector3(offset.x, offset.y, 0.0f);
            _parabolaSegments[i].transform.position = new Vector3(finalPosition.x, finalPosition.y, _parabolaSegments[i].transform.position.z);
         }
      }
   }

   public void setParabolaColor (Color newColor) {
      parabolaColor = newColor;
      foreach (SpriteRenderer renderer in _parabolaSegmentRenderers) {
         renderer.color = parabolaColor;
      }
   }

   private void overrideSprites () {
      for (int i = 0; i < numSegments; i++) {
         // Override the last dot if appropriate
         if (i == numSegments - 1 && endDotOverride) {
            _parabolaSegmentRenderers[i].sprite = endDotOverride;

            // Override other dots if appropriate
         } else if (dotOverride) {
            _parabolaSegmentRenderers[i].sprite = dotOverride;
         }
      }
   }

   public void setNewSprites (Sprite newDotSprite, Sprite newEndDotSprite = null) {
      if (newDotSprite != null) {
         dotOverride = newDotSprite;
      }

      if (newEndDotSprite != null) {
         endDotOverride = newEndDotSprite;
      }

      overrideSprites();
   }

   #region Private Variables

   // A list of the objects that represent each segment of this dotted parabola
   private List<GameObject> _parabolaSegments = new List<GameObject>();

   // A list of the renderers of the segments of this dotted parabola
   private List<SpriteRenderer> _parabolaSegmentRenderers = new List<SpriteRenderer>();

   #endregion
}
