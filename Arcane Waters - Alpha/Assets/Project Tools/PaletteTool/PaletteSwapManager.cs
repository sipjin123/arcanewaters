using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PaletteSwapManager : MonoBehaviour {
   #region Public Variables

   // Singleton reference
   public static PaletteSwapManager self;

   #endregion

   private void Awake () {
      if (self == null) {
         self = this;
      } else {
         Destroy(this);
      }
   }

   public PaletteToolData[] getPaletteData () {
      return _paletteDataList.ToArray();
   }

   public void storePaletteData (PaletteToolData[] paletteData) {
      _paletteDataList.Clear();
      foreach (PaletteToolData data in paletteData) {
         _paletteDataList.Add(data);
      }
   }

   public void fetchPaletteData () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getPaletteXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               PaletteToolData paletteData = Util.xmlLoad<PaletteToolData>(newTextAsset);

               // Save the palette data in the memory cache
               _paletteDataList.Add(paletteData);
            }
         });
      });
   }

   public static Texture2D generateTexture2D (string name) {
      if (self == null) {
         D.error("PaletteSwapManager has not been created yet");
         return null;
      }

      PaletteToolData data = _paletteDataList.Find((PaletteToolData toolData) => toolData.paletteName.Equals(name));
      if (data == null) {
         return null;
      }
      List<Color> srcColors = new List<Color>(); 
      List<Color> dstColors = new List<Color>();

      foreach (string hex in data.srcColor) {
         srcColors.Add(PaletteToolManager.convertHexToRGB(hex));
      }
      foreach (string hex in data.dstColor) {
         dstColors.Add(PaletteToolManager.convertHexToRGB(hex));
      }
      if (srcColors.Count != dstColors.Count) {
         D.error("Source and destination palette has different element count in canvas. Cannot generate Texture2D");
         return null;
      }

      Texture2D tex = new Texture2D(2, srcColors.Count);
      tex.filterMode = FilterMode.Point;
      tex.wrapMode = TextureWrapMode.Clamp;

      for (int i = 0; i < srcColors.Count; i++) {
         tex.SetPixel(0, i, srcColors[i]);
         tex.SetPixel(1, i, dstColors[i]);
      }

      tex.Apply();
      return tex;
   }

   #region Private Variables

   // Cached data from database about created palettes
   private static List<PaletteToolData> _paletteDataList = new List<PaletteToolData>();

   #endregion
}
