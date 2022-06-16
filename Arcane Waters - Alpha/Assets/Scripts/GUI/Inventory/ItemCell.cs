﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using static PaletteToolManager;

public class ItemCell : MonoBehaviour, IPointerClickHandler
{

   #region Public Variables

   // The icon of the item
   public Image icon;

   // The shadow of the icon
   public Image iconShadow;

   // The text containing the amount of stacked items
   public TextMeshProUGUI itemCountText;

   // The box visible when the cell is selected
   public Image selectedBox;

   // The tooltip showing the name of the item
   public ToolTipComponent tooltip;

   // The recolored sprite component on the icon
   public RecoloredSprite recoloredSprite;

   // The default icons, used when it is not defined by the item type
   public Sprite foodIcon;
   public Sprite defaultItemIcon;
   public Sprite disabledItemIcon;

   // The object displayed if item does not match level requirement
   public GameObject levelRestrictionObj;

   // The background image
   public Image backgroundImage;

   // The rarity of the item in the cell
   public Rarity.Type itemRarityType;

   // The click events
   public UnityEvent leftClickEvent;
   public UnityEvent rightClickEvent;
   public UnityEvent doubleClickEvent;
   public UnityEvent shiftClickEvent;
   public UnityEvent onPointerEnter;
   public UnityEvent onPointerExit;
   public UnityEvent onDragStarted;

   // Item type id
   public int itemTypeId;

   // Item sprite id
   public int itemSpriteId;

   // The cahed item data
   public Item itemCache;

   // The icon indicating this item is a blueprint
   public GameObject blueprintIcon;

   // The icon which is shown when item is a prop
   public GameObject propIcon;

   // If the item is disabled
   public bool isDisabledItem;

   // Color overlay depending on item durability
   public Color midDurabilityColor, lowDurabilityColor, highDurabilityColor;

   // Image reference of the item cell
   public Image itemCellImage;

   // The transparency of the durability notification
   public const float DURABILITY_NOTIF_ALPHA = 250;

   #endregion

   public virtual void clear () {
      itemCache = null;
      itemSpriteId = 0;
      itemTypeId = 0;
      leftClickEvent.RemoveAllListeners();
      rightClickEvent.RemoveAllListeners();
      doubleClickEvent.RemoveAllListeners();
      shiftClickEvent.RemoveAllListeners();
      onPointerEnter.RemoveAllListeners();
      onPointerExit.RemoveAllListeners();
      onDragStarted.RemoveAllListeners();
      icon.sprite = null;
      iconShadow.sprite = null;
      itemRarityType = Rarity.Type.None;

      icon.enabled = false;
      iconShadow.enabled = false;

      itemCountText.transform.parent.gameObject.SetActive(false);

      disablePointerEvents();
   }

   public virtual void setCellForItem (Item item) {
      icon.enabled = true;
      iconShadow.enabled = true;
      enablePointerEvents();

      itemCache = item;
      setCellForItem(item, item == null ? 0 : item.count);
      itemRarityType = Item.getRarity(item);
   }

   public void setCellForItem (Item item, int count) {
      string rawItemData = item.data;

      // Retrieve the icon sprite and coloring depending on the type
      switch (item.category) {
         case Item.Category.Weapon:
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            if (weaponData == null) {
               D.debug("Failed to process weapon data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.transform.parent.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               if (Global.player != null && !Global.player.isAdmin()) {
                  gameObject.SetActive(false);
               }
               return;
            } else {
               Weapon newWeapon = WeaponStatData.translateDataToWeapon(weaponData);
               newWeapon.id = item.id;
               newWeapon.paletteNames = item.paletteNames;

               // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
               string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
               if (paletteDataCount.Length < 1) {
                  newWeapon.paletteNames = PaletteSwapManager.extractPalettes(weaponData.defaultPalettes);
               }

               newWeapon.durability = item.durability;
               item = newWeapon;
               item.data = rawItemData;

               icon.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);
               itemSpriteId = weaponData.weaponType;
               itemTypeId = item.itemTypeId;
            }
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
            if (armorData == null) {
               D.debug("Failed to process armor data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.transform.parent.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               if (Global.player != null && !Global.player.isAdmin()) {
                  gameObject.SetActive(false);
               }
               return;
            } else {
               Armor newArmor = ArmorStatData.translateDataToArmor(armorData);
               newArmor.id = item.id;
               newArmor.paletteNames = item.paletteNames;

               // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
               string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
               if (paletteDataCount.Length < 1) {
                  newArmor.paletteNames = PaletteSwapManager.extractPalettes(armorData.defaultPalettes);
               }

               newArmor.durability = item.durability;
               item = newArmor;
               item.data = rawItemData;
               if (Global.player != null) {
                  Sprite newSprite = ImageManager.getSprite(armorData.equipmentIconPath + (Global.player.gender == Gender.Type.Female ? "_female" : ""));

                  if (newSprite == null || newSprite == ImageManager.self.blankSprite) {
                     newSprite = ImageManager.getSprite(armorData.equipmentIconPath);
                  }

                  icon.sprite = newSprite;
               }
               itemSpriteId = armorData.armorType;
               itemTypeId = item.itemTypeId;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
            if (hatData == null) {
               D.debug("Failed to process hat data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.transform.parent.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               if (Global.player != null && !Global.player.isAdmin()) {
                  gameObject.SetActive(false);
               }
               return;
            } else {
               Hat newHat = HatStatData.translateDataToHat(hatData);
               newHat.id = item.id;
               newHat.paletteNames = item.paletteNames;

               // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
               string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
               if (paletteDataCount.Length < 1) {
                  newHat.paletteNames = PaletteSwapManager.extractPalettes(hatData.defaultPalettes);
               }

               newHat.durability = item.durability;
               item = newHat;
               item.data = rawItemData;

               icon.sprite = ImageManager.getSprite(hatData.equipmentIconPath);
               itemSpriteId = hatData.hatType;
               itemTypeId = item.itemTypeId;
            }
            break;
         case Item.Category.Ring:
            RingStatData ringData = EquipmentXMLManager.self.getRingData(item.itemTypeId);
            if (ringData == null) {
               D.debug("Failed to process ring data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.transform.parent.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               if (Global.player != null && !Global.player.isAdmin()) {
                  gameObject.SetActive(false);
               }
               return;
            } else {
               Ring newRing = RingStatData.translateDataToRing(ringData);
               newRing.id = item.id;
               newRing.paletteNames = item.paletteNames;

               // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
               string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
               if (paletteDataCount.Length < 1) {
                  newRing.paletteNames = PaletteSwapManager.extractPalettes(ringData.defaultPalettes);
               }

               newRing.durability = item.durability;
               item = newRing;
               item.data = rawItemData;

               icon.sprite = ImageManager.getSprite(ringData.equipmentIconPath);
               itemSpriteId = ringData.ringType;
               itemTypeId = item.itemTypeId;
            }
            break;
         case Item.Category.Necklace:
            NecklaceStatData necklaceData = EquipmentXMLManager.self.getNecklaceData(item.itemTypeId);
            if (necklaceData == null) {
               D.debug("Failed to process necklace data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.transform.parent.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               if (Global.player != null && !Global.player.isAdmin()) {
                  gameObject.SetActive(false);
               }
               return;
            } else {
               Necklace newNecklace = NecklaceStatData.translateDataToNecklace(necklaceData);
               newNecklace.id = item.id;
               newNecklace.paletteNames = item.paletteNames;

               // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
               string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
               if (paletteDataCount.Length < 1) {
                  newNecklace.paletteNames = PaletteSwapManager.extractPalettes(necklaceData.defaultPalettes);
               }

               newNecklace.durability = item.durability;
               item = newNecklace;
               item.data = rawItemData;

               icon.sprite = ImageManager.getSprite(necklaceData.equipmentIconPath);
               itemSpriteId = necklaceData.necklaceType;
               itemTypeId = item.itemTypeId;
            }
            break;
         case Item.Category.Trinket:
            TrinketStatData trinketData = EquipmentXMLManager.self.getTrinketData(item.itemTypeId);
            if (trinketData == null) {
               D.debug("Failed to process trinket data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.transform.parent.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               if (Global.player != null && !Global.player.isAdmin()) {
                  gameObject.SetActive(false);
               }
               return;
            } else {
               Trinket newTrinket = TrinketStatData.translateDataToTrinket(trinketData);
               newTrinket.id = item.id;
               newTrinket.paletteNames = item.paletteNames;

               // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
               string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
               if (paletteDataCount.Length < 1) {
                  newTrinket.paletteNames = PaletteSwapManager.extractPalettes(trinketData.defaultPalettes);
               }

               newTrinket.durability = item.durability;
               item = newTrinket;
               item.data = rawItemData;

               icon.sprite = ImageManager.getSprite(trinketData.equipmentIconPath);
               itemSpriteId = trinketData.trinketType;
               itemTypeId = item.itemTypeId;
            }
            break;
         case Item.Category.Potion:
            item = item.getCastItem();
            icon.sprite = foodIcon;
            break;
         case Item.Category.Usable:
            item = item.getCastItem();
            icon.sprite = defaultItemIcon;
            break;
         case Item.Category.CraftingIngredients:
            item = item.getCastItem();
            CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
            icon.sprite = ImageManager.getSprite(CraftingIngredients.getBorderlessIconPath(ingredientType));
            // If we don't get a borderless sprite, try to get at least the bordered one
            if (icon.sprite == null || icon.sprite == ImageManager.self.blankSprite) {
               icon.sprite = ImageManager.getSprite(CraftingIngredients.getIconPath(ingredientType));
            }
            break;
         case Item.Category.Blueprint:
            blueprintIcon.SetActive(true);
            if (item.data.Contains(Blueprint.ARMOR_DATA_PREFIX)) {
               ArmorStatData fetchedArmorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
               if (fetchedArmorData == null) {
                  D.debug("Failed to fetch Armor Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedArmorData.equipmentIconPath);

                  // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
                  string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
                  if (paletteDataCount.Length < 1) {
                     item.paletteNames = PaletteSwapManager.extractPalettes(fetchedArmorData.defaultPalettes);
                  }
               }
            } else if (item.data.Contains(Blueprint.WEAPON_DATA_PREFIX)) {
               WeaponStatData fetchedWeaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
               if (fetchedWeaponData == null) {
                  D.debug("Failed to fetch Weapon Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedWeaponData.equipmentIconPath);

                  // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
                  string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
                  if (paletteDataCount.Length < 1) {
                     item.paletteNames = PaletteSwapManager.extractPalettes(fetchedWeaponData.defaultPalettes);
                  }
               }
            } else if (item.data.Contains(Blueprint.HAT_DATA_PREFIX)) {
               HatStatData fetchedHatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
               if (fetchedHatData == null) {
                  D.debug("Failed to fetch Hat Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedHatData.equipmentIconPath);

                  // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
                  string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
                  if (paletteDataCount.Length < 1) {
                     item.paletteNames = PaletteSwapManager.extractPalettes(fetchedHatData.defaultPalettes);
                  }
               }
            } else if (item.data.Contains(Blueprint.RING_DATA_PREFIX)) {
               RingStatData fetchedRingData = EquipmentXMLManager.self.getRingData(item.itemTypeId);
               if (fetchedRingData == null) {
                  D.debug("Failed to fetch Ring Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedRingData.equipmentIconPath);

                  // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
                  string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
                  if (paletteDataCount.Length < 1) {
                     item.paletteNames = PaletteSwapManager.extractPalettes(fetchedRingData.defaultPalettes);
                  }
               }
            } else if (item.data.Contains(Blueprint.NECKLACE_DATA_PREFIX)) {
               NecklaceStatData fetchedNecklaceData = EquipmentXMLManager.self.getNecklaceData(item.itemTypeId);
               if (fetchedNecklaceData == null) {
                  D.debug("Failed to fetch Necklace Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedNecklaceData.equipmentIconPath);

                  // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
                  string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
                  if (paletteDataCount.Length < 1) {
                     item.paletteNames = PaletteSwapManager.extractPalettes(fetchedNecklaceData.defaultPalettes);
                  }
               }
            } else if (item.data.Contains(Blueprint.TRINKET_DATA_PREFIX)) {
               TrinketStatData fetchedTrinketData = EquipmentXMLManager.self.getTrinketData(item.itemTypeId);
               if (fetchedTrinketData == null) {
                  D.debug("Failed to fetch Trinket Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedTrinketData.equipmentIconPath);

                  // If there are no palette assignments during translation, fetch the default palettes from the equipment tool xml
                  string paletteDataCount = item.paletteNames.Replace(" ", "").Replace(",", "");
                  if (paletteDataCount.Length < 1) {
                     item.paletteNames = PaletteSwapManager.extractPalettes(fetchedTrinketData.defaultPalettes);
                  }
               }
            } else if (item.data.Contains(Blueprint.INGREDIENT_DATA_PREFIX)) {
               CraftingIngredients ingredientReference = new CraftingIngredients(item);

               item.itemName = ingredientReference.getName();
               item.itemDescription = ingredientReference.getDescription();
               item.iconPath = ingredientReference.getBorderlessIconPath();
               icon.sprite = ImageManager.getSprite(ingredientReference.getBorderlessIconPath());

            } else {
               D.debug("Unknown blueprint item! " + item.data);
               Destroy(gameObject);
            }

            if (icon.sprite == ImageManager.self.blankSprite) {
               D.debug("Could not retrieve Blueprint: " + item.category + " : " + item.itemTypeId + " : " + item.data);
            }
            break;

         case Item.Category.Quest_Item:
            QuestItem questItem = EquipmentXMLManager.self.getQuestItemById(item.itemTypeId);
            if (questItem == null) {
               D.debug("Failed to fetch Quest Item Data for: " + item.itemTypeId);
               Destroy(gameObject);
               break;
            }

            item.itemName = questItem.itemName;
            item.itemDescription = questItem.itemDescription;
            item.iconPath = questItem.iconPath;
            icon.sprite = ImageManager.getSprite(questItem.iconPath);
            break;

         case Item.Category.Haircut:
            HaircutData haircutData = HaircutXMLManager.self.getHaircutData(item.itemTypeId);

            if (haircutData == null) {
               D.debug("Failed to fetch Haircut Data for: " + item.itemTypeId);
               Destroy(gameObject);
               break;
            }

            icon.sprite = ImageManager.getSprites(item.getIconPath())[0];
            break;

         case Item.Category.Dye:
            PaletteToolData palette = PaletteSwapManager.self.getPalette(item.itemTypeId);

            if (palette == null) {
               D.debug("Failed to fetch Dye Data for: " + item.itemTypeId);
               Destroy(gameObject);
               break;
            }

            PaletteImageType dyeType = (PaletteImageType) palette.paletteType;
            Sprite chosenSprite = ImageManager.self.blankSprite;

            if (dyeType == PaletteImageType.Armor) {
               chosenSprite = ImageManager.getSprites(item.getIconPath())[0];
            } else if (dyeType == PaletteImageType.Weapon) {
               chosenSprite = ImageManager.getSprites(item.getIconPath())[1];
            } else if (dyeType == PaletteImageType.Hair) {
               chosenSprite = ImageManager.getSprites(item.getIconPath())[2];
            } else if (dyeType == PaletteImageType.Hat) {
               chosenSprite = ImageManager.getSprites(item.getIconPath())[3];
            }

            icon.sprite = chosenSprite;
            break;

         case Item.Category.ShipSkin:
            ShipSkinData shipSkinData = ShipSkinXMLManager.self.getShipSkinData(item.itemTypeId);

            if (shipSkinData == null) {
               D.debug("Failed to fetch Ship Skin Data for: " + item.itemTypeId);
               Destroy(gameObject);
               break;
            }

            icon.sprite = ImageManager.getSprites(item.getIconPath())[2];
            break;

         case Item.Category.Consumable:
            ConsumableData consumableData = ConsumableXMLManager.self.getConsumableData(item.itemTypeId);

            if (consumableData == null) {
               D.debug("Failed to fetch Consumable Data for: " + item.itemTypeId);
               Destroy(gameObject);
               break;
            }

            icon.sprite = ImageManager.getSprites(item.getIconPath())[1]; //TODO: Use a more item specific sprite of this item.
            break;
         case Item.Category.Crop:
            if (item.tryGetCastItem(out CropItem cropItem)) {
               item = cropItem;
               icon.sprite = ImageManager.getSprite(cropItem.getIconPath());
            } else {
               D.debug("Failed to cast Crop item for: " + item.itemTypeId);
               Destroy(gameObject);
               break;
            }
            break;
         case Item.Category.Prop:
            propIcon.SetActive(true);
            if (item.tryGetCastItem(out Prop propItem)) {
               item = propItem;
               icon.sprite = ImageManager.getSprite(propItem.getIconPath());
            } else {
               D.debug("Failed to cast Prop item for: " + item.itemTypeId);
               Destroy(gameObject);
               break;
            }
            break;
         default:
            D.editorLog("Failed to process Uncategorized item: " + item.itemTypeId, Color.red);
            icon.sprite = defaultItemIcon;
            break;
      }

      // Set the sprite of the shadow
      iconShadow.sprite = icon.sprite;

      // Recolor
      recoloredSprite.recolor(item.paletteNames);

      // Adds notification if level requirement does not match
      if (item == null || item.category == Item.Category.None || item.itemTypeId == 0) {
         levelRestrictionObj.SetActive(false);
      } else if (item != null && levelRestrictionObj != null && Global.player != null) {
         bool isLevelValid = EquipmentXMLManager.self.isLevelValid(LevelUtil.levelForXp(Global.player.XP), item);
         levelRestrictionObj.SetActive(!isLevelValid);
      }

      // Show the item count when relevant
      if (count > 1 || Item.shouldAlwaysShowCount(item.category)) {
         itemCountText.SetText(count.ToString());
         itemCountText.transform.parent.gameObject.SetActive(true);
      } else {
         itemCountText.transform.parent.gameObject.SetActive(false);
      }

      // Set the tooltip
      tooltip.message = item.category == Item.Category.Blueprint ? EquipmentXMLManager.self.getItemName(item) : item.getTooltip();
      tooltip.message += Item.isUsingEquipmentXML(item.category) ? "\nDurability = " + item.durability : "";

      // Level requirement
      appendLevelRequirementTextToTooltip(item, ref tooltip.message);

      // Soulbinding
      appendBindInfoTextToTooltip(item, ref tooltip.message);

      updateCellColor(item.durability);

      // Saves the item
      _item = item;

      // Store the rarity type
      itemRarityType = item.getRarity();

      // Hides the selection box
      hideSelectedBox();
   }

   public static void appendLevelRequirementTextToTooltip (Item item, ref string tooltipMessage) {
      int xp = 0;

      if (Global.player != null) {
         xp = Global.player.XP;
      }

      int playerLevel = LevelUtil.levelForXp(xp);
      int reqLevel = EquipmentXMLManager.self.equipmentLevelRequirement(item);
      bool meetsRequirement = playerLevel >= reqLevel;

      if (reqLevel > 0) {
         tooltipMessage += string.Format("\n<color=#{0}>Requires Level {1}</color>", ColorUtility.ToHtmlStringRGBA(meetsRequirement ? Color.white : Color.red), reqLevel);
      }
   }

   public static void appendBindInfoTextToTooltip (Item item, ref string tooltipMessage) {
      Item.SoulBindingType sbtype = SoulBindingManager.getItemSoulbindingTypeClient(item);

      if (sbtype == Item.SoulBindingType.Equip) {
         tooltipMessage += string.Format("\n<color=#{0}>Bind On Equip</color>", ColorUtility.ToHtmlStringRGBA(new Color32(105, 255, 110, 255)));
      } else if (sbtype == Item.SoulBindingType.Acquisition) {
         tooltipMessage += string.Format("\n<color=#{0}>Bind On Acquire</color>", ColorUtility.ToHtmlStringRGBA(new Color32(105, 255, 210, 255)));
      }
   }

   public void updateCellColor (int durability) {
      // Prototype UI feeback if an item durability is low or high
      if (itemCellImage) {
         if (durability < 25) {
            lowDurabilityColor.a = DURABILITY_NOTIF_ALPHA;
            itemCellImage.color = lowDurabilityColor;
         } else if (durability <= 50) {
            lowDurabilityColor.a = DURABILITY_NOTIF_ALPHA;
            itemCellImage.color = midDurabilityColor;
         } else {
            lowDurabilityColor.a = 0;
            itemCellImage.color = highDurabilityColor;
         }
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      // Invoke the click events
      if (_interactable) {
         if (eventData.button == PointerEventData.InputButton.Left) {
            // Determine if two clicks where close enough in time to be considered a double click
            if (Time.time - _lastClickTime < DOUBLE_CLICK_DELAY) {
               doubleClickEvent.Invoke();
            } else if (InputManager.self.inputMaster.Land.Sprint.IsPressed() || InputManager.self.inputMaster.Sea.Dash.IsPressed()) {
               shiftClickEvent.Invoke();
            } else {
               leftClickEvent.Invoke();
            }
            _lastClickTime = Time.time;
         } else if (eventData.button == PointerEventData.InputButton.Right) {
            rightClickEvent.Invoke();
         }
      }
   }

   public void disablePointerEvents () {
      _interactable = false;
   }

   public void enablePointerEvents () {
      _interactable = true;
   }

   public void showSelectedBox () {
      if (selectedBox != null) {
         selectedBox.enabled = true;
      }
   }

   public void hideSelectedBox () {
      if (selectedBox != null) {
         selectedBox.enabled = false;
      }
   }

   public Item getItem () {
      return _item;
   }

   public Sprite getItemSprite () {
      return icon.sprite;
   }

   public void hideBackground () {
      backgroundImage.enabled = false;
   }

   public void hideItemCount () {
      itemCountText.transform.parent.gameObject.SetActive(false);
   }

   public void LeftClick () {
      leftClickEvent.Invoke();
   }

   #region Private Variables

   // A reference to the item being displayed
   protected Item _item;

   // Gets set to true when the text must react to pointer events
   protected bool _interactable = true;

   // The time since the last left click on this cell
   private float _lastClickTime = float.MinValue;

   // The delay between two clicks to be considered a double click
   private float DOUBLE_CLICK_DELAY = 0.5f;

   #endregion

}
