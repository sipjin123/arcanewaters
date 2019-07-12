using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class AutoTyper : MonoBehaviour {
   #region Public Variables
   // The default amount of delay per character
   public static float CHAR_DELAY = .03f;

   // The default amount of delay per word
   public static float WORD_DELAY = .18f;

   // The amount of delay between beeps while the text is typing
   public static float BEEP_DELAY = .07f;

   #endregion

   public static void TypeText (Text textElement, string text) {
      TypeText(textElement, text, CHAR_DELAY, true);
   }

   public static void TypeText (Text textElement, string text, bool usePlaceholderSpaces) {
      TypeText(textElement, text, CHAR_DELAY, usePlaceholderSpaces);
   }

   public static void TypeText (Text textElement, string text, float characterDelay, bool usePlaceholderSpaces) {
      textElement.StopAllCoroutines();
      textElement.StartCoroutine(SetText(textElement, text, characterDelay, usePlaceholderSpaces));
   }

   public static void TypeWords (Text textElement, string text) {
      textElement.StopAllCoroutines();
      textElement.StartCoroutine(SetWords(textElement, text, WORD_DELAY));
   }

   public static void FinishText (Text textElement, string text) {
      textElement.StopAllCoroutines();
      textElement.text = text;
   }

   public static void SlowlyRevealText (Text textElement, string text) {
      textElement.StopAllCoroutines();
      textElement.text = CLEAR_START + text + CLEAR_END;

      // Slowly move the CLEAR_START forward in the text
      textElement.StartCoroutine(MoveColorClearForward(textElement));
   }

   public static void SlowlyRevealText (TextMeshProUGUI text, string msg) {
      text.GetComponent<RollingTextFade>().fadeInText(msg);
   }

   protected static IEnumerator SetText (Text textElement, string text, float characterDelay, bool usePlaceholderSpaces) {
      float lastBeepTime = 0f;
      textElement.text = "";

      // Set all of the non-space character to non-breaking spaces initially
      if (usePlaceholderSpaces) {
         for (int i = 0; i < text.Length; i++) {
            if (text[i] != ' ') {
               textElement.text += '\u00A0';
            } else {
               textElement.text += " ";
            }
         }
      }

      for (int i = 0; i < text.Length; i++) {
         float startTime = Time.realtimeSinceStartup;
         string initialText = textElement.text;
         textElement.text = text.Substring(0, i + 1);

         // Play some little beeps while the text is being shown
         if (text[i] != ' ' && text[i] != '\n') {
            if (Time.realtimeSinceStartup - lastBeepTime > BEEP_DELAY) {
               // SoundManager.play2DClip(SoundManager.Type.Blip_2, SoundManager.BEEP_VOLUME);
               lastBeepTime = Time.realtimeSinceStartup;
            }
         }

         // If we haven't reached the last character, then append the non-breaking spaces
         if (i < text.Length - 1 && usePlaceholderSpaces) {
            textElement.text += initialText.Substring(i + 1);
         }

         while (Time.realtimeSinceStartup - startTime < characterDelay) {
            yield return null;
         }
      }
   }

   protected static IEnumerator SetWords (Text textElement, string text, float wordDelay) {
      textElement.text = "";
      string[] words = text.Split(' ');

      for (int i = 0; i < words.Length; i++) {
         float startTime = Time.realtimeSinceStartup;
         textElement.text += words[i] + " ";

         while (Time.realtimeSinceStartup - startTime < wordDelay) {
            yield return null;
         }
      }
   }

   protected static IEnumerator MoveColorClearForward (Text textElement) {
      yield return new WaitForSeconds(CHAR_DELAY);

      string msg = textElement.text;

      // Check the index of the opening color tag
      int index = msg.IndexOf(CLEAR_START);

      // Remove the opening and closing color tags for now
      msg = msg.Replace(CLEAR_START, "");
      msg = msg.Replace(CLEAR_END, "");

      // If there's still room left, keep moving the color tag forward
      if (index < msg.Length) {
         msg = msg.Insert(index + 1, CLEAR_START);
         msg += CLEAR_END;

         // Update the text
         textElement.text = msg;
         SoundManager.play2DClip(SoundManager.Type.Blip_2);

         // Keep moving forward
         textElement.StartCoroutine(MoveColorClearForward(textElement));
      }
   }

   #region Private Variables

   // The color tag that begins making the letters clear (invisible)
   protected static string CLEAR_START = "<COLOR='#0000'>";

   // The color tag that stops making the letters clear (invisible)
   protected static string CLEAR_END = "</COLOR>";

   #endregion
}