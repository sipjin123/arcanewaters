using UnityEngine;

public class PageImageData
{
   #region Public Variables

   // The string index at which this image should be placed
   public int tagStartIndex;

   // The length of the tag in the content string
   public int tagLength;

   // The actual image
   public Sprite sprite;

   // The height of the image
   public int height = 100;

   #endregion

   public PageImageData (int tagStartIndex, int tagLength, Sprite imageSprite, int height) {
      this.tagStartIndex = tagStartIndex;
      this.tagLength = tagLength;
      this.sprite = imageSprite;
      this.height = height;
   }

   #region Private Variables

   #endregion
}
