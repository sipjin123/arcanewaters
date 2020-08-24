using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System;

public class PaletteSwapManager : MonoBehaviour {
   #region Public Variables

   // Singleton reference
   public static PaletteSwapManager self;

   // Event that is called after data is setup
   public UnityEvent paletteCompleteEvent = new UnityEvent();

   // If the palette data was set
   public bool hasInitialized;

   // Threshold for palette swap shader
   public float colorThreshold = 0.05f;

   #endregion

   private void Awake () {
      if (self == null) {
         self = this;

         // In Palette Tool - load all data locally
         updateData();
      } else {
         Destroy(this);
      }
   }

   public void updateData () {
      // Only for usage inside palette tool, because data changes during single session
      if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Palette Tool")) {
         fetchPaletteData();
         paletteCompleteEvent.Invoke();
         hasInitialized = true;
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
      paletteCompleteEvent.Invoke();
      hasInitialized = true;
   }

   public void fetchPaletteData () {
      _paletteDataList = new List<PaletteToolData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getPaletteXML(true);

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
      return generateTexture2D(new string[1] { name });
   }

   public static Texture2D generateTexture2D (string[] names) {
      if (self == null) {
         D.debug("PaletteSwapManager has not been created yet");
         return null;
      }

      if (names == null || names.Length == 0) {
         return null;
      }

      List<Color> srcColors = new List<Color>();
      List<Color> dstColors = new List<Color>();
      Texture2D tex = null;
      
      foreach (string name in names) {
         if (name == null || name.Trim() == "") {
            continue;
         }

         PaletteToolData data = _paletteDataList.Find((PaletteToolData toolData) => toolData.paletteName.Equals(name));
         if (data == null) {
            continue;
         }

         if (data.srcColor.Length != data.dstColor.Length) {
            D.debug("Source and destination palette has different element count in canvas. Cannot generate Texture2D");
            continue;
         }

         for (int i = 0; i < data.srcColor.Length; i++) {
            if (data.srcColor[i] != data.dstColor[i]) {
               try {
                  Color s = PaletteToolManager.convertHexToRGB(data.srcColor[i]);
                  Color d = PaletteToolManager.convertHexToRGB(data.dstColor[i]);

                  srcColors.Add(s);
                  dstColors.Add(d);
               } catch {
                  D.debug("Failed to convert Hex to RGB");
               }
            }
         }
      }

      // Changing to RGBA32, mipChain = true (defualt), linear = false (default)
      if (srcColors.Count <= 128) {
         tex = Instantiate(PrefabsManager.self.texturePrefab128);
      } else if (srcColors.Count <= 256) {
         tex = Instantiate(PrefabsManager.self.texturePrefab256);
      } else if (srcColors.Count <= 512) {
         tex = Instantiate(PrefabsManager.self.texturePrefab512);
      } else {
         D.debug("Palette texture is too small to handle all colors downloaded from database");
         return null;
      }
      tex.filterMode = FilterMode.Point;
      tex.wrapMode = TextureWrapMode.Clamp;

      for (int i = 0; i < srcColors.Count; i++) {
         tex.SetPixel(0, i, srcColors[i]);
         tex.SetPixel(1, i, dstColors[i]);
      }

      tex.Apply();
      return tex;
   }

   public static string getColorName (string paletteName) {
      return "TEMP COLOR";
   }

   public static Color getRepresentingColor (string paletteName) {
      string p = paletteName;
      if (p == PaletteDef.Eyes.Black ) {
         return Color.black;
      } else if (p == PaletteDef.Eyes.Blue) {
         return intToColor(0, 102, 255);
      } else if (p == PaletteDef.Eyes.Brown) {
         return intToColor(102, 51, 0);
      } else if (p == PaletteDef.Eyes.Green) {
         return intToColor(0, 153, 51);
      } else if (p == PaletteDef.Eyes.Purple) {
         return intToColor(102, 0, 255);
      }

      if (p == PaletteDef.Hair.Yellow) {
         return intToColor(255, 204, 0);
      } else if (p == PaletteDef.Hair.Red) {
         return intToColor(179, 0, 0);
      } else if (p == PaletteDef.Hair.Brown) {
         return intToColor(102, 51, 0);
      } else if (p == PaletteDef.Hair.Blue) {
         return intToColor(0, 102, 255);
      } else if (p == PaletteDef.Hair.Black) {
         return Color.black;
      } else if (p == PaletteDef.Hair.White) {
         return Color.white;
      }

      if (p == PaletteDef.Armor1.Yellow) {
         return intToColor(255, 204, 0);
      } else if (p == PaletteDef.Armor1.Red) {
         return intToColor(179, 0, 0);
      } else if (p == PaletteDef.Armor1.Brown) {
         return intToColor(102, 51, 0);
      } else if (p == PaletteDef.Armor1.Blue) {
         return intToColor(0, 102, 255);
      } else if (p == PaletteDef.Armor1.Teal) {
         return intToColor(102, 255, 255);
      } else if (p == PaletteDef.Armor1.White) {
         return Color.white;
      } else if (p == PaletteDef.Armor1.Green) {
         return intToColor(0, 153, 51);
      }

      if (p == PaletteDef.Armor2.Yellow) {
         return intToColor(255, 204, 0);
      } else if (p == PaletteDef.Armor2.Red) {
         return intToColor(179, 0, 0);
      } else if (p == PaletteDef.Armor2.Brown) {
         return intToColor(102, 51, 0);
      } else if (p == PaletteDef.Armor2.Blue) {
         return intToColor(0, 102, 255);
      } else if (p == PaletteDef.Armor2.Teal) {
         return intToColor(102, 255, 255);
      } else if (p == PaletteDef.Armor2.White) {
         return Color.white;
      } else if (p == PaletteDef.Armor2.Green) {
         return intToColor(0, 153, 51);
      }

      if (PaletteDef.guildIcon1.ContainsKey(p)) {
         return PaletteDef.guildIcon1[p];
      }

      if (PaletteDef.guildIcon2.ContainsKey(p)) {
         return PaletteDef.guildIcon2[p];
      }

      return Color.magenta;
   }

   public static Color intToColor(int r, int g, int b) {
      return new Color((float) r / 255.0f, (float) g / 255.0f, (float) b / 255.0f);
   }

   #region Private Variables

   // Cached data from database about created palettes
   private static List<PaletteToolData> _paletteDataList = new List<PaletteToolData>();

   #endregion
}