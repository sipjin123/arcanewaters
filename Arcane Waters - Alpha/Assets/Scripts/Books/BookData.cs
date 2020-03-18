using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[System.Serializable]
public class BookData {
   #region Public Variables

   // The book title
   public string title;

   // The book content (raw)
   public string content;

   // The internal bookId
   public int bookId = 0;

   #endregion

   public BookData () { }

   public BookData (string title, string content) {
      this.title = title;
      this.content = content;
   }

#if IS_SERVER_BUILD

   public BookData (MySqlDataReader reader) {
      this.title = reader.GetString("bookTitle");
      this.content = reader.GetString("bookContent");
      this.bookId = reader.GetInt32("bookId");
   }

#endif

#region Private Variables

#endregion
}
