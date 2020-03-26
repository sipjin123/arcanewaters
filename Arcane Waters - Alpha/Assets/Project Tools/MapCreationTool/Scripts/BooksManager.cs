using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

namespace MapCreationTool
{
   public class BooksManager : MonoBehaviour
   {
      #region Public Variables

      // The singleton instance
      public static BooksManager instance;

      // A dictionary that maps the ID of the book to the actual bookData
      public Dictionary<int, BookData> idToBook = new Dictionary<int, BookData>();

      // The number of books that exist in the database
      public int booksCount { get { return _books.Count; } }

      #endregion

      private void Awake () {
         instance = this;
      }

      private void Start () {
         fetchBooks();
      }

      public SelectOption[] formSelectionOptions () {
          return _books.Select(n => new SelectOption(n.bookId.ToString(), n.title)).ToArray();
      }

      private void fetchBooks() {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            _books = DB_Main.getBooksList();
         });
      }

      #region Private Variables

      // The books collection in the database
      private List<BookData> _books;

      #endregion
   }
}