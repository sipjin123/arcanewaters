using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System;
using System.Linq;

public class PaletteSwapManager : GenericGameManager {
   #region Public Variables

   // Singleton reference
   public static PaletteSwapManager self;

   // Event that is called after data is setup
   public UnityEvent paletteCompleteEvent = new UnityEvent();

   // If the palette data was set
   public bool hasInitialized;

   // Threshold for palette swap shader
   public float colorThreshold = 0.05f;

   // The default armor palette id
   public const int DEFAULT_ARMOR_PALETTE_ID = 37;

   // The default armor palette names
   public const string DEFAULT_ARMOR_PALETTE_NAME = "armor_primary_iron";
   public const string DEFAULT_ARMOR_PALETTE_NAMES = "armor_primary_gold, armor_secondary_yellow, , ";

   #endregion

   protected override void Awake () {
      base.Awake();
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

   public static string extractPalettes (List<int> paletteIds) {
      string newPalette = "";
      foreach (int paletteEntry in paletteIds) {
         PaletteToolData paletteInfo = self.getPalette(paletteEntry);
         newPalette += paletteInfo.paletteName + ", ";
      }
      return newPalette;
   }

   public PaletteToolData getPalette (int id) {
      if (_paletteDataRegistry == null || !_paletteDataRegistry.ContainsKey(id)) {
         return null;
      }

      return _paletteDataRegistry[id];
   }

   public PaletteToolData getPaletteByTagId (int id) {
      PaletteToolData paletteData = _paletteDataList.Find(_ => _.tagId == id);
      return paletteData;
   }

   public PaletteToolData getPaletteByName (string paletteName) {
      PaletteToolData paletteData = _paletteDataList.Find(_ => _.paletteName == paletteName);
      return paletteData;
   }

   public PaletteToolData[] getPaletteData () {
      return _paletteDataList.ToArray();
   }

   public void storePaletteData (Dictionary<int, PaletteToolData> paletteData) {
      _paletteDataList.Clear();
      _paletteDataRegistry.Clear();

      foreach (var pair in paletteData) {
         _paletteDataRegistry.Add(pair.Key, pair.Value);
         _paletteDataList.Add(pair.Value);
      }
      paletteCompleteEvent.Invoke();
      hasInitialized = true;
   }

   public void fetchPaletteData () {
      _paletteDataList = new List<PaletteToolData>();
      _paletteDataRegistry = new Dictionary<int, PaletteToolData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getPaletteXML(true);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               PaletteToolData paletteData = Util.xmlLoad<PaletteToolData>(newTextAsset);

               // Save the palette data in the memory cache
               _paletteDataList.Add(paletteData);
               _paletteDataRegistry.Add(xmlPair.xmlId, paletteData);
            }
         });
      });
   }

   public static Texture2D generateTexture2D (string name) {
      return getPaletteTexture(new string[1] { name });
   }

   public static Texture2D getPaletteTexture (string[] names) {
      if (self == null) {
         D.debug("PaletteSwapManager has not been created yet");
         return null;
      }

      if (names == null || names.Length == 0) {
         return null;
      }

      // We save the last palette we created to avoid unnecessarily creating the same palette multiple times in a single frame
      if (_lastPaletteNames != null && names.All(x => _lastPaletteNames.Any(y => x == y))) {
         return _lastTexture2d;
      }

      List<Color> srcColors = new List<Color>();
      List<Color> dstColors = new List<Color>();
            
      foreach (string name in names) {
         if (name == null || name.Trim() == "") {
            continue;
         }

         PaletteToolData data = self.getPaletteList().Find((PaletteToolData toolData) => toolData.paletteName.Equals(name));
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

      Texture2D texture = Instantiate(PrefabsManager.self.textureSquare256);
      texture.filterMode = FilterMode.Point;
      texture.wrapMode = TextureWrapMode.Clamp;

      for (int i = 0; i < srcColors.Count; i++) {         
         Color source = srcColors[i];
         Color dest = dstColors[i];

         // We use the alpha value to check whether a color is valid, so we force the new color to have an alpha value greater than the threshold
         dest.a = dest.a < 0.05f ? 1 : dest.a;

         Vector2Int point = getPointForColor(source);

         texture.SetPixel(point.x, point.y, dest);
      }

      texture.Apply();

      _lastPaletteNames = new string[names.Length];
      Array.Copy(names, _lastPaletteNames, names.Length);
      _lastTexture2d = texture;

      return texture;
   }
   
   private static Vector2Int getPointForColor (Color color) {
      Vector3 vec = new Vector3(color.r * 255, color.g * 255, color.b * 255);
      Vector2 xVector = new Vector2(vec.x * vec.y, Mathf.Max(vec.z, vec.x));
      Vector2 yVector = new Vector2(vec.y * vec.z, Mathf.Max(vec.x, vec.y)); 
      
      int x = Mathf.RoundToInt(Mathf.Sqrt(xVector.magnitude));
      int y = Mathf.RoundToInt(Mathf.Sqrt(yVector.magnitude));
            
      return new Vector2Int(x, y);
   }

   public static string getColorName (string paletteName) {
      return "TEMP COLOR";
   }

   public static Color getRepresentingColor (string[] hexColors) {
      List<Color> colors = new List<Color>();
      foreach (string hex in hexColors) {
         colors.Add(PaletteToolManager.convertHexToRGB(hex));
      }

      float r = 0.0f;
      float g = 0.0f;
      float b = 0.0f;

      foreach (Color color in colors) {
         r += color.r;
         g += color.g;
         b += color.b;
      }

      return new Color(r / (float) colors.Count, g / (float) colors.Count, b / (float) colors.Count, 1.0f);
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

      return Color.magenta;
   }

   public static Color intToColor(int r, int g, int b) {
      return new Color((float) r / 255.0f, (float) g / 255.0f, (float) b / 255.0f);
   }

   public List<PaletteToolData> getPaletteList () {
      return _paletteDataList;
   }

   public static string getPaletteDisplayName (PaletteToolData paletteToolData) {
      if (paletteToolData == null || string.IsNullOrWhiteSpace(paletteToolData.paletteName)) {
         return "";
      }

      string[] tokens = paletteToolData.paletteName.Split('_');

      if (tokens == null || tokens.Length < 2) {
         return "";
      }

      string displayName = "";
      int counter = 0;

      foreach (string token in tokens) {
         if (counter > 0) {
            displayName += Util.UppercaseFirst(token) + " ";
         }
         
         counter++;
      }

      return displayName.Trim();
   }

   #region Private Variables

   // Cached data from database about created palettes
   [SerializeField]
   private protected List<PaletteToolData> _paletteDataList = new List<PaletteToolData>();

   // Stores the palette information
   [SerializeField]
   private protected Dictionary<int, PaletteToolData> _paletteDataRegistry = new Dictionary<int, PaletteToolData>();

   // Information about the last palette we created
   private static string[] _lastPaletteNames;
   private static Texture2D _lastTexture2d;

   #endregion
}