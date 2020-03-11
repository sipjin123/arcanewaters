using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class BooksToolImageSelector : MonoBehaviour {
   
   #region Public Variables

   // The gameObject that holds all images
   public Transform imagesContainer;

   // The template for each image element
   public BookListImageTemplate listImageTemplate;

   // The input field used for the content
   public InputField bookContentInputField;

   // The input field to determine the height of the image
   public InputField imageHeightField;

   #endregion

   public void initialize () {
      foreach (ImageManager.ImageData image in ImageManager.self.imageDataList) {
         BookListImageTemplate template = Instantiate(listImageTemplate);
         template.initialize(image.imageName, image.sprite);
         template.transform.SetParent(imagesContainer, false);
         template.gameObject.SetActive(true);
         template.GetComponent<Button>().onClick.AddListener(() => insertImage(image));
      }

      bookContentInputField.onValueChanged.AddListener((x) => {
         _lastCaretPosition = bookContentInputField.caretPosition;
      });
   }

   private void insertImage (ImageManager.ImageData image) {
      int position = _lastCaretPosition > -1 ? _lastCaretPosition : bookContentInputField.text.Length - 1;
      string text = $"[i={image.imagePath} height={imageHeightField.text}]";
      bookContentInputField.SetTextWithoutNotify(bookContentInputField.text.Insert(position, text));
   }

   #region Private Variables

   // We keep track of the last position of the caret in the content to know where to add images
   private int _lastCaretPosition = -1;

   #endregion
}
