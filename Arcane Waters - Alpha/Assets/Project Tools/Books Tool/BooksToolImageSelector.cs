using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using TMPro;

public class BooksToolImageSelector : MonoBehaviour
{
   #region Public Variables

   // The gameObject that holds all images
   public Transform imagesContainer;

   // The template for each image element
   public BookListImageTemplate listImageTemplate;

   // The input field used for the content
   public TMP_InputField bookContentInputField;

   // The input field to determine the height of the image
   public InputField imageHeightField;

   #endregion

   public void initialize (TMP_SpriteAsset spriteAsset) {
      // Get the images in the "Book Images" folder
      foreach (TMP_SpriteCharacter sprite in spriteAsset.spriteCharacterTable) {
         BookListImageTemplate template = Instantiate(listImageTemplate);
         template.initialize(sprite.name, sprite.glyphIndex);
         template.transform.SetParent(imagesContainer, false); 
         template.gameObject.SetActive(true);
         template.GetComponent<Button>().onClick.AddListener(() => insertImage(sprite));
      }

      bookContentInputField.onValueChanged.AddListener((x) => {
         _lastCaretPosition = bookContentInputField.caretPosition;
      });
   }

   private void insertImage (TMP_SpriteCharacter sprite) {
      int position = _lastCaretPosition > -1 ? _lastCaretPosition : bookContentInputField.text.Length - 1;
      string text = $"<size=27><align=\"center\"><sprite={sprite.glyphIndex}>\n<sprite name=\"BorderBig\"></align></size>";
      bookContentInputField.SetTextWithoutNotify(bookContentInputField.text.Insert(position, text));
   }

   #region Private Variables

   // We keep track of the last position of the caret in the content to know where to add images
   private int _lastCaretPosition = -1;

   #endregion
}
