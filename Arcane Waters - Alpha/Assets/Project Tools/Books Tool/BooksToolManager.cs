using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class BooksToolManager : XmlDataToolManager {
   #region Public Variables

   // Holds the main scene for the data templates
   public BooksToolScene booksToolScene;

   #endregion

   public void saveBookData (BookData data) {                 
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.upsertBook(data.content, data.title, data.bookId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadBooksList();
         });
      });
   }

   public void deleteBookDataFile (BookData data) {
      if (data.bookId > 0) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.deleteBookByID(data.bookId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               loadBooksList();
            });
         });
      }
   }

   public void loadBooksList () {
      _bookDataList = new Dictionary<int, BookData>();

      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<BookData> books = DB_Main.getBooksList();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (BookData book in books) {
               // Save the book data in the memory cache
               if (_bookDataList.ContainsKey(book.bookId)) {
                  Debug.LogWarning("Duplicated ID: " + book.bookId);
               } else {
                  _bookDataList.Add(book.bookId, book);
               }
            }
            booksToolScene.loadBookData(_bookDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void duplicateBookData (BookData data) {
      data.title = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      StringBuilder sb = new StringBuilder();

      using (XmlWriter writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.upsertBook (longString, data.title, 0);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadBooksList();
         });
      });
   }

   #region Private Variables

   // Holds the list of book data
   private Dictionary<int, BookData> _bookDataList = new Dictionary<int, BookData>();

   #endregion
}
