using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CharacterStack : MonoBehaviour {
   #region Public Variables

   // Our layers
   public HairLayer hairBackLayer;
   public WeaponLayer weaponBackLayer;
   public BodyLayer bodyLayer;
   public EyesLayer eyesLayer;
   public ArmorLayer armorLayer;
   public HairLayer hairFrontLayer;
   public WeaponLayer weaponFrontLayer;
   public HatLayer hatLayer;

   #endregion

   private void Awake () {
      // We need instances of our materials so that the colors are unique for this object
      foreach (Image image in GetComponentsInChildren<Image>()) {
         image.material = Instantiate(image.material);
      }
   }

   public void updateLayers (UserObjects userObjects) {
      UserInfo info = userObjects.userInfo;

      bodyLayer.setType(info.bodyType);
      eyesLayer.setType(info.eyesType);
      eyesLayer.recolor(info.eyesPalette1, info.eyesPalette1);
      updateHair(info.hairType, info.hairPalette1, info.hairPalette2);

      ArmorStatData armorData = ArmorStatData.getDefaultData();
      if (userObjects.armor.data != "") {
         armorData = Util.xmlLoad<ArmorStatData>(userObjects.armor.data);
      }
      updateArmor(info.gender, armorData.armorType, armorData.palette1, armorData.palette2);

      WeaponStatData weaponData = WeaponStatData.getDefaultData();
      if (userObjects.weapon.data != "") {
         try {
            weaponData = Util.xmlLoad<WeaponStatData>(userObjects.weapon.data);
         } catch {
            D.editorLog("Failed to translate xml data!", Color.red);
            D.editorLog(userObjects.weapon.data, Color.red);
         }
      }
      updateWeapon(info.gender, weaponData.weaponType, weaponData.palette1, weaponData.palette2);

      HatStatData hatData = HatStatData.getDefaultData();
      if (userObjects.hat.data != "") {
         try {
            hatData = Util.xmlLoad<HatStatData>(userObjects.hat.data);
         } catch {
            D.editorLog("Failed to translate xml data!", Color.red);
            D.editorLog(userObjects.hat.data, Color.red);
         }
      }
      updateHats(info.gender, hatData.hatType, hatData.palette1, hatData.palette2);
   }

   public void updateLayers (NetEntity entity) {
      Armor armor = entity.getArmorCharacteristics();
      Weapon weapon = entity.getWeaponCharacteristics();
      Hat hat = entity.getHatCharacteristics();

      bodyLayer.setType(entity.bodyType);
      eyesLayer.setType(entity.eyesType);
      eyesLayer.recolor(entity.eyesPalette1, entity.eyesPalette1);
      updateHair(entity.hairType, entity.hairPalette1, entity.hairPalette2);
      updateArmor(entity.gender,  armor.itemTypeId, armor.paletteName1, armor.paletteName2);
      updateWeapon(entity.gender, weapon.itemTypeId, weapon.paletteName1, weapon.paletteName2);
      updateHats(entity.gender, hat.itemTypeId, hat.paletteName1, hat.paletteName2);
   }

   public void updateWeapon (Gender.Type gender, int weaponType, string palette1, string palette2) {
      weaponBackLayer.setType(gender, weaponType);
      weaponBackLayer.recolor(palette1, palette2);
      weaponFrontLayer.setType(gender, weaponType);
      weaponFrontLayer.recolor(palette1, palette2);
   }

   public void updateHats (Gender.Type gender, int hatType, string palette1, string palette2) {
      if (hatLayer != null) {
         hatLayer.setType(gender, hatType);
         hatLayer.recolor(palette1, palette2);
      } else {
         D.editorLog("Hat Layer is not set!", Color.red);
      }
   }

   public void updateArmor (Gender.Type gender, int armorType, string palette1, string palette2) {
      armorLayer.setType(gender, armorType);
      armorLayer.recolor(palette1, palette2);

      // Only process this when the user is in avatar mode and not in ship mode
      // This syncs the users color scheme to the GUI material of the character stack(Inventory Char Preview)
      if (Global.player is PlayerBodyEntity) {
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);
         PlayerBodyEntity playerEntity = Global.player as PlayerBodyEntity;

         armorLayer.recolor(playerEntity.armorManager.palette1, playerEntity.armorManager.palette2);
      }
   }

   public void updateHair (HairLayer.Type hairType, string palette1, string palette2) {
      hairBackLayer.setType(hairType);
      hairBackLayer.recolor(palette1, palette2);
      hairFrontLayer.setType(hairType);
      hairFrontLayer.recolor(palette1, palette2);
   }

   public void rotateDirectionClockWise () {
      if (_direction == Direction.South) {
         setDirection(Direction.West);
      } else if (_direction == Direction.West) {
         setDirection(Direction.North);
      } else if (_direction == Direction.North) {
         setDirection(Direction.East);
      } else if (_direction == Direction.East) {
         setDirection(Direction.South);
      }
   }

   public void rotateDirectionCounterClockWise () {
      if (_direction == Direction.South) {
         setDirection(Direction.East);
      } else if (_direction == Direction.East) {
         setDirection(Direction.North);
      } else if (_direction == Direction.North) {
         setDirection(Direction.West);
      } else if (_direction == Direction.West) {
         setDirection(Direction.South);
      }
   }

   public void setDirection (Direction direction) {
      // Save the new direction
      _direction = direction;

      // Check what the min and max indexes should be for this new orientation
      int min = getMinIndexForDirection(direction);
      int max = getMaxIndexForDirection(direction);

      // Update all of our animations
      foreach (SimpleAnimation animation in GetComponentsInChildren<SimpleAnimation>()) {
         animation.updateIndexMinMax(min, max);
      }

      // We might need to flip the sprites based on our new direction
      this.transform.localScale = (_direction == Direction.West) ? new Vector3(-2, 2, 2) : new Vector3(2, 2, 2);
   }

   public void pauseAnimation () {
      foreach (SimpleAnimation animation in GetComponentsInChildren<SimpleAnimation>()) {
         animation.isPaused = true;
      }
   }

   protected int getMinIndexForDirection (Direction direction) {
      switch (direction) {
         case Direction.East:
         case Direction.West:
            return 0;
         case Direction.North:
            return 4;
         case Direction.South:
            return 8;
      }

      return 0;
   }

   protected int getMaxIndexForDirection (Direction direction) {
      switch (direction) {
         case Direction.East:
         case Direction.West:
            return 3;
         case Direction.North:
            return 7;
         case Direction.South:
            return 11;
      }

      return 0;
   }

   #region Private Variables

   // The direction our character is facing
   protected Direction _direction = Direction.South;

   #endregion
}
