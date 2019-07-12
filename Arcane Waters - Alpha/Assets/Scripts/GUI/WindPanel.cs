using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WindPanel : MonoBehaviour {
   #region Public Variables

   // Our container object
   public GameObject container;

   // The Wind Arrow
   public Image windArrow;

   // Self
   public static WindPanel self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void Start() {
      // Update the Window arrow every few seconds
      InvokeRepeating("updateWindArrow", 3f, 5f);
   }

   public void Update () {
      // Only show the Wind panel when we're not on the intro screens
      container.SetActive(!TitleScreen.self.isShowing() && !CharacterScreen.self.isShowing());
   }

   public void updateWindArrow () {
      // Set the rotation of our arrow to match the direction of the wind in the instance
      if (Global.player != null) {
         int instanceId = Global.player.instanceId;
         Instance instance = InstanceManager.self.getInstance(instanceId);

         // If we found our Instance object, look up the current wind direction
         if (instance != null) {
            Vector2 windDirection = new Vector2(1f, 1f);
            float angle = Util.AngleBetween(Vector2.up, windDirection);

            // Apply the rotation to our wind arrow
            windArrow.transform.localRotation = Quaternion.identity;
            windArrow.transform.Rotate(0f, 0f, angle);
         }
      }
   }

   #region Private Variables

   #endregion
}
