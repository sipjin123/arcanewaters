using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class ChatInputField : MonoBehaviour
{
   #region Public Variables

   // How much to increase the character limit per each item tag placed
   const int CHARACTER_LIMIT_PER_ITEM_TAG = 70;

   // Panel that this input belongs to
   public ChatPanel parentPanel = null;

   // Event called when value of the input changes
   public InputField.OnChangeEvent onValueChanged;

   // Font that is being used for local chat in the form of bubble
   public TMPro.TMP_FontAsset chatBubbleFont = null;

   // How many characters can the user type in
   public int characterLimit = 120;

   // Prefab of the item icon we place inside text
   public HoverableItemIcon itemIconPrefab = null;

   #endregion

   private void Awake () {
      _inputField = GetComponent<TMP_InputField>();
      _inputField.onValueChanged.AddListener(inputFieldValueChanged);
   }

   private void Update () {
      if (!parentPanel.shouldShowChat()) {
         return;
      }

      UpdateItemIcons();
      forceCaretOutOfItemInserts();
   }

   private void UpdateItemIcons () {
      for (int i = 0; i < _inputField.textComponent.textInfo.linkCount; i++) {
         string linkId = _inputField.textComponent.textInfo.linkInfo[i].GetLinkID();
         if (linkId.StartsWith(ChatManager.ITEM_INSERT_ID_PREFIX)) {
            if (int.TryParse(linkId.Replace(ChatManager.ITEM_INSERT_ID_PREFIX, ""), out int itemId)) {
               int firstCharIndex = _inputField.textComponent.textInfo.linkInfo[i].linkTextfirstCharacterIndex;
               int lastCharIndex = firstCharIndex + ChatManager.ITEM_INSERT_TEXT_PLACEHOLDER.Length - 1;

               Vector2 charCenter =
                  (_inputField.textComponent.textInfo.characterInfo[firstCharIndex].bottomLeft +
                  _inputField.textComponent.textInfo.characterInfo[lastCharIndex].topRight) / 2f;
               charCenter.x *= _inputField.textComponent.transform.localScale.x;

               Vector2 center = charCenter + _inputField.textComponent.rectTransform.anchoredPosition;

               if (i >= _itemIcons.Count) {
                  HoverableItemIcon itemInsert = Instantiate(itemIconPrefab, _inputField.textComponent.transform.parent);
                  itemInsert.setItemId(itemId, true);
                  _itemIcons.Add(itemInsert);
               } else if (_itemIcons[i].getItemId() != itemId) {
                  _itemIcons[i].setItemId(itemId, true);
               }

               _itemIcons[i].GetComponent<RectTransform>().anchoredPosition = center;
            }
         }
      }

      while (_itemIcons.Count > _inputField.textComponent.textInfo.linkCount) {
         Destroy(_itemIcons[_itemIcons.Count - 1].gameObject);
         _itemIcons.RemoveAt(_itemIcons.Count - 1);
      }
   }

   private void forceCaretOutOfItemInserts () {
      bool foundUpdate = false;

      for (int i = 0; i < _inputField.textComponent.textInfo.linkCount; i++) {
         if (_inputField.textComponent.textInfo.linkInfo[i].GetLinkID().StartsWith(ChatManager.ITEM_INSERT_ID_PREFIX)) {
            int firstIndex = _inputField.textComponent.textInfo.linkInfo[i].linkTextfirstCharacterIndex;
            int textLength = _inputField.textComponent.textInfo.linkInfo[i].linkTextLength;

            if (_inputField.caretPosition > firstIndex && _inputField.caretPosition < firstIndex + textLength) {
               _inputField.caretPosition = firstIndex;
               foundUpdate = true;
            }

            if (_inputField.selectionAnchorPosition > firstIndex && _inputField.selectionAnchorPosition < firstIndex + textLength) {
               _inputField.selectionAnchorPosition = firstIndex;
               foundUpdate = true;
            }

            if (_inputField.selectionFocusPosition > firstIndex && _inputField.selectionFocusPosition < firstIndex + textLength) {
               _inputField.selectionFocusPosition = firstIndex + textLength;
               foundUpdate = true;
            }
         }
      }

      if (foundUpdate) {
         _inputField.ForceLabelUpdate();
      }
   }

   private void inputFieldValueChanged (string value) {
      // User typed something in the input field

      _inputField.ForceLabelUpdate();
      int caretPos = _inputField.caretPosition;

      // Turn it into data
      string text = textViewToData(_inputField.text, out int _, out bool takenTextOutOfLinks);

      // Remove any unsupported characters that the user might've added
      text = removeUnsupportedCharacters(text);

      // Reapply the text in the input field
      _inputField.SetTextWithoutNotify(textDataToView(text, out int itemTagCount));
      _itemTagCount = itemTagCount;
      _inputField.characterLimit = characterLimit + _itemTagCount * CHARACTER_LIMIT_PER_ITEM_TAG;

      _inputField.textComponent.ForceMeshUpdate(false, true);
      if (takenTextOutOfLinks) {
         _inputField.readOnly = true;
      }
      _inputField.ForceLabelUpdate();
      StartCoroutine(CO_MoveCaretAfterFrame(caretPos));

      // Invoke the change with the sanitized data text
      onValueChanged.Invoke(text);
   }

   private IEnumerator CO_MoveCaretAfterFrame (int caretPos) {
      yield return null;
      int fromSelection = _inputField.selectionAnchorPosition;
      int toSelection = _inputField.selectionFocusPosition;

      _inputField.caretPosition = caretPos;
      selectTextPart(fromSelection, toSelection);
      _inputField.readOnly = false;
      _inputField.ForceLabelUpdate();
   }

   public void setText (string text) {
      // Since this changes the changed event, it will be caught afterwards, sanitized and formatted
      _inputField.text = text;
   }

   public void setTextWithoutNotify (string text) {
      // Remove any unsupported characters that the user might've added
      text = removeUnsupportedCharacters(text);

      // Apply the text in the input field
      _inputField.SetTextWithoutNotify(textDataToView(text, out int itemTagCount));
      _itemTagCount = itemTagCount;
      _inputField.characterLimit = characterLimit + _itemTagCount * CHARACTER_LIMIT_PER_ITEM_TAG;
   }

   public string getTextData () {
      // Remove any unsupported characters that the user might've added
      string text = removeUnsupportedCharacters(_inputField.text);

      return textViewToData(text, out int _, out bool _);
   }

   public bool isEmpty () {
      return _inputField.text.Length == 0;
   }

   private string textDataToView (string text, out int itemTagCount) {
      return ChatManager.injectItemSnippetLinks(text, out itemTagCount);
   }

   private string textViewToData (string text, out int itemTagCount, out bool takenTextOutOfLinks) {
      return ChatManager.turnItemSnippetLinksToItemTags(text, out itemTagCount, out takenTextOutOfLinks);
   }

   public bool hasItemTags () {
      return _itemTagCount > 0;
   }

   public int getItemTagCount () {
      return _itemTagCount;
   }

   private string removeUnsupportedCharacters (string text) {
      for (int i = text.Length - 1; i >= 0; i--) {
         char c = text[i];

         if (!chatBubbleFont.HasCharacter(c)) {
            text = text.Remove(i, 1);
         }
      }
      return text;
   }

   public void select () {
      _inputField.Select();
   }

   public void moveTextEnd (bool shift) {
      _inputField.MoveTextEnd(shift);
   }

   public void activateInputField () {
      _inputField.ActivateInputField();
   }

   public void deactivateInputField () {
      _inputField.DeactivateInputField();
   }

   public bool isFocused => _inputField.isFocused;
   public int caretPosition => _inputField.caretPosition;

   public void selectTextPart (int from, int to) {
      _inputField.selectionAnchorPosition = from;
      _inputField.selectionStringAnchorPosition = from;

      _inputField.selectionFocusPosition = to;
      _inputField.selectionStringFocusPosition = to;
   }

   public Color selectionColor
   {
      get { return _inputField.selectionColor; }
      set { _inputField.selectionColor = value; }
   }

   #region Private Variables

   // The actual input field we are wrapping
   private TMP_InputField _inputField = null;

   // Current amount of item tags that are placed inside the field
   private int _itemTagCount = 0;

   // Item icons we currently have instantiated
   protected List<HoverableItemIcon> _itemIcons = new List<HoverableItemIcon>();

   #endregion
}
