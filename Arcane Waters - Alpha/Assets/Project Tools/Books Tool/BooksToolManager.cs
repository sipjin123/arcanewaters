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

   public void saveXMLData (BookData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      StringBuilder sb = new StringBuilder();
      using (XmlWriter writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBooksXML(longString, data.title);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void overwriteData (BookData data, string nameToDelete) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      StringBuilder sb = new StringBuilder();
      using (XmlWriter writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBooksXML(longString, data.title);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            deleteBookDataFile(new BookData { title = nameToDelete });
         });
      });
   }

   public void deleteBookDataFile (BookData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteBooksXML(data.title);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _bookDataList = new Dictionary<string, BookData>();

      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getBooksXML();
         userNameData = DB_Main.getSQLDataByName(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               BookData bookData = Util.xmlLoad<BookData>(newTextAsset);

               // Save the book data in the memory cache
               if (_bookDataList.ContainsKey(bookData.title)) {
                  Debug.LogWarning("Duplicated ID: " + bookData.title);
               } else {
                  _bookDataList.Add(bookData.title, bookData);
               }
            }
            booksToolScene.loadBookData(_bookDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void duplicateXMLData (BookData data) {
      data.title = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      StringBuilder sb = new StringBuilder();

      using (XmlWriter writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBooksXML (longString, data.title);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   #region Private Variables

   // Holds the list of book data
   private Dictionary<string, BookData> _bookDataList = new Dictionary<string, BookData>();

   #endregion
}
