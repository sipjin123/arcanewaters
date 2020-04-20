using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class BookListImageTemplate : MonoBehaviour {
   #region Public Variables

   // The text displaying the name of the image
   public Text imageNameText;

   // The image showing a preview
   public TextMeshProUGUI imageSprite;
   
   #endregion

   public void initialize (string name, uint spriteIndex) {
      imageNameText.text = name;
      imageSprite.SetText($"<sprite={spriteIndex}>");
   }

   #region Private Variables
      
   #endregion
}
