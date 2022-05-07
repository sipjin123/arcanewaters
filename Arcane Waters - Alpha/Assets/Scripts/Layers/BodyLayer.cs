using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class BodyLayer : SpriteLayer {
   #region Public Variables

   // The Type of Body we have
   public enum Type {
      Male_Body_1 = 101, Male_Body_2 = 102, Male_Body_3 = 103, Male_Body_4 = 104,
      Female_Body_1 = 201, Female_Body_2 = 202, Female_Body_3 = 203, Female_Body_4 = 204,
   }

   #endregion

   public void setType (Type newBodyType) {
      _type = newBodyType;

      // Update our Animated Sprite
      Gender.Type gender = newBodyType.ToString().StartsWith("Male") ? Gender.Type.Male : Gender.Type.Female;
      string path = "Bodies/" + gender + "/" + newBodyType;
      Texture2D result = ImageManager.getTexture(path, false);
      setTexture(result);
   }

   public static List<Type> getList (Gender.Type gender) {
      List<Type> list = new List<Type>();

      foreach (Type bodyType in Enum.GetValues(typeof(Type))) {
         if (bodyType.ToString().Contains(gender.ToString())) {
            list.Add(bodyType);
         }
      }

      return list;
   }

   public Type getType () {
      return _type;
   }

   public static Type getRandomBodyTypeOfGender (Gender.Type gender) {
      List<Type> genderTypes = new List<Type>();
      Array bodyTypes = Enum.GetValues(typeof(Type));

      foreach (var item in bodyTypes) {
         var bodyType = (Type) item;

         if ((gender == Gender.Type.Male && bodyType.ToString().ToLower().StartsWith("male")) || (gender == Gender.Type.Female && bodyType.ToString().ToLower().StartsWith("female"))) {
            genderTypes.Add(bodyType);
         }
      }

      int index = Mathf.FloorToInt(UnityEngine.Random.Range(0, genderTypes.Count - 1));
      return genderTypes[index];
   }

   #region Clipmask

   private Texture2D getClipmaskAt (string clipmaskPath) {
      return ImageManager.getTexture(clipmaskPath);
   }

   public void toggleClipmask (string clipmaskPath, bool enable = true) {
      // Skip update for server in batch mode, or if the state of the clipmask hasn't changed
      if (Util.isBatch() || getMaterial() == null || _isClipmaskEnabled == enable) {
         return;
      }

      if (_lastClipTexture == null || !Util.areStringsEqual(clipmaskPath, _clipmaskPath)) {
         Texture2D clipmask = getClipmaskAt(clipmaskPath);

         if (clipmask == null || clipmask == ImageManager.self.blankTexture) {
            return;
         }

         _clipmaskPath = clipmaskPath;
         overrideClipmask(clipmask);
      }

      getMaterial().SetFloat("_EnableClipping", enable ? 1.0f : 0.0f);
      _isClipmaskEnabled = enable;
   }

   public bool isClipmaskEnabled () {
      return _isClipmaskEnabled;
   }

   public void overrideClipmask (Texture2D texture) {
      if (Util.isBatch() || getMaterial() == null || texture == _lastClipTexture) {
         return;
      }

      if (texture != null) {
         getMaterial().SetTexture("_ClipTex", texture);
      }

      _lastClipTexture = texture;
   }

   #endregion

   #region Private Variables

   // Our current type
   [SerializeField]
   protected Type _type;

   // Current clip state
   private bool _isClipmaskEnabled = false;

   // Reference to the current clip texture
   private Texture2D _lastClipTexture;

   // The path to last clipmask used
   private string _clipmaskPath;

   #endregion
}
