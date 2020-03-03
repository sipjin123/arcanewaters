using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class BooksManager : MonoBehaviour
{
   #region Public Variables

   // The possible images
   public List<BookImage> bookImages;

   // Self
   public static BooksManager self;

   #endregion

   private void Awake () {
      self = this;
      _bookImages = new Dictionary<string, Sprite>();

      // Initialize a dictionary for faster searching
      foreach (BookImage img in bookImages) {
         _bookImages.Add(img.key, img.sprite);
      }
   }

   public Sprite getImage(string imageName) {
      return _bookImages[imageName];
   }

   #region Private Variables

   // A collection containing all the existing images that can be used in a book
   private Dictionary<string, Sprite> _bookImages = new Dictionary<string, Sprite>();

   #endregion
}

[System.Serializable]
public struct BookImage
{
   // The unique ID for the image
   public string key;

   // The sprite asset to be placed in the book
   public Sprite sprite;
}
