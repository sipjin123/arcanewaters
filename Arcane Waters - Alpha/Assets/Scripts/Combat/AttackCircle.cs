using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackCircle : MonoBehaviour {

   #region Public Variables

   // The creator of this Attack Circle
   public SeaEntity creator;

   // Our Start Point
   public Vector2 startPos;

   // Our End Point
   public Vector2 endPos;

   // Our Start Time
   public double startTime;

   // Our End Time
   public double endTime;

   // The color of the circle
   public Color color;

   // The alpha value of the sprite - set by the animator
   public float alpha = 1f;

   // The renderer
   public SpriteRenderer imageRenderer;

   #endregion

   void Start () {
      // Initialize the color of the sprite
      imageRenderer.color = new Color(color.r, color.g, color.b, alpha);

      // Set the z position
      setZ();
   }

   void Update () {
      // Set the z position
      setZ();

      // Update the alpha of the sprite
      imageRenderer.color = new Color(color.r, color.g, color.b, alpha);

      // If we've been alive long enough, destroy ourself
      if (NetworkTime.time > this.endTime) {
         Destroy(this.gameObject);
         return;
      }
   }

   private void setZ () {
      if (Global.player != null) {
         // Get the current area
         Area area = AreaManager.self.getArea(Global.player.areaKey);

         // Set the z position, above the sea and below the land
         if (area != null) {
            GetComponent<FixedZ>().newZ = area.waterZ - 0.01f;
         }
      }
   }

   #region Private Variables

   #endregion
}
