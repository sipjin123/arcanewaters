using System.Collections.Generic;
using System;

[Serializable]
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

   // The xml id
   public int xmlId = 0;

   // The order index of the tutorial
   public int order = 0;

   // Gets set to true when the tutorial is active
   public bool isActive = true;

   #endregion

   public Tutorial3 () { }

   #region Private Variables

   #endregion
}
