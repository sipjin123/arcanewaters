using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class TutorialData
{
   // The tutorial title
   public string tutorialName;

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

   // List of message to display for tutorial info
   public List<string> msgList = new List<string>();

   // Json raw data
   public string rawDataJson;

   // The requirement for the succession
   public RequirementType requirementType;

   // For tutorial indicators
   public string tutorialIndicatorMessage = "";
   public string tutorialIndicatorImgPath = "";

   public TutorialData () {

   }
}

public enum RequirementType
{
   None = 0,
   Area = 1,
   Item = 2,
   ReachCoord = 3
}