using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class BookReaderPanel : Panel
{
   #region Public Variables

   // The left page text
   public BookPage leftPage;

   // The right page text
   public BookPage rightPage;

   // A default book (for testing purposes)
   public BookData defaultBook;

   #endregion

   public override void Awake () {
      _animator = GetComponent<Animator>();
   }

   public override void Update () {
      if (Input.GetButtonDown("Horizontal")) {
         if (Input.GetAxisRaw("Horizontal") > 0) {
            setNextPages();
         } else if (Input.GetAxisRaw("Horizontal") < 0) {
            setPreviousPages();
         }
      }
   }

   public override void show () {
      if (_currentBook == null) {
         Debug.LogError("A book must be set before showing this screen. Use setBook(BookData book) before calling this method or show(BookData book) instead.");
      }

      setNextPages();
      base.show();
   }

   public void show (BookData book) {
      setBook(book);
      show();
   }

   public void setBook (BookData book) {
      // Reset values from the previous book

      // -1 is the value before opening the book
      _lastPage = -1;
      _lastContentIndex = 0;
      _nextImageIndex = 0;
      leftPage.clearPage();
      rightPage.clearPage();

      // Set new book
      _currentBook = book;
      _currentBookContent = _currentBook.content;

      // Find the images in the raw content
      _contentImages = loadImagesFromContent(_currentBookContent);

      // Remove every image tag in the content
      cleanBookContent();

      // Every page from the book
      _pages = new List<PageContent>();

      // Create the content for each page
      int pageNumber = 1;
      while (_currentBookContent.Length > 0) {
         PageImageData image;
         string text = getNextPage(out image);
         _pages.Add(new PageContent(text, image, pageNumber));
         pageNumber++;
      }
   }

   private void cleanBookContent () {
      // Keep track of the removed characters so the index of the removed string matches the index of the image
      int charactersRemoved = 0;

      foreach (PageImageData image in _contentImages) {
         _currentBookContent = _currentBookContent.Remove(image.tagStartIndex - charactersRemoved, image.tagLength);
         charactersRemoved += image.tagLength;
      }
   }

   private List<PageImageData> loadImagesFromContent (string content) {
      List<PageImageData> images = new List<PageImageData>();

      // Use regular expressions to find all matches of the special image tag (e.g. [i=ImageName])
      Regex regex = new Regex("\\[i=(\\w*) height=(\\w*)\\]");
      MatchCollection matches = regex.Matches(content);

      foreach (Match match in matches) {
         // Create the PageImageData with the index at which the tag began and the image from the BookManager
         images.Add(new PageImageData(match.Index, match.Groups[0].Length, BooksManager.self.getImage(match.Groups[1].Value), int.Parse(match.Groups[2].Value)));
      }

      return images;
   }

   public override void hide () {
      base.hide();
      _currentBook = null;
      _pages.Clear();
      _contentImages.Clear();
   }

   private void setNextPages () {
      if (_lastPage + 1 < _pages.Count) {
         _lastPage++;

         // Set up left page
         leftPage.setUpPage(_pages[_lastPage]);

         if (_lastPage + 1 < _pages.Count) {
            _lastPage++;

            // Set up right page
            rightPage.setUpPage(_pages[_lastPage]);
         } else {
            rightPage.clearPage();
         }
      }
   }

   private void setPreviousPages () {
      if (_lastPage % 2 == 0) {
         // If the last page of the book was the left one we only go back 2 pages
         _lastPage -= 3;
      } else {
         // Otherwise, we go back 4 pages
         _lastPage -= 4;
      }

      // Make sure we don't go out of bounds
      _lastPage = Mathf.Max(_lastPage, -1);

      setNextPages();
   }

   private string getNextPage (out PageImageData image) {
      string pageContent = "";

      bool containsImage = rangeContainsImage(_lastContentIndex, _lastContentIndex + MAX_FULLPAGE_CHARACTERS);

      // Determine how much space can be used by text depending on the image size
      if (containsImage) {
         image = _contentImages[_nextImageIndex];
         leftPage.setImageWithHeight(image.sprite, image.height);
         _nextImageIndex++;
      } else {
         leftPage.imageContainer.gameObject.SetActive(false);
         image = null;
      }

      // Set the entire book text to the page
      leftPage.text.SetText(_currentBookContent);

      // Force VerticalLayoutGroup and TextMeshPro component to rebuild so we get accurate values
      LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) leftPage.transform);
      leftPage.text.ForceMeshUpdate();

      // Determine how many characters we can actually see
      int visibleCharacters = leftPage.text.textInfo.characterCount;
      pageContent = _currentBookContent.Substring(0, visibleCharacters);

      // If this is not the last page
      if (pageContent.Length < _currentBookContent.Length) {
         // Take spaces in mind so words don't get split between pages
         int length = pageContent.LastIndexOf(' ') + 1;
         pageContent = _currentBookContent.Substring(0, length);
      }

      // Remove used substring from the total _currentBookContent
      _currentBookContent = _currentBookContent.Remove(0, pageContent.Length);

      _lastContentIndex += pageContent.Length;

      return pageContent;
   }

   private bool rangeContainsImage (int min, int max) {
      // Check if a range within a string contains an image tag
      if (_contentImages.Count - 1 < _nextImageIndex) {
         return false;
      } else {
         return _contentImages[_nextImageIndex] != null && _contentImages[_nextImageIndex].tagStartIndex > min && _contentImages[_nextImageIndex].tagStartIndex < max;
      }
   }

   #region Private Variables

   // The book being currently read
   private BookData _currentBook;

   // The content of the current book, which is modified after being processed (e.g. to remove image tags)
   private string _currentBookContent;

   // The index of the last character shown
   private int _lastContentIndex = 0;

   // The last processed book page. -1 is the value before opening the book.
   private int _lastPage = -1;

   // The position of images in the content
   private List<PageImageData> _contentImages = new List<PageImageData>();

   // The index of the last used image in the _imageIndexes list
   private int _nextImageIndex = 0;

   // The content of each page
   private List<PageContent> _pages = new List<PageContent>();

   // Our components
   private Animator _animator;

   // The maximum number of characters that can fit a page if there are no images
   private const int MAX_FULLPAGE_CHARACTERS = 875;

   #endregion
}