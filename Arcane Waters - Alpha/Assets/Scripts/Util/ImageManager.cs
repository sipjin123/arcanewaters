using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Mirror;
using System;

public class ImageManager : ClientMonoBehaviour {
   #region Public Variables

   // A struct we can use to keep track of the Image assets in our project
   [Serializable]
   public struct ImageData {
      // The name of the Image
      public string imageName;

      // The relative path to the Image
      public string imagePath;

      // The Texture for the Image
      public Texture2D texture2D;

      // The Sprite for the Image
      public Sprite sprite;

      // A list of sprite frames, if there are any
      public List<Sprite> sprites;

      public override bool Equals (object obj) {
         return obj is ImageData && Equals((ImageData) obj);
      }

      public bool Equals (ImageData other) {
         if (imageName.CompareTo(other.imageName) != 0 ||
            imagePath.CompareTo(other.imagePath) != 0 ||
            texture2D != other.texture2D ||
            sprite != other.sprite ||
            sprites.Count != other.sprites.Count)
            return false;

         for (int i = 0; i < sprites.Count; i++) {
            if (sprites[i] != other.sprites[i]) {
               return false;
            }
         }

         return true;
      }

      public override int GetHashCode () {
         return
            imageName.GetHashCode() ^
            imagePath.GetHashCode() ^
            texture2D.GetHashCode() ^
            sprite.GetHashCode() ^
            sprites.GetHashCode();
      }
   }

   // A reference to a blank sprite for null values
   public Sprite blankSprite;

   // A reference to a blank texture for null values
   public Texture2D blankTexture;

   // Path containing all project sprites
   public static string SPRITES_PATH = "Assets/Resources/Sprites";

   // Path containing filepaths for texture resources
   public static string FILEPATH_FOLDER = "Assets/Resources/Filepaths";

   // Self
   public static ImageManager self;

   #endregion

   protected override void Awake () {
      if (self == null) {
         base.Awake();

         // Store a self reference
         self = this;
         _dataByTexture = new Dictionary<Texture2D, Sprite[]>();
         _dataByPath = new Dictionary<string, List<ImageData>>();
      }
   }

   public static Sprite getSprite (string path) {
      Sprite fetchedSprite = self.getSpriteFromPath(path);

      // Returns a blank sprite if the fetched data from the path is null
      if (fetchedSprite == null) {
         if (!Util.isBatch()) {
            D.debug("Could not find sprite at path(" + path + "). Returning a blank sprite");
         }
         return self.blankSprite;
      }
      return fetchedSprite;
   }

   public static Texture2D getTexture (string path, bool warnOnNull=true) {
      Texture2D fetchedTexture = self.getTextureFromPath(path.ToLowerInvariant(), warnOnNull);

      // Returns a blank texture if the fetched data from the path is null
      if (fetchedTexture == null) {
         if (warnOnNull) {
            if (!Util.isBatch()) {
               D.debug("Could not find Texture at path(" + path + "). Returning a blank texture");
            }
         }
         return self.blankTexture;
      }
      return fetchedTexture;
   }

   public static Sprite[] getSprites (Texture2D texture) {
      Sprite[] fetchedSprites = self.getSpritesFromTexture(texture);

      // Returns a blank sprite if the fetched data from the path is null
      if (fetchedSprites == null) {
         if (!Util.isBatch()) {
            D.debug("Could not retrieve sprites from Texture(" + texture?.name + "). Returning a blank sprite array");
         }
         return new Sprite[] { self.blankSprite };
      }
      return fetchedSprites;
   }

   public static List<ImageData> getSpritesInDirectory (string path) {
      path = self.getResourcePath(path);

      if (_dataByPath.ContainsKey(path)) {
         return _dataByPath[path];
      } else {
         // Avoid storing data for batch server
         if (Util.isBatch()) {
            List<ImageData> batchList = new List<ImageData>();

            ImageData dataForBatch = new ImageData();
            dataForBatch.sprite = self.blankSprite;
            dataForBatch.texture2D = self.blankTexture;
            dataForBatch.sprites = new List<Sprite>();
            for (int i = 0; i < 10; i++) {
               dataForBatch.sprites.Add(self.blankSprite);
               batchList.Add(dataForBatch);
            }

            return batchList;
         }

         List<ImageData> imageData = new List<ImageData>();
         Texture2D[] textureList = Resources.LoadAll<Texture2D>(path);
         if (textureList == null) {
            return new List<ImageData>();
         }

         foreach (Texture2D tex in textureList) {
            ImageData data = new ImageData();

            data.imageName = tex.name;
            data.imagePath = path + tex.name;

            data.texture2D = tex;
            if (data.texture2D == null) {
               continue;
            }
            data.sprites = self.getSpritesFromTexture(data.texture2D).ToList();
            if (data.sprites.Count > 0) {
               data.sprite = data.sprites[0];
            }

            imageData.Add(data);
         }

         _dataByPath.Add(path, imageData);
         if (_dataByPath.ContainsKey(path)) {
            return _dataByPath[path];
         }
      }

      return new List<ImageData>();
   }

   public static Sprite[] getSprites (string path) {
      Sprite[] fetchedSprites = self.getSpritesFromPath(path);

      // Returns a blank sprite if the fetched data from the path is null
      if (fetchedSprites == null) {
         if (!Util.isBatch()) {
            D.debug("Could not find sprites at path(" + path + "). Returning a blank sprite array");
         }
         return new Sprite[] { self.blankSprite };
      }
      return fetchedSprites;
   }

   protected Sprite getSpriteFromPath (string path) {
      // Avoid using Resources.Load() on batch server
      if (Util.isBatch()) {
         return self.blankSprite;
      }

      path = getResourcePath(path);
      Sprite sprite = Resources.Load<Sprite>(path);

      if (sprite == null) {
         D.debug("Could not find sprite at path(" + path + "). Returning a blank sprite");
         return self.blankSprite;
      }
      return sprite;
   }

   protected Texture2D getTextureFromPath (string path, bool warnOnNull=true) {
      // Avoid using Resources.Load() on batch server
      if (Util.isBatch()) {
         return blankTexture;
      }

      path = getResourcePath(path);
      Texture2D tex = Resources.Load<Texture2D>(path);

      if (tex == null && warnOnNull) {
         if (!Util.isBatch()) {
            D.debug("Couldn't find texture for path: " + path);
         }
         return blankTexture;
      }
      return tex;
   }

   protected Sprite[] getSpritesFromTexture (Texture2D texture) {
      // Avoid storing data for batch server
      if (Util.isBatch()) {
         return new Sprite[10] { blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite };
      }

      if (_dataByTexture.ContainsKey(texture)) {
         return _dataByTexture[texture];
      } else {
         string path = "Filepaths/" + getHashForTexture(texture);
         TextAsset textAsset = (TextAsset) Resources.Load(path, typeof(TextAsset));
         if (textAsset == null) { 
            return new Sprite[0];
         }
         Sprite[] sprites = getSpritesFromPath(textAsset.text);
         _dataByTexture.Add(texture, sprites);

         if (_dataByTexture.ContainsKey(texture)) {
            return _dataByTexture[texture];
         }
      }
      return new Sprite[0];
   }

   protected Sprite[] getSpritesFromPath (string path) {
      // Avoid using Resources.Load() on batch server
      if (Util.isBatch()) {
         return new Sprite[10] { blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite, blankSprite };
      }

      path = getResourcePath(path);
      UnityEngine.Object[] data = Resources.LoadAll(path);
      List<Sprite> sprites = new List<Sprite>();

      foreach (UnityEngine.Object obj in data) {
         if (obj is Sprite) {
            sprites.Add((Sprite) obj);
         }
      }
      sprites.OrderBy(_ => extractInteger(_.name)).ToList();
      return sprites.ToArray();
   }

   protected string getResourcePath (string path) {
      try {
         path = path.Replace("Assets/Sprites/", "Sprites/");
         path = path.Replace("Assets/Resources/Sprites/", "Sprites/");
         if (!path.StartsWith("Sprites/")) {
            path = "Sprites/" + path;
         }
         return System.IO.Path.ChangeExtension(path, null);
      } catch {
         D.debug("Failed to return path: (" + path + ")");
         return "";
      }
   }

   public static int extractInteger (string name) {
      string newString = "";
      for (int i = name.Length - 1; i > 0; i--) {
         if (name[i] == '_') {
            break;
         }
         newString = newString.Insert(0, name[i].ToString());
      }

      try {
         return int.Parse(newString);
      } catch {
         return 0;
      }
   }

   public static string convertRGBToHex (Color color) {
      return convertIntToHex(color.r) + convertIntToHex(color.g) + convertIntToHex(color.b);
   }

   private static string convertIntToHex (float val) {
      int dec = Mathf.RoundToInt(val * 255.0f);
      return translateIntToHexLetter(dec / 16) + translateIntToHexLetter(dec % 16);
   }

   private static string translateIntToHexLetter (int val) {
      switch (val) {
         case 10: return "A";
         case 11: return "B";
         case 12: return "C";
         case 13: return "D";
         case 14: return "E";
         case 15: return "F";
      }
      return val.ToString();
   }

   public static string getHashForTexture (Texture2D tex, string name = "") {
      string hash = "";

      const int numberOfLines = 12;
      int heightStep = tex.height / numberOfLines;
      if (tex.height < numberOfLines) {
         heightStep = 1;
      }

      for (int y = 0; y < tex.height; y += heightStep) { 
         Color[] pixels = tex.GetPixels(0, y, tex.width, 1);

         double averageColorR = 0;
         double averageColorG = 0;
         double averageColorB = 0;
         foreach (Color color in pixels) {
            averageColorR += color.r * 255.0;
            averageColorG += color.g * 255.0;
            averageColorB += color.b * 255.0;
         }
         averageColorR /= (double)pixels.Length;
         averageColorG /= (double)pixels.Length;
         averageColorB /= (double)pixels.Length;

         hash += convertRGBToHex(new Color((float) averageColorR / 255.0f, (float) averageColorG / 255.0f, (float) averageColorB / 255.0f));
      }

      return System.IO.Path.ChangeExtension(tex.name != "" ? tex.name : name, null) + hash;
   }

   public static string getImageName (string imagePath) {
      string splitKey = "/";
      string[] stringGroup = imagePath.Split(new string[] { splitKey }, StringSplitOptions.None);

      string returnString = stringGroup[stringGroup.Length - 1];
      return returnString;
   }

   public static string getImagePath (string imagePath) {
      string splitKey = "/";
      string[] stringGroup = imagePath.Split(new string[] { splitKey }, StringSplitOptions.None);

      if (stringGroup.Length < 2) {
         return imagePath;
      }

      int groupMemberCount = stringGroup.Length - 1;
      List<string> newStringGroup = stringGroup.ToList();
      newStringGroup.RemoveAt(groupMemberCount - 1);
      newStringGroup.RemoveAt(groupMemberCount - 1);

      string returnString = "";
      int index = 0;
      foreach (string groups in newStringGroup) {
         string merger = index < (groupMemberCount - 1) ? splitKey : "";
         returnString += groups + merger;
      }
      return returnString;
   }

   #region Private Variables

   // Cache of our data by Texture
   protected static Dictionary<Texture2D, Sprite[]> _dataByTexture = new Dictionary<Texture2D, Sprite[]>();

   // Cache of our data by path
   protected static Dictionary<string, List<ImageData>> _dataByPath = new Dictionary<string, List<ImageData>>();

   #endregion
}