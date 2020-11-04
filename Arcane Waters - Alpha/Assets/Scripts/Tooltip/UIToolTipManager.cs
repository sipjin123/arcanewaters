using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Xml.Linq;
using System.Linq;

public class UIToolTipManager : MonoBehaviour {

   #region Public Variables

   // Self
   public static UIToolTipManager self;

   // Dictionary of tooltip text
   public Dictionary<string, string> toolTipDict = new Dictionary<string, string>();

   #endregion

   private void Awake () {
      self = this;

      // Read in XML file
      loadXMLFile();
   }

   private void Start () {
      //initializeRuntimeTooltips();
   }

   private void initializeRuntimeTooltips () {
      // Find icons that require ToolTiipComponent to be added at runtime
      IEnumerable<GameObject> icons = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Icon");
      foreach (GameObject go in icons) {
         if (go.GetComponent<ToolTipComponent>() == null) {
            go.AddComponent(typeof(ToolTipComponent));
            go.GetComponent<ToolTipComponent>().tooltipPlacement = ToolTipComponent.TooltipPlacement.AutoPlacement;
         }
      }

      IEnumerable<GameObject> gemIcons = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Gem Icon");
      foreach (GameObject go in gemIcons) {
         if (go.GetComponent<ToolTipComponent>() == null) {
            go.AddComponent(typeof(ToolTipComponent));
            go.GetComponent<ToolTipComponent>().tooltipPlacement = ToolTipComponent.TooltipPlacement.AutoPlacement;
         }
      }
   }

   private void loadXMLFile () {
      // Load the xml file from disk
      XElement baseElement = XElement.Load("Assets/Resources/XMLTooltips.xml");

      // Write xml file to dictionary
      toolTipDict = xmlToDictionary("key1", "key2", "value", baseElement);
   }

   public static XElement dictToXml (Dictionary<string, string> inputDict, string elmName, string valuesName) {
      XElement outElm = new XElement(elmName);
      Dictionary<string, string>.KeyCollection keys = inputDict.Keys;

      foreach (string key in keys) {
         XElement entry = new XElement(valuesName);
         entry.Add(new XAttribute("key", key));
         entry.Add(new XAttribute("value", inputDict[key]));
         outElm.Add(entry);
      }
      return outElm;
   }

   public static Dictionary<string, string> xmlToDictionary (string key1, string key2, string value, XElement baseElm) {
      Dictionary<string, string> dict = new Dictionary<string, string>();

      foreach (XElement elm in baseElm.Elements()) {
         string dictKey = elm.Attribute(key1).Value + elm.Attribute(key2).Value;
         string dictVal = elm.Attribute(value).Value;

         dict.Add(dictKey, dictVal);
      }
      return dict;
   }

   #region Private Variables

   #endregion
}