using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class BooksToolbar : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      _boldButton.onClick.AddListener(() => {
         addBoldTag();
      });

      _italicsButton.onClick.AddListener(() => {
         addItalicsTag();
      });

      _colorPickerButton.onClick.AddListener(() => {
         toggleColorPicker();
      });

      _colorPickerAddTagButton.onClick.AddListener(() => {
         addColorTag();
      });

      _alignLeftButton.onClick.AddListener(() => {
         addAlignLeftTag();
      });

      _alignCenterButton.onClick.AddListener(() => {
         addAlignCenterTag();
      });

      _alignRightButton.onClick.AddListener(() => {
         addAlignRightTag();
      });

      _sizeButton.onClick.AddListener(() => {
         toggleSizeSlider();
      });

      _fontSizeSlider.onApply += addSizeTag;
   }

   private void toggleSizeSlider () {
      _fontSizeSlider.gameObject.SetActive(!_fontSizeSlider.gameObject.activeInHierarchy);
   }

   private void addSizeTag (int size) {
      string tag = SIZE_TAG.Replace("X", size.ToString());
      int position = tag.IndexOf('>') + 1;

      addTag(tag, position);

      _fontSizeSlider.gameObject.SetActive(false);
   }

   private void addAlignRightTag () {
      addTag(ALIGN_RIGHT_TAG, 15);
   }

   private void addAlignCenterTag () {
      addTag(ALIGN_CENTER_TAG, 16);
   }

   private void addAlignLeftTag () {
      addTag(ALIGN_LEFT_TAG, 14);
   }

   private void addBoldTag () {
      addTag(BOLD_TAG, 3);
   }

   private void addItalicsTag () {
      addTag(ITALICS_TAG, 3);
   }

   private void toggleColorPicker () {
      _colorPicker.gameObject.SetActive(!_colorPicker.gameObject.activeInHierarchy);
   }

   private void addColorTag () {
      string tag = COLOR_TAG.Replace("X", _colorPickerHexField.text);
      addTag(tag, 15);
      _colorPicker.gameObject.SetActive(false);
   }

   private void addTag (string tag, int openTagCharacterCount) {
      int caretPosition = _contentInputField.caretPosition;
      _contentInputField.text = _contentInputField.text.Insert(caretPosition, tag);

      // Reposition the caret so it's between the tags
      _contentInputField.caretPosition += openTagCharacterCount;

      // Focus on the input field again 
      EventSystem.current.SetSelectedGameObject(_contentInputField.gameObject);
   }

   #region Private Variables

   // The book content input field
   [SerializeField]
   private TMP_InputField _contentInputField;

   // The book content input field
   [SerializeField]
   private InputField _colorPickerHexField;

   // The color picker
   [SerializeField]
   private ColorPicker _colorPicker;

   // The button to add a bold tag
   [SerializeField]
   private Button _boldButton;

   // The button to add an italics tag
   [SerializeField]
   private Button _italicsButton;

   // The button to toggle the color picker
   [SerializeField]
   private Button _colorPickerButton;
   
   // The button to add the color tag
   [SerializeField]
   private Button _colorPickerAddTagButton;

   // The align left button
   [SerializeField]
   private Button _alignLeftButton;

   // The align center button
   [SerializeField]
   private Button _alignCenterButton;

   // The align right button
   [SerializeField]
   private Button _alignRightButton;

   // The size button
   [SerializeField]
   private Button _sizeButton;

   // The size slider
   [SerializeField]
   private BookFontSizeSlider _fontSizeSlider;

   // The bold tag
   private const string BOLD_TAG = "<b></b>";

   // The italics tag
   private const string ITALICS_TAG = "<i></i>";

   // The color tag
   private const string COLOR_TAG = "<color=X></color>";

   // The align left tag
   private const string ALIGN_LEFT_TAG = "<align=\"left\"></align>";

   // The align center tag
   private const string ALIGN_CENTER_TAG = "<align=\"center\"></align>";

   // The align right tag
   private const string ALIGN_RIGHT_TAG = "<align=\"right\"></align>";

   // The size tag
   private const string SIZE_TAG = "<size=X%></size>";

   #endregion
}
