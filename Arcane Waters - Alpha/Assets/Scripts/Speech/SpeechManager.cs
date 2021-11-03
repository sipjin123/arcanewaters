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
      if (player.isInvisible) {
         // Skipping speech bubble for invisible players
         return;
      }
      
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

      // Make sure the speech bubble stays on screen
      keepSpeechBubbleOnScreen(speechInstance);
   }

   public void keepSpeechBubbleOnScreen (SpeechBubble speechInstance) {
      RectTransform speechBubbleRect = speechInstance.GetComponent<RectTransform>();

      // Find the real world location of the speech bubble
      Vector3 speechBubblePos = speechBubbleRect.transform.position;

      // Find the real world corners of the speech bubble
      Vector3[] worldCorners = new Vector3[4];
      speechBubbleRect.GetWorldCorners(worldCorners);
      float speechBubbleWidth = worldCorners[3].x - worldCorners[0].x;

      // Find the camera bounds
      Vector2 screenUpperBounds = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

      // Check if speech bubble is out of bounds and move if needed
      if (speechBubblePos.x + speechBubbleWidth / 2 > screenUpperBounds.x) {
         speechBubbleRect.transform.localPosition = new Vector3(-0.85f, .25f, -5f);
         speechInstance.speechBubbleText.transform.SetParent(null);
         speechInstance.speechBubbleContainer.transform.localScale = new Vector3(-1, 1, 1);
         speechInstance.speechBubbleText.transform.SetParent(speechInstance.speechBubbleBackground.transform);
      }
   }

   public void resetSpeechBubble (SpeechBubble speechInstance) {
      // Reset the speech bubble
      RectTransform speechBubbleRect = speechInstance.GetComponent<RectTransform>();
      speechBubbleRect.transform.localPosition = new Vector3(.85f, .25f, -5f);
      speechInstance.speechBubbleText.transform.SetParent(null);
      speechInstance.speechBubbleContainer.transform.localScale = new Vector3(1, 1, 1);
      speechInstance.speechBubbleText.transform.SetParent(speechInstance.speechBubbleBackground.transform);
   }

   protected SpeechBubble createSpeechBubble (NetEntity player) {
      // Create a new instance from the prefab
      SpeechBubble speechBubble = Instantiate(speechBubblePrefab);

      Transform speechBubbleParent = player.transform;
      PlayerBodyEntity playerBody = player.getPlayerBodyEntity();
      if (playerBody) {
         speechBubbleParent = playerBody.followJumpHeight;
      }

      // Set the name, parent, position, etc.
      speechBubble.name = "Canvas - Speech Bubble";
      speechBubble.transform.SetParent(speechBubbleParent);
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
