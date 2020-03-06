using System.Collections;
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

   // The image used for the turning page animation
   public Image turnPageAnimationGameObject;

   // A default book (for testing purposes)
   public BookData defaultBook;

   // The arrow to navigate back
   public Image leftArrow;

   // The arrow to navigate forward
   public Image rightArrow;

   // Our components
   public Animator _animator;   

   #endregion

   public override void show () {
      if (_currentBook == null) {
         Debug.LogError("A book must be set before showing this screen. Use setBook(BookData book) before calling this method or show(BookData book) instead.");
      }

      // Set up the first two pages without playing the animation
      if (_pages.Count > 0) {
         leftPage.setUpPage(_pages[0]);

         if (_pages.Count > 1) {
            rightPage.setUpPage(_pages[1]);
         }

         _lastPage = 1;
      }

      // Disable the left arrow since we can't go back
      leftArrow.gameObject.SetActive(false);

      // Force disabling the turning page animation game object just in case it got enabled
      turnPageAnimationGameObject.gameObject.SetActive(false);

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

   public void setNextPages () {
      if (_lastPage + 1 < _pages.Count) {
         turnPageAnimationGameObject.gameObject.SetActive(true);
         _animator.SetTrigger("NextPages");

         _lastPage++;
         updatePages();
      }
   }

   private void updatePages () {
      leftPage.setUpPage(_pages[_lastPage]);

      if (_lastPage + 1 < _pages.Count) {
         _lastPage++;
         rightPage.setUpPage(_pages[_lastPage]);
      } else {
         rightPage.clearPage();
      }
            
      updateNavigationArrows();
   }

   private void updateNavigationArrows () {
      // Enable or disable navigation arrows depending on whether or not you can keep navigating
      leftArrow.gameObject.SetActive(_lastPage > 1);
      rightArrow.gameObject.SetActive(_lastPage + 1 < _pages.Count);
   }

   public void setPreviousPages () {
      int previousIndex = _lastPage;

      if (_lastPage % 2 == 0) {
         // If the last page of the book was the left one we only go back 2 pages
         _lastPage = Mathf.Max(_lastPage - 2, 0);
      } else {
         // Otherwise, we go back 3 pages
         _lastPage = Mathf.Max(_lastPage - 3, 0);
      }

      // Only play the animation if we actually switched pages
      if (_lastPage + 1 != previousIndex) {
         updatePages();
         turnPageAnimationGameObject.gameObject.SetActive(true);
         _animator.SetTrigger("PreviousPages");
      }
            
      // Make sure we don't go out of bounds
      _lastPage = Mathf.Max(_lastPage, 1);
   }

   private string getNextPage (out PageImageData image) {
      string pageContent = "";

      // Update the layout values that will be used for calculations
      leftPage.updateLayoutValues();

      // Calculate the max number of lines that can fit in the page
      int maxLines = 0;

      bool containsImage = rangeContainsImage(_lastContentIndex, _lastContentIndex + leftPage.maxCharactersFullPage);

      // Determine how much space can be used by text depending on the image size
      if (containsImage) {
         image = _contentImages[_nextImageIndex];

         // Since there's an image, the text container will be smaller
         leftPage.textContainerFullHeight -= image.height;

         _nextImageIndex++;
      } else {
         image = null;
      }

      // Calculate the maximum number of lines based on the height of the container
      maxLines = Mathf.FloorToInt(leftPage.textContainerFullHeight / leftPage.textLineHeight);
      
      // Calculate the max number of characters that can fit in a page
      int maxCharacters = maxLines * leftPage.maxCharactersPerLine;
      
      pageContent = _currentBookContent.Substring(0, Mathf.Min(maxCharacters, _currentBookContent.Length));

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

   public void showPages() {
      leftPage.gameObject.SetActive(true);
      rightPage.gameObject.SetActive(true);

      turnPageAnimationGameObject.gameObject.SetActive(false);
   }

   public void hidePages() {
      leftPage.gameObject.SetActive(false);
      rightPage.gameObject.SetActive(false);
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
   
   #endregion
}