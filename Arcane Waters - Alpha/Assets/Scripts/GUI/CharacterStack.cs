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

      _originalScale = this.transform.localScale;
   }

   private void Start () {
      if (_setDirectionOnStart) {
         setDirection(_direction);
      }
   }

   public void updateLayers (UserObjects userObjects, bool updatePalettes = true) {
      UserInfo info = userObjects.userInfo;

      bodyLayer.setType(info.bodyType);
      eyesLayer.setType(info.eyesType);
      eyesLayer.recolor(info.eyesPalettes);
      updateHair(info.hairType, info.hairPalettes);

      ArmorStatData armorData = ArmorStatData.getDefaultData();
      if (userObjects.armor.data != "" && userObjects.armor.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
         try {
            armorData = Util.xmlLoad<ArmorStatData>(userObjects.armor.data);
            if (updatePalettes) {
               if (Global.player && Global.player.GetComponent<BodyEntity>() && Global.player.GetComponent<BodyEntity>().armorManager) {
                  ArmorManager armorManager = Global.player.GetComponent<BodyEntity>().armorManager;
                  armorData.palettes = armorManager.palettes;
               }
            }
         } catch {
            D.editorLog("Failed to translate xml data!", Color.red);
            D.editorLog(userObjects.armor.data, Color.red);
         }
      } else {
         if (userObjects.armor.itemTypeId > 0) {
            armorData = EquipmentXMLManager.self.getArmorData(userObjects.armor.itemTypeId);
         }
      }
      updateArmor(info.gender, armorData.armorType, armorData.palettes, updatePalettes);

      WeaponStatData weaponData = WeaponStatData.getDefaultData();
      if (userObjects.weapon.data != "" && userObjects.weapon.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
         try {
            weaponData = Util.xmlLoad<WeaponStatData>(userObjects.weapon.data);
            if (updatePalettes) {
               if (Global.player && Global.player.GetComponent<BodyEntity>() && Global.player.GetComponent<BodyEntity>().weaponManager) {
                  WeaponManager weaponManager = Global.player.GetComponent<BodyEntity>().weaponManager;
                  weaponData.palettes = weaponManager.palettes;
               }
            }
         } catch {
            D.editorLog("Failed to translate xml data!", Color.red);
            D.editorLog(userObjects.weapon.data, Color.red);
         }
      } else {
         if (userObjects.weapon.itemTypeId > 0) {
            weaponData = EquipmentXMLManager.self.getWeaponData(userObjects.weapon.itemTypeId);
         }
      }
      updateWeapon(info.gender, weaponData.weaponType, weaponData.palettes, updatePalettes);

      HatStatData hatData = HatStatData.getDefaultData();
      if (userObjects.hat.data != "" && userObjects.hat.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
         try {
            hatData = Util.xmlLoad<HatStatData>(userObjects.hat.data);
            if (updatePalettes) {
               if (Global.player && Global.player.GetComponent<BodyEntity>() && Global.player.GetComponent<BodyEntity>().hatsManager) {
                  HatManager hatsManager = Global.player.GetComponent<BodyEntity>().hatsManager;
                  hatData.palettes = hatsManager.palettes;
               }
            }
         } catch {
            D.editorLog("Failed to translate xml data!", Color.red);
            D.editorLog(userObjects.hat.data, Color.red);
         }
      } else {
         if (userObjects.hat.itemTypeId > 0) {
            hatData = EquipmentXMLManager.self.getHatData(userObjects.hat.itemTypeId);
         }
      }
      updateHats(info.gender, hatData.hatType, hatData.palettes, updatePalettes);
   }

   public void updateLayers (NetEntity entity) {
      Armor armor = entity.getArmorCharacteristics();
      Weapon weapon = entity.getWeaponCharacteristics();
      Hat hat = entity.getHatCharacteristics();

      bodyLayer.setType(entity.bodyType);
      eyesLayer.setType(entity.eyesType);
      eyesLayer.recolor(entity.eyesPalettes);
      updateHair(entity.hairType, entity.hairPalettes);
      updateArmor(entity.gender,  armor.itemTypeId, armor.paletteNames);
      updateWeapon(entity.gender, weapon.itemTypeId, weapon.paletteNames);
      updateHats(entity.gender, hat.itemTypeId, hat.paletteNames);
   }

   public void updateWeapon (Gender.Type gender, int weaponType, string palettes, bool updatePalettes = true) {
      if (weaponFrontLayer.gameObject.activeInHierarchy) {
         weaponBackLayer.setType(gender, weaponType);
         if (updatePalettes) {
            weaponBackLayer.recolor(palettes);
         }
         weaponFrontLayer.setType(gender, weaponType);
         if (updatePalettes) {
            weaponFrontLayer.recolor(palettes);
         }
      } 
   }

   public void updateHats (Gender.Type gender, int hatType, string palettes, bool updatePalettes = true) {
      if (hatLayer != null) {
         if (hatLayer.gameObject.activeInHierarchy) {
            hatLayer.setType(gender, hatType);
            if (updatePalettes) {
               hatLayer.recolor(palettes);
            }
         } 
      } else {
         D.editorLog("Hat Layer is not set!", Color.red);
      }
   }

   public void updateArmor (Gender.Type gender, int armorType, string palettes, bool updatePalettes = true) {
      if (armorLayer.gameObject.activeInHierarchy) {
         armorLayer.setType(gender, armorType);
         if (updatePalettes) {
            armorLayer.recolor(palettes);
         }
      } 

      // Only process this when the user is in avatar mode and not in ship mode
      // This syncs the users color scheme to the GUI material of the character stack(Inventory Char Preview)
      if (Global.player is PlayerBodyEntity) {
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);
         PlayerBodyEntity playerEntity = Global.player as PlayerBodyEntity;

         armorLayer.recolor(playerEntity.armorManager.palettes);
      }
   }

   public void updateHair (HairLayer.Type hairType, string palettes) {
      hairBackLayer.setType(hairType);
      hairBackLayer.recolor(palettes);
      hairFrontLayer.setType(hairType);
      hairFrontLayer.recolor(palettes);
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
      Vector2 scale = _originalScale;
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

   #region Private Variables

   // The direction our character is facing
   [SerializeField]
   protected Direction _direction = Direction.South;

   // Whether the direction should be applied to sprites when the game starts
   [SerializeField]
   protected bool _setDirectionOnStart = false;

   // The original scale of the stack
   protected Vector2 _originalScale;

   #endregion
}
