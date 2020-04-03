using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SpeechManager : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating Speech Bubbles
   public SpeechBubble speechBubblePrefab;

   // Self
   public static SpeechManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void Update () {
      List<string> toRemove = new List<string>();

      // Look for any speech bubbles that should be removed
      foreach (string username in _speechBubbles.Keys) {
         SpeechBubble bubble = _speechBubbles[username];

         if (bubble.canvasGroup == null || bubble.canvasGroup.alpha <= 0f) {
            toRemove.Add(username);
         }
      }

      // Clean up any expired speech bubbles
      foreach (string username in toRemove) {
         SpeechBubble bubble = _speechBubbles[username];
         _speechBubbles.Remove(username);

         if (bubble != null) {
            Destroy(bubble.gameObject);
         }
      }
   }

   public void showSpeechBubble (NetEntity player, string message) {
      SpeechBubble speechInstance = null;

      // No need to bother with this in batch mode
      if (Util.isBatch()) {
         return;
      }

      // No point in saying empty messages
      if (Util.isEmpty(message)) {
         return;
      }

      // Either get an existing Speech Bubble, or create a new one
      if (_speechBubbles.ContainsKey(player.entityName)) {
         speechInstance = _speechBubbles[player.entityName];
      } else {
         speechInstance = createSpeechBubble(player);
      }

      if (speechInstance != null) {
         speechInstance.sayText(message);
      }
   }

   protected SpeechBubble createSpeechBubble (NetEntity player) {
      // Create a new instance from the prefab
      SpeechBubble speechBubble = Instantiate(speechBubblePrefab);

      // Set the name, parent, position, etc.
      speechBubble.name = "Canvas - Speech Bubble";
      speechBubble.transform.SetParent(player.transform);
      speechBubble.transform.localPosition = new Vector3(.85f, .25f, -5f);
      speechBubble.canvasGroup.alpha = 1f;

      // Store it in our internal list
      _speechBubbles[player.entityName] = speechBubble;

      return speechBubble;
   }

   #region Private Variables

   // Stores a mapping of Player names to their associated SpeechBubble instance
   protected Dictionary<string, SpeechBubble> _speechBubbles = new Dictionary<string, SpeechBubble>();

   #endregion
}
