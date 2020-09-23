using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using TMPro.Examples;

public class RollingTextFade : ClientMonoBehaviour {
   #region Public Variables

   // Gets set to true once we're finished
   public bool isDone = false;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up our text component
      _textComponent = GetComponent<TMP_Text>();

      // Store the text color
      _originalColor = _textComponent.color;
   }

   private void Update () {
      // If the player clicks the mouse while we're fading in, then finish up
      if (Input.GetMouseButtonDown(0)) {
         _delayPerCharacter = .01f;
      }
   }

   public void fadeInText (string text) {
      // Clear any current coroutines that might be running
      if (_changeTextCoroutine != null) {
         StopCoroutine(_changeTextCoroutine);
      }

      // Reset the speed back to the default
      _delayPerCharacter = DEFAULT_DELAY;

      // Store the specified text
      _textComponent.text = "<line-height=120%>" + text + "</line-height>";

      // Make the text start out invisible
      _textComponent.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0f);

      // Start the coroutine to fade in the text
      if (_textComponent.gameObject.activeInHierarchy) {
         _changeTextCoroutine = StartCoroutine(AnimateVertexColors());
      }
   }

   public void finishFading () {
      if (_changeTextCoroutine != null) {
         // Clear the animation coroutine
         StopCoroutine(_changeTextCoroutine);
         _changeTextCoroutine = null;

         // Force the full reveal of the text
         _textComponent.color = _originalColor;
         _textComponent.ForceMeshUpdate();
      }
   }

   /// <summary>
   /// Method to animate vertex colors of a TMP Text object.
   /// </summary>
   /// <returns></returns>
   IEnumerator AnimateVertexColors () {
      this.isDone = false;

      if (_textComponent.gameObject.activeInHierarchy) {
         // Need to force the text object to be generated so we have valid data to work with right from the start.
         _textComponent.ForceMeshUpdate();
         yield return null;
         _textComponent.ForceMeshUpdate();

         // Store our text info and vertex colors
         TMP_TextInfo textInfo = _textComponent.textInfo;
         Color32[] newVertexColors;

         int currentCharacter = 0;
         int startingCharacterRange = currentCharacter;
         bool isRangeMax = false;

         while (!isRangeMax && _textComponent.gameObject.activeInHierarchy) {
            int characterCount = textInfo.characterCount;

            for (int i = startingCharacterRange; i < currentCharacter + 1; i++) {
               // Skip characters that are not visible
               if (!textInfo.characterInfo[i].isVisible) {
                  continue;
               }

               // Get the index of the material used by the current character.
               int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

               // Get the vertex colors of the mesh used by this text element (character or sprite).
               newVertexColors = textInfo.meshInfo[materialIndex].colors32;

               // Get the index of the first vertex used by this text element.
               int vertexIndex = textInfo.characterInfo[i].vertexIndex;

               // Set new alpha values.
               newVertexColors[vertexIndex + 0].a = 255;
               newVertexColors[vertexIndex + 1].a = 255;
               newVertexColors[vertexIndex + 2].a = 255;
               newVertexColors[vertexIndex + 3].a = 255;

               // Set new alpha values
               newVertexColors[vertexIndex + 0] = newVertexColors[vertexIndex + 0];
               newVertexColors[vertexIndex + 1] = newVertexColors[vertexIndex + 1];
               newVertexColors[vertexIndex + 2] = newVertexColors[vertexIndex + 2];
               newVertexColors[vertexIndex + 3] = newVertexColors[vertexIndex + 3];

               startingCharacterRange += 1;

               if (startingCharacterRange == characterCount) {
                  // Update mesh vertex data one last time.
                  _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                  // Note that we're done now
                  this.isDone = true;

                  yield return new WaitForSeconds(1.0f);

                  // Reset the text object back to original state.
                  _textComponent.color = _originalColor;
                  _textComponent.ForceMeshUpdate();

                  yield return new WaitForSeconds(1.0f);

                  // Reset our counters.
                  currentCharacter = 0;
                  startingCharacterRange = 0;
                  isRangeMax = true; // End the coroutine.
               }
            }

            // Upload the changed vertex colors to the Mesh.
            _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            bool wasSpaceBar = textInfo.characterInfo[currentCharacter].character == ' ';

            // Play a sound
            SoundManager.play2DClip(SoundManager.Type.Character_Type_3);

            if (currentCharacter + 1 < characterCount) currentCharacter += 1;

            if (_delayPerCharacter > 0f) {
               yield return new WaitForSeconds(wasSpaceBar ? Random.Range(_delayPerCharacter, _delayPerCharacter * 4) : Random.Range(_delayPerCharacter * 0.5f, _delayPerCharacter * 2f));
            }
         }
      }
   }

   #region Private Variables

   // Our text component
   protected TMP_Text _textComponent;

   // The original color of our text
   protected Color _originalColor;

   // How long we wait after each character is revealed
   protected float _delayPerCharacter = DEFAULT_DELAY;

   // The default wait length per character
   protected static float DEFAULT_DELAY = .02f;

   // Holds the coroutine for the changing of the text
   protected Coroutine _changeTextCoroutine;

   #endregion
}
