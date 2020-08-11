using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialRow3 : MonoBehaviour
{
   #region Public Variables

   // The text component displaying the title
   public Text titleText;

   // The image displayed when the tutorial is completed
   public Image completionImage;

   // The border displayed when the tutorial is selected
   public Image selectBox;

   // A reference to the tutorial
   [HideInInspector]
   public Tutorial3 tutorial = null;

   #endregion

   public void setRowForTutorial (Tutorial3 tutorial) {
      this.tutorial = tutorial;
      titleText.text = tutorial.title;
      completionImage.enabled = tutorial.isCompleted;
      selectBox.enabled = false;
   }

   public void onRowPressed () {
      TutorialManager3.self.panel.onTutorialRowPressed(this);
   }

   public void select () {
      selectBox.enabled = true;
   }

   public void deselect () {
      selectBox.enabled = false;
   }

   public void refresh (string selectedTutorialKey) {
      if (string.Equals(tutorial.key, selectedTutorialKey)) {
         select();
      } else {
         deselect();
      }
      completionImage.enabled = tutorial.isCompleted;
   }

   #region Private Variables

   #endregion
}
