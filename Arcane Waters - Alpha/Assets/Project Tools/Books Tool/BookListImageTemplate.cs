using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BookListImageTemplate : MonoBehaviour {
   #region Public Variables

   // The text displaying the name of the image
   public Text imageNameText;

   // The image showing a preview
   public Image imageSprite;
   
   #endregion

   public void initialize (string name, Sprite sprite) {
      imageNameText.text = name;
      imageSprite.sprite = sprite;
   }

   #region Private Variables
      
   #endregion
}
