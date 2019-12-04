using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public abstract class XmlManager : MonoBehaviour
{
   // Holds the xml raw data
   public List<TextAsset> textAssets;

   public virtual void loadAllXMLData () { }

   protected void loadXMLData (string folderName) {
      TextAsset[] textArray = Resources.LoadAll("Data/" + folderName, typeof(TextAsset)).Cast<TextAsset>().ToArray();
      textAssets = new List<TextAsset>(textArray);
   }

   public virtual void clearAllXMLData () { }
}