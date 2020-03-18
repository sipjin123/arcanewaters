using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class BooksToolDataPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public BooksToolManager toolManager;

   // The list of every possible image
   public BooksToolImageSelector imageSelector;

   // The book reader panel for preview
   public BookReaderPanel readerPanel;
   
   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Button for preview
   public Button previewButton;

   // Caches the initial type incase it is changed
   public string startingName;

   // Selection Event
   public UnityEvent selectionChangedEvent = new UnityEvent();

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      saveButton.onClick.AddListener(() => {
         BookData itemData = getBookData();
         if (itemData != null) {
            toolManager.saveBookData(itemData);
            gameObject.SetActive(false);
         }
      });

      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadBooksList();
      });

      previewButton.onClick.AddListener(() => {
         readerPanel.gameObject.SetActive(true);
         readerPanel.show(getBookData());
      });

      imageSelector.initialize();
   }

   public void loadData (BookData bookData) {
      startingName = bookData.title;

      _bookTitle.text = bookData.title;
      _bookContent.text = bookData.content;
   }

   private BookData getBookData () {
      BookData bookData = new BookData();

      bookData.title = _bookTitle.text;
      bookData.content = _bookContent.text;

      return bookData;
   }

   #region Private Variables
#pragma warning disable 0649

   // Title of the book
   [SerializeField]
   private InputField _bookTitle;

   // Content of the book
   [SerializeField]
   private InputField _bookContent;

#pragma warning restore 0649
   #endregion

}
