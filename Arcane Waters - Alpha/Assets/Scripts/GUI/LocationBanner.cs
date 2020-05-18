using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class LocationBanner : MonoBehaviour {
   #region Public Variables

   // Our canvas group
   public CanvasGroup canvasGroup;

   // The text name of this location
   public Text locationText;

   // Self
   public static LocationBanner self;

   #endregion

   public void Awake () {
      self = this;

      // Start off hidden
      canvasGroup.alpha = 0f;
   }

   public void Update () {
      float timeSinceTextSet = Time.time - _textSetTime;

      // Update the alpha
      canvasGroup.alpha += timeSinceTextSet < DISPLAY_DURATION ? Time.smoothDeltaTime * .5f : -Time.smoothDeltaTime;
   }

   public void setText (string locationName) {
      StartCoroutine(CO_ChangeText(locationName));
   }

   public void hide () {
      StopAllCoroutines();
      _textSetTime = float.MinValue;
      canvasGroup.alpha = 0f;
   }

   protected IEnumerator CO_ChangeText (string locationName) {
      // Start out hidden
      _textSetTime = float.MinValue;
      canvasGroup.alpha = 0f;

      yield return new WaitForSeconds(.25f);

      // Update the text
      locationText.text = locationName;

      yield return new WaitForSeconds(.25f);

      // Make note of the time
      _textSetTime = Time.time;
   }

   #region Private Variables

   // How long we show the name for
   protected static float DISPLAY_DURATION = 5f;

   // The time at which the text was set
   protected float _textSetTime = float.MinValue;

   #endregion
}
