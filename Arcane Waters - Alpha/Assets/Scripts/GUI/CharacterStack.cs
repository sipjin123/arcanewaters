using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CharacterStack : MonoBehaviour
{
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

      _originalScale = this.transform.localScale;
   }

   private void Start () {
      if (_setDirectionOnStart) {
         setDirection(_direction);
      }
   }

   public void updateLayers (UserObjects userObjects, bool updatePalettes = true) {
      UserInfo info = userObjects.userInfo;
      updateLayers(info.gender, info.bodyType, info.eyesType, info.hairType, info.eyesPalettes, info.hairPalettes, userObjects.armor, userObjects.weapon, userObjects.hat);
   }

   public void updateLayers (BodyEntity entity) {
      updateLayers(entity.gender, entity.bodyType, entity.eyesType, entity.hairType, entity.eyesPalettes, entity.hairPalettes, entity.getArmorCharacteristics(), entity.getWeaponCharacteristics(), entity.getHatCharacteristics());
   }

   public void updateLayers (Gender.Type gender, BodyLayer.Type bodyType, EyesLayer.Type eyesType, HairLayer.Type hairType, string eyesPalettes, string hairPalettes, Item armor, Item weapon, Item hat) {
      bodyLayer.setType(bodyType);
      eyesLayer.setType(eyesType);
      eyesLayer.recolor(eyesPalettes);

      // Armor
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(armor.itemTypeId);

      if (armorData == null) {
         armorData = ArmorStatData.getDefaultData();
      }

      updateArmor(gender, armorData.armorType, armor.paletteNames);

      // Weapon
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);

      if (weaponData == null) {
         weaponData = WeaponStatData.getDefaultData();
      }

      updateWeapon(gender, weaponData.weaponType, weapon.paletteNames);

      // Hat
      HatStatData hatData = EquipmentXMLManager.self.getHatData(hat.itemTypeId);

      if (hatData == null) {
         hatData = HatStatData.getDefaultData();
      }

      updateHats(gender, hatData.hatType, hat.paletteNames);

      // Hair
      updateHair(hairType, hairPalettes);

      synchronizeAnimationIndexes();
   }

   public void synchronizeAnimationIndexes () {
      if (bodyLayer == null || bodyLayer.getSimpleAnimation() == null) {
         return;
      }

      int index = bodyLayer.getSimpleAnimation().getIndex();

      if (eyesLayer != null && eyesLayer.getSimpleAnimation() != null) {
         eyesLayer.getSimpleAnimation().setIndex(index);
      }

      if (armorLayer != null && armorLayer.getSimpleAnimation() != null) {
         armorLayer.getSimpleAnimation().setIndex(index);
      }

      if (hairBackLayer != null && hairBackLayer.getSimpleAnimation() != null) {
         hairBackLayer.getSimpleAnimation().setIndex(index);
      }

      if (hairFrontLayer != null && hairFrontLayer.getSimpleAnimation() != null) {
         hairFrontLayer.getSimpleAnimation().setIndex(index);
      }

      if (weaponBackLayer != null && weaponBackLayer.getSimpleAnimation() != null) {
         weaponBackLayer.getSimpleAnimation().setIndex(index);
      }

      if (weaponFrontLayer != null && weaponFrontLayer.getSimpleAnimation() != null) {
         weaponFrontLayer.getSimpleAnimation().setIndex(index);
      }

      if (hatLayer != null && hatLayer.getSimpleAnimation() != null) {
         hatLayer.getSimpleAnimation().setIndex(index);
      }
   }

   public void updateWeapon (Gender.Type gender, int weaponType, string palettes, bool updatePalettes = true) {
      if (weaponFrontLayer == null || weaponBackLayer == null) {
         return;
      }

      weaponBackLayer.setType(gender, weaponType);
      weaponFrontLayer.setType(gender, weaponType);

      weaponFrontLayer.recolor(palettes);
      weaponBackLayer.recolor(palettes);
   }

   public void updateHats (Gender.Type gender, int hatType, string palettes, bool updatePalettes = true) {
      if (hatLayer == null) {
         return;
      }

      hatLayer.setType(gender, hatType);
      hatLayer.recolor(palettes);
   }

   public void updateArmor (Gender.Type gender, int armorType, string palettes, bool updatePalettes = true) {
      if (armorLayer == null) {
         return;
      }

      armorLayer.setType(gender, armorType);
      armorLayer.recolor(palettes);
   }

   public void updateHair (HairLayer.Type hairType, string palettes) {
      hairBackLayer.setType(hairType);
      hairBackLayer.recolor(palettes);
      hairFrontLayer.setType(hairType);
      hairFrontLayer.recolor(palettes);
   }

   public void rotateDirectionClockWise () {
      Animator[] animList = GetComponentsInChildren<Animator>();

      // Set the direction for offline character
      if (GetComponentInParent<OfflineCharacter>()) {
         if (_direction == Direction.South) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 7);
            }
            setDirection(Direction.West);
         } else if (_direction == Direction.West) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 1);
            }
            setDirection(Direction.North);
         } else if (_direction == Direction.North) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 3);
            }
            setDirection(Direction.East);
         } else if (_direction == Direction.East) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 5);
            }
            setDirection(Direction.South);
         }
      } else {
         // Set the direction for online character
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
   }

   public void rotateDirectionCounterClockWise () {
      Animator[] animList = GetComponentsInChildren<Animator>();

      // Set the direction for offline character
      if (GetComponentInParent<OfflineCharacter>()) {
         if (_direction == Direction.South) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 3);
            }
            setDirection(Direction.East);
         } else if (_direction == Direction.East) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 1);
            }
            setDirection(Direction.North);
         } else if (_direction == Direction.North) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 7);
            }
            setDirection(Direction.West);
         } else if (_direction == Direction.West) {
            foreach (Animator anim in animList) {
               anim.SetInteger("facing", 5);
            }
            setDirection(Direction.South);
         }
      } else {
         // Set the direction for online character
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
      Vector3 scale = _originalScale;
      if (_direction == Direction.West) {
         scale.x *= -1;
      }

      this.transform.localScale = scale;
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

   public void setGlobalTint (Color color) {
      hairBackLayer.getRenderer().color = color;
      weaponBackLayer.getRenderer().color = color;
      bodyLayer.getRenderer().color = color;
      eyesLayer.getRenderer().color = color;
      armorLayer.getRenderer().color = color;
      hairFrontLayer.getRenderer().color = color;
      weaponFrontLayer.getRenderer().color = color;
      hatLayer.getRenderer().color = color;
   }

   #region Private Variables

   // The direction our character is facing
   [SerializeField]
   protected Direction _direction = Direction.South;

   // Whether the direction should be applied to sprites when the game starts
   [SerializeField]
   protected bool _setDirectionOnStart = false;

   // The original scale of the stack
   protected Vector3 _originalScale;

   #endregion
}
