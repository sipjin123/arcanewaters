using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

      // The relative path to the Image, without the file extension
      public string imagePathWithoutExtension;

      // The Texture for the Image
      public Texture2D texture2D;

      // The Sprite for the Image
      public Sprite sprite;

      // A list of sprite frames, if there are any
      public List<Sprite> sprites;
   }

   // An array of data on the image assets in our project
   public List<ImageData> imageDataList = new List<ImageData>();

   // Self
   public static ImageManager self;

   #endregion

   protected override void Awake () {
      // The batchmode server doesn't need to waste memory on these images
      if (Application.isBatchMode) {
         imageDataList.Clear();
      }

      base.Awake();

      // Store a self reference
      self = this;
   }

   public static Sprite getSprite (string path) {
      return self.getSpriteFromPath(path);
   }

   public static Texture2D getTexture (string path, bool warnOnNull=true) {
      return self.getTextureFromPath(path.ToLowerInvariant(), warnOnNull);
   }

   public static Sprite[] getSprites (Texture2D texture) {
      return self.getSpritesFromTexture(texture);
   }

   public static List<ImageData> getSpritesInDirectory (string path) {
      List<ImageData> imgDataList = self.imageDataList.FindAll(_ => _.imagePath.Contains(path));

      return imgDataList;
   }

   public static Sprite[] getSprites (string path) {
      return self.getSpritesFromPath(path);
   }

   protected Sprite getSpriteFromPath (string path) {
      ImageData imageData = getData(path);

      return imageData.sprite;
   }

   protected Texture2D getTextureFromPath (string path, bool warnOnNull=true) {
      ImageData imageData = getData(path);

      if (imageData.texture2D == null && warnOnNull) {
         Debug.LogWarning("Couldn't find texture for path: " + path);
      }

      return imageData.texture2D;
   }

   protected Sprite[] getSpritesFromTexture (Texture2D texture) {
      ImageData imageData = getData(texture);

      if (imageData.sprites == null) {
         D.warning("Couldn't find image data for texture: " + texture);
         return new Sprite[0];
      }

      return imageData.sprites.ToArray();
   }

   protected Sprite[] getSpritesFromPath (string path) {
      ImageData imageData = getData(path);

      if (imageData.sprites == null) {
         return new Sprite[0];
      }

      return imageData.sprites.ToArray();
   }

   protected ImageData getData (string path) {
      string simplePath = getSimplePath(path);

      // Cache our files before we start looking them up
      if (!_hasCached) {
         cacheFiles();
      }

      if (_dataByPath.ContainsKey(simplePath)) {
         return _dataByPath[simplePath];
      }

      return new ImageData();
   }

   protected ImageData getData (Texture2D texture) {
      // Cache our files before we start looking them up
      if (!_hasCached) {
         cacheFiles();
      }

      if (_dataByTexture.ContainsKey(texture)) {
         return _dataByTexture[texture];
      }

      return new ImageData();
   }

   protected string getSimplePath (string path) {
      string editedPath = path.ToLowerInvariant();
      editedPath = editedPath.Replace("assets/sprites/", "");
      editedPath = System.IO.Path.ChangeExtension(editedPath, null);

      return editedPath;
   }

   protected void cacheFiles () {
      // Cache our Image Data for fast lookup
      foreach (ImageData imageData in imageDataList) {
         string simplePath = getSimplePath(imageData.imagePath);

         _dataByTexture[imageData.texture2D] = imageData;
         _dataByPath[simplePath] = imageData;
      }

      _hasCached = true;
   }

   #region Private Variables

   // Gets set to true after we store our cache
   protected bool _hasCached = false;

   // Cache of our data by Texture
   protected Dictionary<Texture2D, ImageData> _dataByTexture = new Dictionary<Texture2D, ImageData>();

   // Cache of our data by path
   protected Dictionary<string, ImageData> _dataByPath = new Dictionary<string, ImageData>();

   #endregion
}
