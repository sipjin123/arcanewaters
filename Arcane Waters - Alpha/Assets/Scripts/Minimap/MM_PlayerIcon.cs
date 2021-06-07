﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_PlayerIcon : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static MM_PlayerIcon self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
   }

   private void Update () {
      if (Global.player == null) {
         return;
      }

      // Keep the icon in the right position
      Area currentArea = AreaManager.self.getArea(Global.player.areaKey);

      // Physical map size is in range [-5, 5], we need to transform it to minimap space which is [-64, 64]
      const float worldToMapSpaceTransform = 64f / 5f;
      if (currentArea != null) {
         float minimapSpriteWidth = (Minimap.self.realAreaSize != Vector2Int.zero) ? Minimap.self.realAreaSize.x : Minimap.self.backgroundImage.sprite.textureRect.width;

         // Scale transformation based on current map size
         float relativePositionScale = (minimapSpriteWidth / 64f);
         Vector3 relativePosition = Global.player.transform.localPosition * worldToMapSpaceTransform / relativePositionScale;

         // For 64x64 map, there is no minimap translation
         float minimapTranslationScale = (minimapSpriteWidth - 64.0f) / 64.0f;

         // It is more suited for Minimap class but to avoid race condition and ensure correct calling sequence, it is used here
         if (minimapTranslationScale < 0.0f) {
            Minimap.self.backgroundImage.rectTransform.localPosition = Vector2.zero;
         } else {
            Minimap.self.backgroundImage.rectTransform.localPosition = new Vector2(-relativePosition.x * minimapTranslationScale, -relativePosition.y * minimapTranslationScale);
         }
         Util.setLocalXY(this.transform, relativePosition);

         // Rotate the player arrow based on our facing direction
         _image.transform.rotation = Quaternion.Euler(0, 0, getArrowRotation());
      }
   }

   protected int getArrowRotation () {
      switch (Global.player.facing) {
         case Direction.North:
            return 0;
         case Direction.NorthEast:
         case Direction.East:
         case Direction.SouthEast:
            return -90;
         case Direction.South:
            return -180;
         case Direction.SouthWest:
         case Direction.West:
         case Direction.NorthWest:
            return -270;
      }

      return 0;
   }

   public void onHoverBegin () {
      Minimap.self.displayIconInfo("You");
   }

   public void onHoverEnd () {
      Minimap.self.disableIconInfo();
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}