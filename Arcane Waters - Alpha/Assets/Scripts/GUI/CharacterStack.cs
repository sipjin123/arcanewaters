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

   #endregion

   private void Awake () {
      // We need instances of our materials so that the colors are unique for this object
      foreach (Image image in GetComponentsInChildren<Image>()) {
         image.material = Instantiate(image.material);
      }
   }

   public void updateLayers (UserObjects userObjects) {
      UserInfo info = userObjects.userInfo;
      WeaponStatData weaponData = UserEquipmentCache.self.getWeaponDataByEquipmentID(userObjects.weapon.type);

      bodyLayer.setType(info.bodyType);
      eyesLayer.setType(info.eyesType);
      eyesLayer.recolor(info.eyesColor1, info.eyesColor1);
      updateHair(info.hairType, info.hairColor1, info.hairColor2);
      updateArmor(info.gender, userObjects.armor.type, userObjects.armorColor1, userObjects.armorColor2);
      updateWeapon(info.gender, weaponData == null ? 0 : weaponData.weaponType, userObjects.weaponColor1, userObjects.weaponColor2);
   }

   public void updateLayers (NetEntity entity) {
      Armor armor = entity.getArmorCharacteristics();
      Weapon weapon = entity.getWeaponCharacteristics();

      bodyLayer.setType(entity.bodyType);
      eyesLayer.setType(entity.eyesType);
      eyesLayer.recolor(entity.eyesColor1, entity.eyesColor1);
      updateHair(entity.hairType, entity.hairColor1, entity.hairColor2);
      updateArmor(entity.gender,  armor.type, armor.color1, armor.color2);
      updateWeapon(entity.gender, weapon.type, weapon.color1, weapon.color2);
   }

   public void updateWeapon (Gender.Type gender, int weaponType, ColorType color1, ColorType color2) {
      weaponBackLayer.setType(gender, weaponType);
      weaponBackLayer.recolor(color1, color2);
      weaponFrontLayer.setType(gender, weaponType);
      weaponFrontLayer.recolor(color1, color2);
   }

   public void updateArmor (Gender.Type gender, int armorType, ColorType color1, ColorType color2) {
      armorLayer.setType(gender,armorType);
      armorLayer.recolor(color1, color2);
   }

   public void updateHair (HairLayer.Type hairType, ColorType color1, ColorType color2) {
      hairBackLayer.setType(hairType);
      hairBackLayer.recolor(color1, color2);
      hairFrontLayer.setType(hairType);
      hairFrontLayer.recolor(color1, color2);
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
