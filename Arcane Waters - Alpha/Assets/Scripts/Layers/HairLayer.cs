using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Rendering;

public class HairLayer : SpriteLayer {
   #region Public Variables

   // Whether this is the front or back hair layer
   public bool isFront = true;

   // The Type
   public enum Type
   {
      Male_Hair_1 = 101, Male_Hair_2 = 102, Male_Hair_3 = 103, Male_Hair_4 = 104, Male_Hair_5 = 105,
      Male_Hair_6 = 106, Male_Hair_7 = 107, Male_Hair_8 = 108, Male_Hair_9 = 109,

      Female_Hair_1 = 201, Female_Hair_2 = 202, Female_Hair_3 = 203, Female_Hair_4 = 204, Female_Hair_5 = 205,
      Female_Hair_6 = 206, Female_Hair_7 = 207, Female_Hair_8 = 208, Female_Hair_9 = 209, Female_Hair_10 = 210,

   }

   // Reference to the hatLayer
   public HatLayer hatLayer;

   #endregion

   private void Update () {
      // Skip update for server in batch mode
      if (Util.isBatch()) {
         return;
      }
      
      setClipmask();
      updateClipmask();
   }

   private void setClipmask () {
      if (hatLayer == null || _currentHatType == hatLayer.getType()) {
         return;
      }

      _shouldClipHair = false;

      if (!isFront || getMaterial() == null) {
         return;
      }

      _currentHatType = hatLayer.getType();

      if (_currentHatType == 0) {
         return;
      }

      Texture2D clipMask = findClipmaskTexture();

      if (clipMask == null || clipMask == ImageManager.self.blankTexture) {
         return;
      }

      _shouldClipHair = true;
      setClipmaskTexture(clipMask);
   }

   private void updateClipmask () {
      if (!_shouldClipHair) {
         setClipmaskVisibility(false);
         return;
      }

      if (getPlayer() != null && getPlayer().facing == Direction.North) {
         setClipmaskVisibility(_shouldClipHairWhenFacingNorth);
         return;
      }

      setClipmaskVisibility(true);
   }

   private Texture2D findClipmaskTexture () {
      return ImageManager.getTexture($"Hats/hat_{_currentHatType}_clipmask");
   }

   private void setClipmaskTexture(Texture2D texture = null) {
      Material mat = getMaterial();

      if (mat == null) {
         return;
      }

      mat.SetTexture("_ClipTex", texture);
   }

   private void setClipmaskVisibility (bool show = true) {
      Material mat = getMaterial();

      if (mat == null) {
         return;
      }

      mat.SetFloat("_EnableClipping", show ? 1.0f : 0.0f);
      _isHairClipped = show;
   }

   public static string getSheetName (Type newType, bool isFront) {
      // Insert "Front" or "Back" into the string name
      string[] split = newType.ToString().Split('_');
      string adjustedName = split[0] + "_" + split[1] + (isFront ? "_Front" : "_Back") + "_" + split[3];

      return adjustedName;
   }

   public void setType (Type newType) {
      _type = newType;

      // Update our Animated Sprite
      setTexture(getTexture(newType, isFront));

      HaircutData haircutData = HaircutXMLManager.self.haircutStatList.Find(_ => _.type == _type);

      if (haircutData != null) {
         _shouldClipHairWhenFacingNorth = haircutData.clipWhenFacingNorth;
      } else {
         _shouldClipHairWhenFacingNorth = false;
      }
   }

   public static Texture2D getTexture (Type hairType, bool isFront) {
      Gender.Type gender = hairType.ToString().StartsWith("Male") ? Gender.Type.Male : Gender.Type.Female;
      string subfolder = isFront ? "Front" : "Back";
      string typeString = hairType.ToString().Replace("Hair", "Hair_" + subfolder);
      string path = "Hair/" + gender + "/" + subfolder + "/" + typeString;
      Texture2D result = ImageManager.getTexture(path, false);
      if (result == null) {
         result = ImageManager.getTexture("Empty_Layer");
      }

      return result;
   }

   public static List<Type> getList (Gender.Type gender) {
      List<Type> list = new List<Type>();

      foreach (Type hairType in Enum.GetValues(typeof(Type))) {
         if (hairType.ToString().Contains(gender.ToString())) {
            list.Add(hairType);
         }
      }

      return list;
   }

   public static string computeNumber (Type hairType) {
      return hairType.ToString().Split('_')[2];
   }

   public static Gender.Type computeGender (Type hairType) {
      return hairType.ToString().ToLower().Contains("female") ? Gender.Type.Female : Gender.Type.Male;
   }

   public Type getType () {
      return _type;
   }

   public bool isHairClipped () {
      return _isHairClipped;
   }

   #region Private Variables

   // Our current type
   protected Type _type;

   // The property ID of the stencil compare function for hats
   protected int _stencilCompPropertyId;

   // Current hat type
   private int _currentHatType = 0;

   // Should clip hair?
   private bool _shouldClipHair = false;

   // Should clip when facing north?
   private bool _shouldClipHairWhenFacingNorth = true;

   // Current hair clip state
   private bool _isHairClipped = false;

   #endregion
}
