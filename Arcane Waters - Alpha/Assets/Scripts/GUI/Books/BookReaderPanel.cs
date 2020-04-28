using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

      _pagesFirstVisibleCharacters = new List<int>();
      _currentPageIndex = 0;

      leftPage.contentText.firstVisibleCharacter = 0;
      leftPage.contentText.SetText(_currentBookContent);

      leftPage.setPageNumber(1);
      rightPage.setPageNumber(2);

      // Force disabling the turning page animation game object just in case it got enabled
      turnPageAnimationGameObject.gameObject.SetActive(false);

      base.show();

      showPages();
   }

   public void show (BookData book) {
      setBookAndShow(book);
   }

   public void setBookAndShow (BookData book) {
      // Reset values from the previous book
      _currentPageIndex = 0;
      leftPage.clearPage();
      rightPage.clearPage();

      // Set new book
      _currentBook = book;
      _currentBookContent = _currentBook.content;

      show();
   }

   public override void hide () {
      base.hide();
      _currentBook = null;
   }

   private void updatePageNumbers () {
      StartCoroutine(CO_UpdatePageNumbers());
   }

   private IEnumerator CO_UpdatePageNumbers () {
      // We need to wait a frame to give TMPro time to update values
      yield return null;

      // Left page number = pagePairIndex * 2 + 1
      leftPage.setPageNumber(_currentPageIndex * 2 + 1);

      if (leftPage.contentText.isTextTruncated) {
         rightPage.setPageNumber(_currentPageIndex * 2 + 2);
      } else {
         rightPage.clearPage();
      }
   }

   private void updateNavigationArrows () {
      StartCoroutine(CO_UpdateNavigationArrows());
   }

   private IEnumerator CO_UpdateNavigationArrows () {
      // We need to wait a frame to give TMPro time to update values
      yield return null;

      // Enable or disable navigation arrows depending on whether or not you can keep navigating
      leftArrow.gameObject.SetActive(leftPage.contentText.firstVisibleCharacter > 0);
      rightArrow.gameObject.SetActive(rightPage.contentText.isTextTruncated);
   }

   public void setNextPages () {
      // Save current firstVisibleCharacter
      if (_pagesFirstVisibleCharacters.Count - 1 < _currentPageIndex) {
         _pagesFirstVisibleCharacters.Add(leftPage.contentText.firstVisibleCharacter);
      }

      turnPageAnimationGameObject.gameObject.SetActive(true);
      _animator.SetTrigger("NextPages");

      leftPage.contentText.firstVisibleCharacter = rightPage.contentText.firstOverflowCharacterIndex;

      _currentPageIndex++;
   }

   public void setPreviousPages () {
      _currentPageIndex--;

      turnPageAnimationGameObject.gameObject.SetActive(true);
      _animator.SetTrigger("PreviousPages");

      leftPage.contentText.firstVisibleCharacter = _pagesFirstVisibleCharacters[_currentPageIndex];
   }

   public void showPages () {
      leftPage.gameObject.SetActive(true);
      rightPage.gameObject.SetActive(true);

      turnPageAnimationGameObject.gameObject.SetActive(false);

      updateNavigationArrows();
      updatePageNumbers();
   }

   public void hidePages () {
      leftPage.gameObject.SetActive(false);
      rightPage.gameObject.SetActive(false);
      leftPage.pageNumberText.SetText("");
      rightPage.pageNumberText.SetText("");

      leftArrow.gameObject.SetActive(false);
      rightArrow.gameObject.SetActive(false);
   }

   #region Private Variables

   // The book being currently read
   private BookData _currentBook;

   // The content of the current book, which is modified after being processed (e.g. to remove image tags)
   private string _currentBookContent;

   // The page pair index
   [SerializeField]
   private int _currentPageIndex;

   // The first visible character of each page
   private List<int> _pagesFirstVisibleCharacters = new List<int>();

   #endregion
}