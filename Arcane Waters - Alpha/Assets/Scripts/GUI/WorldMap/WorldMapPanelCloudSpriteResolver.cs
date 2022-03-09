using System.Collections.Generic;
using UnityEngine;
using static WorldMapPanelCloud.CloudConfiguration;

public class WorldMapPanelCloudSpriteResolver : MonoBehaviour
{
   #region Public Variables

   // The base path for the folder containing the cloud sprites
   public string cloudSpritesFolder;

   // The base path for the folder containing the cloud shadow sprites
   public string cloudShadowSpritesFolder;

   // The cloud sprite prefix
   public string cloudSpritePrefix;

   // The cloud shadow sprite prefix
   public string cloudShadowSpritePrefix;

   #endregion

   public Sprite getCloudSprite (int spriteNumber) {
      Sprite[] sprites = ImageManager.getSprites(cloudSpritesFolder + cloudSpritePrefix);

      if (sprites == null) {
         return ImageManager.self.blankSprite;
      }

      return sprites[spriteNumber];
   }

   public Sprite getCloudShadowSprite (int spriteNumber) {
      Sprite[] sprites = ImageManager.getSprites(cloudShadowSpritesFolder + cloudShadowSpritePrefix);

      if (sprites == null) {
         return ImageManager.self.blankSprite;
      }

      return sprites[spriteNumber];
   }

   public int getCloudSpriteNumberByConfiguration (WorldMapPanelCloud.CloudConfiguration cc) {
      // 0 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, East, South) && WorldMapPanelCloud.hasDirections(cc, SouthEast))
         return 0;

      // 1 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, North) && WorldMapPanelCloud.hasDirections(cc, SouthEast, SouthWest))
         return 1;

      // 2 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, West, South) && WorldMapPanelCloud.hasDirections(cc,SouthWest))
         return 2;

      // 3 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, South))
         return 3;

      // 4 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsBut(cc, SouthEast))
         return 4;

      // 5 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsBut(cc, SouthWest))
         return 5;

      // 6 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, SouthEast))
         return 6;

      // 7 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, SouthWest))
         return 7;

      // 8 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, West) && WorldMapPanelCloud.hasDirections(cc, SouthEast, NorthEast))
         return 8;

      // 9 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsAll(cc))
         return 9;

      // 10 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, East) && WorldMapPanelCloud.hasDirections(cc, SouthWest, NorthWest))
         return 10;

      // 11 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, North, South))
         return 11;

      // 12 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsBut(cc, NorthEast))
         return 12;

      // 13 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsBut(cc, NorthWest))
         return 13;

      // 14 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, NorthEast))
         return 14;

      // 15 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, NorthWest))
         return 15;

      // 16 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, East, North) && WorldMapPanelCloud.hasDirections(cc, NorthEast))
         return 16;

      // 17 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, South) && WorldMapPanelCloud.hasDirections(cc, NorthWest, NorthEast))
         return 17;

      // 18 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, West, North) && WorldMapPanelCloud.hasDirections(cc, NorthWest))
         return 18;

      // 19 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, North))
         return 19;

      // 20 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, North) && WorldMapPanelCloud.hasDirections(cc, SouthWest))
         return 20;

      // 21 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, North) && WorldMapPanelCloud.hasDirections(cc, SouthEast))
         return 21;

      // 22 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, West) && WorldMapPanelCloud.hasDirections(cc, NorthEast))
         return 22;

      // 23 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, East) && WorldMapPanelCloud.hasDirections(cc, NorthWest))
         return 23;

      // 24 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, East))
         return 24;

      // 25 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, West, East))
         return 25;

      // 26 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, West))
         return 26;

      // 27 - OK
      if (WorldMapPanelCloud.cardinalsNone(cc))
         return 27;

      // 28 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, South) && WorldMapPanelCloud.hasDirections(cc, NorthWest))
         return 28;

      // 29 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, South) && WorldMapPanelCloud.hasDirections(cc, NorthEast))
         return 29;

      // 30 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, West) && WorldMapPanelCloud.hasDirections(cc, SouthEast))
         return 30;

      // 31 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, East) && WorldMapPanelCloud.hasDirections(cc, SouthWest))
         return 31;

      // 32 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, East, South) && WorldMapPanelCloud.excludesDirections(cc, SouthEast))
         return 32;

      // 33 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, North) && WorldMapPanelCloud.excludesDirections(cc, SouthEast, SouthWest))
         return 33;

      // 34 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, West, South) && WorldMapPanelCloud.excludesDirections(cc, SouthWest))
         return 34;

      // 35 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, NorthEast, SouthWest))
         return 35;

      // 36 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, West) && WorldMapPanelCloud.excludesDirections(cc, NorthEast, SouthEast))
         return 36;

      // 37 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsNone(cc))
         return 37;

      // 38 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, East) && WorldMapPanelCloud.excludesDirections(cc, NorthWest, SouthWest))
         return 38;

      // 39 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, West) && WorldMapPanelCloud.excludesDirections(cc, SouthEast, NorthEast))
         return 39;

      // 40 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, North) && WorldMapPanelCloud.excludesDirections(cc, SouthEast, SouthWest))
         return 40;

      // 41 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, NorthWest, SouthWest))
         return 41;

      // 42 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, NorthWest, NorthEast))
         return 42;

      // 43 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, East, North) && WorldMapPanelCloud.excludesDirections(cc, NorthEast))
         return 43;

      // 44 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, South) && WorldMapPanelCloud.excludesDirections(cc, NorthEast, NorthWest))
         return 44;

      // 45 - OK
      if (WorldMapPanelCloud.cardinalsOnly(cc, North, West) && WorldMapPanelCloud.excludesDirections(cc, NorthWest))
         return 45;

      // 46 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, South) && WorldMapPanelCloud.excludesDirections(cc, NorthWest, NorthEast))
         return 46;

      // 47 - OK
      if (WorldMapPanelCloud.cardinalsBut(cc, East) && WorldMapPanelCloud.excludesDirections(cc, NorthWest, SouthWest))
         return 47;

      // 48 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, SouthEast, SouthWest))
         return 48;

      // 49 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, NorthEast, SouthEast))
      return 49;

      // 50 - OK
      if (WorldMapPanelCloud.cardinalsAll(cc) && WorldMapPanelCloud.nonCardinalsOnly(cc, NorthWest, SouthEast))
      return 50;

      return 0;
   }

   public int getCloudShadowSpriteNumberByConfiguration (WorldMapPanelCloud.CloudConfiguration cc) {
      return getCloudSpriteNumberByConfiguration(cc);
   }

   #region Private Variables

   #endregion
}
