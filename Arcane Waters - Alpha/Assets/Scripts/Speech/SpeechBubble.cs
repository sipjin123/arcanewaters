using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class SpeechBubble : MonoBehaviour {
   #region Public Variables

   // How long the text should stick around before fading
   public float fadeDelay = 6f;

   // The various components we manage
   public TextMeshProUGUI speechText;

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // Reference to the child gameobject called container
   public GameObject speechBubbleContainer;

   // Reference to the child gameobject called background
   public GameObject speechBubbleBackground;

   // Reference to the child gameobject that holds the text
   public GameObject speechBubbleText;

   #endregion

   void Awake () {
      // Make note of our text at the start
      _lastTextString = speechText.text;

      // Start out invisible
      canvasGroup.alpha = 0f;
   }

   void Update () {
      // Check if we recently updated the text
      if (!_lastTextString.Equals(speechText.text)) {
         _lastTextChangeTime = Time.time;
      }

      // Keep track of the current string for the next frame
      _lastTextString = speechText.text;

      // Check how long has passed since we changed the text
      float timePassed = Time.time - _lastTextChangeTime;

      // Adjust the alpha of our components over time
      float targetAlpha = Util.isEmpty(speechText.text) ? 0f : 1f;
      if (timePassed > fadeDelay && !Util.isEmpty(speechText.text)) {
         targetAlpha = 1f - (timePassed - fadeDelay);
         targetAlpha = Mathf.Clamp(targetAlpha, 0f, 1f);

         if (targetAlpha == 0) {
            SpeechManager.self.resetSpeechBubble(this);
         }
      }

      canvasGroup.alpha = targetAlpha;
   }

   public void sayText (string textToSay) {
      // Start typing the text into the speech bubble
      AutoTyper.TypeText(speechText, textToSay, false);

      // Explicitly set this, in case our new text is the same size as our old text
      _lastTextChangeTime = Time.time;
   }

   #region Private Variables

   // The string that Our Text had in the last frame
   protected string _lastTextString = "";

   // The time at which the number of characters last changed
   protected float _lastTextChangeTime = float.MinValue;

   #endregion
}
