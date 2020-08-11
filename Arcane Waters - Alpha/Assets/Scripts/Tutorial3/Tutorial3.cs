using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Tutorial3
{
   #region Public Variables

   // The unique key identifying the tutorial
   public string key;

   // The title of the tutorial
   public string title;

   // The steps of the tutorial
   public List<TutorialStep3> steps;

   // Gets set to true when the tutorial is completed
   public bool isCompleted = false;

   #endregion

   public Tutorial3 (string key, string title, List<TutorialStep3> steps) {
      this.key = key;
      this.title = title;
      this.steps = steps;
   }

   #region Private Variables

   #endregion
}
