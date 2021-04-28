using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class FloatAndStop : MonoBehaviour {
   #region Public Variables

   // How long this should live
   public float lifetime = 3f;

   // How far up we should float before stopping
   public float floatHeight;

   // The associated Text (if any)
   public TextMeshProUGUI nameText;

   // The associated Text (if any)
   public TextMeshProUGUI quantityText;

   // How fast this should float up
   public static float RISE_SPEED = .005f;

   // Alters the transform using an animator instead of this script
   public bool animateTransform;

   #endregion

   void Start () {
      // Make sure we show up in front
      Util.setZ(this.transform, -.32f);

      _startTime = Time.time;
      _startPos = this.transform.position;

      // If there's text, slowly type it in
      StartCoroutine(CO_RevealText());

      // Our Canvas Group
      _canvasGroup = GetComponentInChildren<CanvasGroup>();

      // Start floating upwards
      InvokeRepeating("floatUp", 0f, .02f);

      // Destroy after a couple seconds
      Destroy(this.gameObject, lifetime);
   }

   protected void floatUp () {
      Vector3 currentPos = this.transform.position;
      float timeAlive = Time.time - _startTime;

      if (!animateTransform) {
         // Slowly move upwards
         if (currentPos.y - _startPos.y < floatHeight) {
            currentPos.y += RISE_SPEED;
            this.transform.position = currentPos;
         }
      }

      // Also fade in
      _canvasGroup.alpha = timeAlive / (lifetime /2f);
   }

   protected IEnumerator CO_RevealText () {
      if (nameText == null) {
         yield break;
      }

      // Note the current text, then clear it out
      string itemName = nameText.text;
      nameText.text = "";

      // Wait a little bit
      yield return new WaitForSeconds(1f);

      // Slowly reveal the name of the item
      AutoTyper.SlowlyRevealText(nameText, itemName);
   }

   #region Private Variables

   // The position at which we started
   protected Vector2 _startPos;

   // The time at which we were created
   protected float _startTime;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
