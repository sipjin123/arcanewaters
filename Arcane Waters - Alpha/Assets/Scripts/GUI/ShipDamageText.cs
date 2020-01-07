using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipDamageText : MonoBehaviour {
   #region Public Variables

   // Our associated Text
   public Text text;

   // How long this text should live
   public static float LIFETIME = 1f;

   // How fast this text should float up
   public static float RISE_SPEED = .0035f;

   // Determines if positive or negative attribute
   public bool negativeEffect = false;

   // Effect indicators
   public GameObject frameObj;
   public Image icon;
   public Text notificationText;

   #endregion

   void Start () {
      _startTime = Time.time;

      // Make sure we show up in front of the ship
      Util.setZ(this.transform, -.32f);

      // Start floating upwards
      InvokeRepeating("floatUp", 0f, .02f);
      
      // Destroy after a couple seconds
      Destroy(this.gameObject, LIFETIME);
   }

   public void setIcon (string path) {
      if (!frameObj || !icon) {
         return;
      }

      frameObj.SetActive(true);
      icon.sprite = ImageManager.getSprite(path);
   }

   public void setMessage (string msg) {
      if (!frameObj || !notificationText) {
         return;
      }

      frameObj.SetActive(true);
      notificationText.text = msg;
   }

   public void setDamage (int amount) {
      this.text.text = negativeEffect ? "-" : "+" + amount;
   }

   protected void floatUp () {
      float timeAlive = Time.time - _startTime;

      // Slowly move upwards
      Vector3 currentPos = this.transform.position;
      currentPos.y += RISE_SPEED;
      this.transform.position = currentPos;

      // Also fade out
      Util.setAlpha(text, 1f - (timeAlive/LIFETIME));
   }

   #region Private Variables

   // The time at which we were created
   protected float _startTime;

   #endregion
}
