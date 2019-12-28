using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialData
{
   // The tutorial title
   public string tutorialName;

   // The key step of the tutorial
   public Step tutorialStep;

   // Info of the tutorial
   public string tutorialDescription;

   // IconPath
   public string iconPath = "";

   // ActionType
   public ActionType actionType;

   // Count value
   public int countRequirement;

   // Determines the order of this step
   public int stepOrder;

   public TutorialData () {

   }
}