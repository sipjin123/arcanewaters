using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class BooksManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static BooksManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public BookData getBookData (int bookId) {      
      return _books.First(x => x.bookId == bookId);
   }

   private void fetchBook (int bookId) {
      if (_books == null || _books.Count == 0) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            _books = DB_Main.getBooksList();
         });
      }
   }
   
   #region Private Variables

   // The collection of books in the DB
   private List<BookData> _books;

   #endregion
}
