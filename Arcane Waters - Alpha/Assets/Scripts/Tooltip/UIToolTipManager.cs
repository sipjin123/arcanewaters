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
   public Dictionary<string, string> toolTipDict = new Dictionary<string, string>();

   #endregion

   private void Awake () {
      self = this;

      // Read in XML file
      loadXMLFile();
   }

   private void loadXMLFile () {
      // Load the xml file from disk
      XElement baseElement = XElement.Load(Path.Combine(Application.dataPath, "StreamingAssets/XmlTexts/XMLTooltips.xml"));

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