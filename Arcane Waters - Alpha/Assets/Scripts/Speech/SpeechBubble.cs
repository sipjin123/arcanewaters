using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class SpeechBubble : MonoBehaviour
{
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

   // Prefab of the item icon we place inside text
   public HoverableItemIcon itemIconPrefab = null;

   #endregion

   void Awake () {
      // Make note of our text at the start
      _lastTextString = speechText.text;

      // Start out invisible
      canvasGroup.alpha = 0f;
   }

   void Update () {
      UpdateItemIcons();

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

   private void UpdateItemIcons () {
      for (int i = 0; i < speechText.textInfo.linkCount; i++) {
         string linkId = speechText.textInfo.linkInfo[i].GetLinkID();
         if (linkId.StartsWith(ChatManager.ITEM_INSERT_ID_PREFIX)) {
            if (int.TryParse(linkId.Replace(ChatManager.ITEM_INSERT_ID_PREFIX, ""), out int itemId)) {
               int firstCharIndex = speechText.textInfo.linkInfo[i].linkTextfirstCharacterIndex;
               int lastCharIndex = firstCharIndex + ChatManager.ITEM_INSERT_TEXT_PLACEHOLDER.Length - 1;

               Vector2 charCenter =
                  (speechText.textInfo.characterInfo[firstCharIndex].bottomLeft +
                  speechText.textInfo.characterInfo[lastCharIndex].topRight) / 2f;
               charCenter.x *= speechText.transform.localScale.x;

               Vector2 center = charCenter + speechText.rectTransform.anchoredPosition;

               if (i >= _itemIcons.Count) {
                  HoverableItemIcon itemInsert = Instantiate(itemIconPrefab, speechText.transform.parent);
                  itemInsert.setItemId(itemId, false);
                  _itemIcons.Add(itemInsert);

                  // Set pivot and anchors so the item icons align
                  _itemIcons[i].GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                  _itemIcons[i].GetComponent<RectTransform>().anchorMin = new Vector2(0, 1f);
                  _itemIcons[i].GetComponent<RectTransform>().anchorMax = new Vector2(0, 1f);

                  _itemIcons[i].GetComponent<RectTransform>().sizeDelta = new Vector2(8, 8);
                  _itemIcons[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 8);
                  _itemIcons[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 8);
               }

               _itemIcons[i].GetComponent<RectTransform>().anchoredPosition = center;
            }
         }
      }
   }

   public void sayText (string textToSay) {
      // Insert item <links> into text before typing it
      textToSay = ChatManager.injectItemSnippetLinks(textToSay, out int itemTagCount);

      // Destroy any previous item inserts
      foreach (HoverableItemIcon icon in _itemIcons) {
         Destroy(icon.gameObject);
      }
      _itemIcons.Clear();

      // Start typing the text into the speech bubble
      AutoTyper.typeText(speechText, textToSay, false);

      // Explicitly set this, in case our new text is the same size as our old text
      _lastTextChangeTime = Time.time;
   }

   #region Private Variables

   // The string that Our Text had in the last frame
   protected string _lastTextString = "";

   // The time at which the number of characters last changed
   protected float _lastTextChangeTime = float.MinValue;

   // Item icons we currently have instantiated
   protected List<HoverableItemIcon> _itemIcons = new List<HoverableItemIcon>();

   #endregion
}
