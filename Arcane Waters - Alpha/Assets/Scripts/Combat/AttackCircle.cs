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
   public float startTime;

   // Our End Time
   public float endTime;

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
   }

   void Update () {
      // Update the alpha of the sprite
      imageRenderer.color = new Color(color.r, color.g, color.b, alpha);

      // If we've been alive long enough, destroy ourself
      if (TimeManager.self.getSyncedTime() > this.endTime) {
         Destroy(this.gameObject);
         return;
      }
   }

   #region Private Variables

   #endregion
}
