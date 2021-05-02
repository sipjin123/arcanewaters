using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Xml.Linq;
using System.Linq;
using System.IO;

public class UIToolTipManager : MonoBehaviour {

   #region Public Variables

   // Self
   public static UIToolTipManager self;

   // Dictionary of tooltip text
   public Dictionary<string, TooltipSqlData> toolTipDict = new Dictionary<string, TooltipSqlData>();

   // List of tool tip data fetched from xml content
   public List<TooltipSqlData> toolTipDataList = new List<TooltipSqlData>();

   // List of open tooltips
   public static List<GameObject> openTooltips;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      openTooltips = new List<GameObject>();
   }

   public void receiveZipData (List<TooltipSqlData> xmlTooltipList) {
      toolTipDataList = xmlTooltipList;
      if (toolTipDataList != null) {
         toolTipDict = listToDictionary(toolTipDataList);
      }
   }

   public Dictionary<string, TooltipSqlData> listToDictionary (List<TooltipSqlData> toolTipDataList) {
      Dictionary<string, TooltipSqlData> dict = new Dictionary<string, TooltipSqlData>();

      // Add each key to dictionary
      foreach (TooltipSqlData tooltip in toolTipDataList) {
         string dictKey = tooltip.key1 + tooltip.key2;
         dict.Add(dictKey, tooltip);
      }
      return dict;
   }

   #region Private Variables

   #endregion
}