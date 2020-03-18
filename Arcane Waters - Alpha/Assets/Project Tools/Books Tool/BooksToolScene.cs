using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class BooksToolScene : MonoBehaviour {
   #region Public Variables

   // The template for list elements
   public BooksToolListTemplate booksListTemplatePrefab;

   // The books tool manager
   public BooksToolManager toolManager;

   // Holds the book data panel
   public BooksToolDataPanel booksDataPanel;

   // The parent holding the books template
   public GameObject itemTemplateParent;

   // The create book button
   public Button createBookButton;

   // The button to return to main menu
   public Button mainMenuButton;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         createBookButton.gameObject.SetActive(false);
      }

      createBookButton.onClick.AddListener(createBookTemplate);
      mainMenuButton.onClick.AddListener(() => SceneManager.LoadScene(MasterToolScene.masterScene));

      toolManager.loadBooksList();
   }

   private void createBookTemplate () {
      BookData data = new BookData();
      data.title = "Undefined Book";
      data.content = "Lorem ipsum";

      BooksToolListTemplate template = GenericEntryTemplate.createGenericTemplate(booksListTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform) as BooksToolListTemplate;
      template.editButton.onClick.AddListener(() => {
         booksDataPanel.loadData(data);
         booksDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteBookDataFile(data);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateBookData(data);
      });

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void loadBookData (Dictionary<int, BookData> bookDataCollection) {
      itemTemplateParent.gameObject.DestroyChildren();

      List<BookData> sortedList = bookDataCollection.Values.ToList().OrderBy(w => w.title).ToList();

      // Create a row for each book element
      foreach (BookData bookData in sortedList) {
         BooksToolListTemplate template = GenericEntryTemplate.createGenericTemplate(booksListTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform) as BooksToolListTemplate;

         template.nameText.text = bookData.title;

         template.editButton.onClick.AddListener(() => {
            booksDataPanel.loadData(bookData);
            booksDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteBookDataFile(bookData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateBookData(bookData);
         });
         
         if (!Util.hasValidEntryName(template.nameText.text)) {
            template.setWarning();
         }

         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
