using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public class PageContent  {
   #region Public Variables
   
   // The page text
   public string content;
   
   // The page image (if it has one)
   public PageImageData image;
   
   // The page number
   public int pageNumber;

   // Whether this page has an image
   public bool hasImage;

   #endregion

   public PageContent (string content, PageImageData image, int pageNumber) {
      this.content = content;
      this.pageNumber = pageNumber;
      this.image = image;
      hasImage = image != null;
   }
   
   #region Private Variables

   #endregion
}
