using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

// Christopher Palacios

namespace ItemEditor
{
   public class ItemBuilderWindow : EditorWindow
   {
      #region Public Variables

      // Holder for the stances that can be used for the ability that is being created in the AbilityBuilder
      public BattlerBehaviour.Stance[] abilityAllowedStances = { BattlerBehaviour.Stance.Attack, BattlerBehaviour.Stance.Balanced, BattlerBehaviour.Stance.Defense };

      // Temporary holder that will store the hit sprites for the ability that is being built
      public Sprite[] hitSpritesEffect;

      // Temporary holder that will store the cast sprites for the ability that is being built
      public Sprite[] castSpritesEffect;

      #endregion

      private void Awake () {
         _onRemovedItem.AddListener(cleanInputs);
      }

      [MenuItem("Window/Aster/Item Builder")]
      static void Init () {
         // Get existing open window or if none, make a new one:
         _window = GetWindow<ItemBuilderWindow>();

         _window.minSize = new Vector2(500, 200);
         _window.Show();
      }

      private void OnGUI () {
         _builderTypeText = (_battleItemType == BattleItemType.Ability) ? "Ability" : "Weapon";
         _builderFolder = (_battleItemType == BattleItemType.Ability) ? "Abilities" : "Weapons";

         if (_battleItemType == BattleItemType.UNDEFINED) {
            ItemEditorLayout.header("ITEM BUILDER - " + "Select an item type");

         } else {
            ItemEditorLayout.header("ITEM BUILDER - " + "Building " + _builderTypeText + ":");
         }

         string editMsg = isItemInSlot ? "Editing item..." : "Drag or select an item to edit";
         EditorGUILayout.LabelField(editMsg, GUILayout.MinWidth(100));

         if(GUILayout.Button("Check all item IDs")){
            string[] guids = AssetDatabase.FindAssets(" t:BattleItemData", new[] { "Assets/CreatedItems/Abilities" });

            checkForDuplicatedIDs(guids);
         }

         _itemToEdit = (BattleItemData) EditorGUILayout.ObjectField(_itemToEdit, typeof(BattleItemData), true);

         if (isItemInSlot) {
            EditorGUILayout.HelpBox("Item in slot, now editting " + _itemToEdit.getName() + " properties...", MessageType.Info);
         }

         EditorGUILayout.BeginHorizontal("box");
         EditorGUILayout.BeginVertical();

         _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false);
         drawDefault();

         // Item in slot to edit, we fill the data to the item we placed in the slot
         if (_itemToEdit != null) {
            if (isItemInSlot) {
               if (_lastPlacedItemID != _itemToEdit.getItemID()) {
                  _lastPlacedItemID = _itemToEdit.getItemID();

                  _hasPlacedItem = false;
               }

               if (!_hasPlacedItem) {
                  setMainItemValues(_itemToEdit);

                  // Check the item type.
                  switch (_itemToEdit.getBattleItemType()) {
                     case BattleItemType.Ability:
                        _battleItemType = BattleItemType.Ability;
                        setBasicAbilityItemValues((BasicAbilityData) _itemToEdit);

                        switch (_abilityType) {
                           case AbilityType.Standard:
                              setAttackAbilityItemValues((AttackAbilityData) _itemToEdit);
                              break;
                           case AbilityType.BuffDebuff:
                              setBuffAbilityItemValues((BuffAbilityData) _itemToEdit);
                              break;
                        }

                        break;
                     case BattleItemType.Weapon:
                        _battleItemType = BattleItemType.Weapon;
                        break;
                  }

                  _hasPlacedItem = true;
               }
            } else {
               // Check if it had an item before
               if (_hasPlacedItem) {
                  _onRemovedItem.Invoke();
                  _hasPlacedItem = false;
               }
            }
         }

         switch (_battleItemType) {
            case BattleItemType.Ability:
               drawAllBasicAbilityBlocks();
               break;
            case BattleItemType.Weapon:
               drawWeaponBlock();
               break;
         }

         if (_battleItemType != BattleItemType.UNDEFINED) {
            if (GUILayout.Button("Build " + _builderTypeText)) {

               // Item ID validation.
               if (_itemID.Equals(-1)) {
                  if (EditorUtility.DisplayDialog("Invalid item ID", "Please select a positive item ID", "Ok", "Ignore")) {
                     return;
                  }
               }

               // Basic data set
               BattleItemData basicData = BattleItemData.CreateInstance(_itemID, _itemName, _itemDesc, _itemElementType, _hitAudioClip,
                  hitSpritesEffect, _battleItemType, _itemIcon, _levelRequirement);

               switch (_battleItemType) {
                  case BattleItemType.Ability:
                     BasicAbilityData basicAbilityData = BasicAbilityData.CreateInstance(basicData, _abilityCost, castSpritesEffect,
                        _abilityCastAudioclip, abilityAllowedStances, _abilityType, _cooldown, _apChange, _fxTimePerFrame);

                     switch (_abilityType) {
                        case AbilityType.Standard:
                           AttackAbilityData newAttackAbility = AttackAbilityData.CreateInstance(basicAbilityData, _hasKnockup, _itemDamage, _hasShake,
                              _abilityActionType, _abilityCanBeBlocked);

                           createAttackAbilityAsset(_builderFolder, newAttackAbility);
                           break;

                        case AbilityType.BuffDebuff:
                           BuffAbilityData newBuffAbility = BuffAbilityData.CreateInstance(basicAbilityData, _buffDuration, _buffType,
                              _buffActionType, _buffIcon, _buffValue);

                           createBuffAbilityAsset(_builderFolder, newBuffAbility);
                           break;
                     }

                     break;
                  case BattleItemType.Weapon:
                     WeaponData newWeapon = WeaponData.CreateInstance(basicData, _classRequirement, _primaryColor, _secondaryColor, _itemDamage);
                     createWeaponAsset(_builderFolder, newWeapon);
                     break;
               }
            }
         }

         EditorGUILayout.EndScrollView();
         EditorGUILayout.EndHorizontal();
         EditorGUILayout.EndVertical();
      }

      private void drawDefault () {
         EditorGUILayout.BeginHorizontal("box");

         ItemEditorLayout.centeredLabel("Item ID");
         _itemID = EditorGUILayout.IntField(_itemID, GUILayout.MinWidth(20), GUILayout.MaxWidth(60));

         ItemEditorLayout.centeredLabel("Item Name");
         _itemName = EditorGUILayout.TextArea(_itemName, GUILayout.MinWidth(20));

         EditorGUILayout.EndHorizontal();

         EditorGUILayout.Space();

         ItemEditorLayout.centeredLabel("Item Description");
         _itemDesc = EditorGUILayout.TextArea(_itemDesc, GUILayout.MinWidth(20), GUILayout.MinHeight(60));

         EditorGUILayout.Space();

         EditorGUILayout.BeginHorizontal("box");

         ItemEditorLayout.centeredLabel("Level requirement");
         _levelRequirement = EditorGUILayout.IntField(_levelRequirement, GUILayout.MinWidth(20), GUILayout.MaxWidth(60));

         ItemEditorLayout.centeredLabel("Item Icon");
         _itemIcon = (Sprite) EditorGUILayout.ObjectField(_itemIcon, typeof(Sprite), true, GUILayout.MaxWidth(120));

         ItemEditorLayout.centeredLabel("Item Element");
         _itemElementType = (Element) EditorGUILayout.EnumPopup(_itemElementType);

         EditorGUILayout.EndHorizontal();
         EditorGUILayout.Space();

         EditorGUILayout.PrefixLabel("Hit SFX");
         _hitAudioClip = (AudioClip) EditorGUILayout.ObjectField(_hitAudioClip, typeof(AudioClip), true);
         drawHitVFXBlock();
         EditorGUILayout.Space();

         EditorGUILayout.PrefixLabel("Class requirement");

         // Class requirement
         _classRequirement = (Weapon.Class) EditorGUILayout.EnumPopup(_classRequirement);

         EditorGUILayout.Space();

         if (!isItemInSlot) {
            EditorGUILayout.PrefixLabel("Item type to build");
            _battleItemType = (BattleItemType) EditorGUILayout.EnumPopup(_battleItemType);
         }

         EditorGUILayout.Space();

         if (_battleItemType == BattleItemType.UNDEFINED) {
            ItemEditorLayout.horizontalHelpbox(() => {
               EditorGUILayout.HelpBox("Please select an item type to build", MessageType.Warning);
            });
         }

         if (_battleItemType == BattleItemType.Ability) {
            // Show the allowed stances in the item creation window
            drawAllowedStancesBlock();
         }

         EditorGUILayout.Space();
      }

      #region Custom window arrays

      private void drawAllowedStancesBlock () {
         // Prepare item array
         ItemBuilderWindow target = this;
         SerializedObject so = new SerializedObject(target);
         SerializedProperty stancesProperties = so.FindProperty("abilityAllowedStances");

         // Stances
         ItemEditorLayout.centeredLabel("Allowed stances");
         EditorGUILayout.PropertyField(stancesProperties, true);
         so.ApplyModifiedProperties();
      }
      
      private void drawHitVFXBlock () {
         // Prepare item array
         ItemBuilderWindow target = this;
         SerializedObject so = new SerializedObject(target);
         SerializedProperty hitProperties = so.FindProperty("hitSpritesEffect");

         // Stances
         EditorGUILayout.PrefixLabel("Hit VFX");
         EditorGUILayout.PropertyField(hitProperties, true);
         so.ApplyModifiedProperties();
      }
      
      private void drawCastVFXBlock () {
         // Prepare item array
         ItemBuilderWindow target = this;
         SerializedObject so = new SerializedObject(target);
         SerializedProperty castProperties = so.FindProperty("castSpritesEffect");

         // Stances
         ItemEditorLayout.centeredLabel("Cast VFX");
         EditorGUILayout.PropertyField(castProperties, true);
         so.ApplyModifiedProperties();
      }

      #endregion

      private void drawWeaponBlock () {
         EditorGUILayout.BeginVertical("box");
         EditorGUILayout.Space();

         ItemEditorLayout.horizontallyCentered(drawFirstWeaponBlock);

         EditorGUILayout.Space();

         EditorGUILayout.EndVertical();
      }

      private void drawFirstWeaponBlock () {
         ItemEditorLayout.centeredLabel("Primary Color");
         _primaryColor = (ColorType) EditorGUILayout.EnumPopup(_primaryColor, GUILayout.MaxWidth(120));

         ItemEditorLayout.centeredLabel("Secondary Color");
         _secondaryColor = (ColorType) EditorGUILayout.EnumPopup(_secondaryColor, GUILayout.MaxWidth(120));
      }

      private void drawAbilityFirstBlock () {
         ItemEditorLayout.centeredLabel("AP Cost");
         _abilityCost = EditorGUILayout.IntField(_abilityCost, GUILayout.MaxWidth(40));

         ItemEditorLayout.centeredLabel("AP Change");
         _apChange = EditorGUILayout.IntField(_apChange, GUILayout.MaxWidth(40));

         ItemEditorLayout.centeredLabel("Base cooldown");
         _cooldown = EditorGUILayout.FloatField(_cooldown, GUILayout.MaxWidth(40));
      }

      private void drawAbilitySecondBlock () {
         ItemEditorLayout.centeredLabel("Cast audioclip");
         _abilityCastAudioclip = (AudioClip) EditorGUILayout.ObjectField(_abilityCastAudioclip, typeof(AudioClip), true);
         
         drawCastVFXBlock();
      }

      private void drawAbilityThirdBlock () {
         EditorGUILayout.BeginVertical("box");

         ItemEditorLayout.centeredLabel("Ability Type");
         _abilityType = (AbilityType) EditorGUILayout.EnumPopup(_abilityType);

         switch (_abilityType) {
            // Fill the remaining blocks with the standard ability (attack)
            case AbilityType.Standard:
               drawAllAttackAbilityBlocks();
               break;
            // Fill the remaining blocks with a buff/debuff ability
            case AbilityType.BuffDebuff:
               drawAllBuffDebuffBlocks();
               break;
         }

         EditorGUILayout.EndVertical();
      }

      private void drawAllBasicAbilityBlocks () {
         EditorGUILayout.BeginVertical("box");

         EditorGUILayout.Space();
         ItemEditorLayout.horizontallyCentered(drawAbilityFirstBlock);
         EditorGUILayout.Space();
         ItemEditorLayout.horizontallyCentered(drawAbilitySecondBlock);
         EditorGUILayout.Space();
         ItemEditorLayout.horizontallyCentered(drawAbilityThirdBlock);

         EditorGUILayout.Space();

         EditorGUILayout.EndVertical();
      }

      private void drawAllAttackAbilityBlocks () {
         EditorGUILayout.BeginVertical("box");

         EditorGUILayout.PrefixLabel("Standard Ability");

         EditorGUILayout.Space();

         EditorGUILayout.BeginHorizontal();

         ItemEditorLayout.centeredLabel("Action Type");
         _abilityActionType = (AbilityActionType) EditorGUILayout.EnumPopup(_abilityActionType);

         ItemEditorLayout.centeredLabel("Item Damage");
         _itemDamage = EditorGUILayout.IntField(_itemDamage, GUILayout.MinWidth(20));

         EditorGUILayout.EndHorizontal();

         EditorGUILayout.Space();

         EditorGUILayout.BeginHorizontal();

         ItemEditorLayout.centeredLabel("Can be blocked");
         _abilityCanBeBlocked = EditorGUILayout.Toggle(_abilityCanBeBlocked);

         ItemEditorLayout.centeredLabel("Has Knockup");
         _hasKnockup = EditorGUILayout.Toggle(_hasKnockup);

         ItemEditorLayout.centeredLabel("Has Shake");
         _hasShake = EditorGUILayout.Toggle(_hasShake);

         EditorGUILayout.EndHorizontal();

         EditorGUILayout.Space();

         EditorGUILayout.EndVertical();
      }

      private void drawAllBuffDebuffBlocks () {
         EditorGUILayout.BeginVertical("box");

         EditorGUILayout.PrefixLabel("Buffs & Debuffs");

         EditorGUILayout.Space();

         EditorGUILayout.BeginHorizontal();

         ItemEditorLayout.centeredLabel("Action Type");
         _buffActionType = (BuffActionType) EditorGUILayout.EnumPopup(_buffActionType, GUILayout.MinWidth(20));

         ItemEditorLayout.centeredLabel("Type");
         _buffType = (BuffType) EditorGUILayout.EnumPopup(_buffType, GUILayout.MinWidth(20));

         EditorGUILayout.EndHorizontal();

         EditorGUILayout.Space();

         EditorGUILayout.BeginHorizontal();

         ItemEditorLayout.centeredLabel("Icon");
         _buffIcon = (Sprite) EditorGUILayout.ObjectField(_buffIcon, typeof(Sprite), true);

         ItemEditorLayout.centeredLabel("Duration");
         _buffDuration = EditorGUILayout.FloatField(_buffDuration, GUILayout.MinWidth(20));

         ItemEditorLayout.centeredLabel("Value");
         _buffValue = EditorGUILayout.IntField(_buffValue, GUILayout.MinWidth(20));

         EditorGUILayout.EndHorizontal();

         EditorGUILayout.Space();

         EditorGUILayout.EndVertical();
      }
      
      private void setMainItemValues (BattleItemData item) {
         BattleItemType itemType = item.getBattleItemType();

         _itemName = item.getName();
         _itemDesc = item.getDescription();
         _itemID = item.getItemID();
         _itemIcon = item.getItemIcon();

         _itemElementType = item.getElementType();

         _hitAudioClip = item.getHitAudioClip();
         _hitVFXSprites = item.getHitEffect();
         _classRequirement = item.getClassRequirement();
      }
      
      private void setBasicAbilityItemValues (BasicAbilityData item) {
         _abilityCost = item.getAbilityCost();
         
         _castVFXSprites = item.getCastEffect();
         _abilityCastAudioclip = item.getCastAudioClip();

         abilityAllowedStances = item.getAllowedStances();
         _apChange = item.getApChange();

         _abilityType = item.getAbilityType();
      }

      private void setAttackAbilityItemValues (AttackAbilityData item) {
         _abilityCanBeBlocked = item.getBlockStatus();
         _itemDamage = item.getBaseDamage();
         _hasShake = item.hasShake();
         _hasKnockup = item.hasKnockup();
         _abilityActionType = item.getAbilityActionType();
      }

      private void setBuffAbilityItemValues (BuffAbilityData item) {
         _buffDuration = item.getBuffDuration();
         _buffIcon = item.getBuffIcon();
         _buffType = item.getBuffType();
         _buffValue = item.getBuffValue();
      }

      private void cleanInputs () {
         _itemName = string.Empty;
         _itemDesc = string.Empty;

         // Basic Item Values, all weapons and abilities have a name and description
         _itemName = "NewItem";
         _itemDesc = "new item description";
         _itemID = -1;
         _itemIcon = null;

         _itemDamage = 10;
         _itemElementType = Element.Physical;
         _hitAudioClip = null;
         _hitVFXSprites = null;
         _battleItemType = BattleItemType.UNDEFINED;

         _classRequirement = Weapon.Class.Any;

         // Ability parameters
         _abilityCost = 10;
         _abilityCanBeBlocked = true;
         _castVFXSprites = null;
         _abilityCastAudioclip = null;
         _cooldown = 4;
         _hasShake = false;
         _hasKnockup = false;
         _apChange = 3;
         _abilityActionType = AbilityActionType.UNDEFINED;
         _abilityType = AbilityType.Standard;
         _fxTimePerFrame = 0.10f;

         // Buff/Debuff parameters
         _buffActionType = BuffActionType.UNDEFINED;
         _buffType = BuffType.UNDEFINED;
         _buffIcon = null;
         _buffDuration = 12;
         _buffValue = 10;

         // Weapon parameters
         _primaryColor = ColorType.Black;
         _secondaryColor = ColorType.Black;
      }

      // Just a very basic verification, for checking if we have abilities that have the same ID
      private static void checkForDuplicatedIDs (string[] guids) {
         List<int> occupiedIDs = new List<int>();

         foreach (string guid in guids) {
            BattleItemData dataAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(BattleItemData)) as BattleItemData;

            if (occupiedIDs.Contains(dataAsset.getItemID())) {
               string msg = "Duplicated item ID found, same item ID assets will and can cause problems in the game," +
                  " please select duplicated item and change its ID to a different one.";

               if (EditorUtility.DisplayDialog("Duplicated Item ID Found", msg, "Yes, select duplicated ID asset", "Ignore")) {
                  EditorUtility.FocusProjectWindow();
                  Selection.activeObject = dataAsset;
               }
            } else {
               occupiedIDs.Add(dataAsset.getItemID());
            }
         }
      }

      // Full check for duplicated IDs if no asset paths are given
      private static void checkForDuplicatedIDs () {
         string[] guids = AssetDatabase.FindAssets(" t:BattleItemData", new[] { "Assets/CreatedItems/Abilities" });

         checkForDuplicatedIDs(guids);
      }

      public static void createAttackAbilityAsset (string folder, AttackAbilityData itemToBuild) {
         AttackAbilityData asset = AttackAbilityData.CreateInstance(itemToBuild);

         string path = "Assets/CreatedItems/" + folder;
         string assetPathAndName = path + "/" + asset.getName() + ".asset";

         EditorUtility.SetDirty(asset);

         createFinalAsset(asset, assetPathAndName, asset.getName());
      }

      public static void createBuffAbilityAsset (string folder, BuffAbilityData itemToBuild) {
         BuffAbilityData asset = BuffAbilityData.CreateInstance(itemToBuild);

         string path = "Assets/CreatedItems/" + folder;
         string assetPathAndName = path + "/" + asset.getName() + ".asset";

         EditorUtility.SetDirty(asset);

         createFinalAsset(asset, assetPathAndName, asset.getName());
      }

      public static void createWeaponAsset (string folder, WeaponData itemToBuild) {
         WeaponData asset = WeaponData.CreateInstance(itemToBuild);

         string path = "Assets/CreatedItems/" + folder;
         string assetPathAndName = path + "/" + asset.getName() + ".asset";

         EditorUtility.SetDirty(asset);

         createFinalAsset(asset, assetPathAndName, asset.getName());
      }

      private static void createFinalAsset (Object finalAsset, string assetFinalPath, string assetName) {
         if (File.Exists(assetFinalPath)) {
            if (EditorUtility.DisplayDialog("Item already exists", "Overwrite " + assetName + " ?", "Yes, replace", "Cancel")) {
               AssetDatabase.CreateAsset(finalAsset, assetFinalPath);

               AssetDatabase.SaveAssets();
               AssetDatabase.Refresh();
               EditorUtility.FocusProjectWindow();
               Selection.activeObject = finalAsset;
            }
         } else {
            AssetDatabase.CreateAsset(finalAsset, assetFinalPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = finalAsset;
         }
      }

      private bool isItemInSlot
      {
         get { return _itemToEdit != null; }
      }

      private BattleItemType typeInSlot
      {
         get { return _itemToEdit.getBattleItemType(); }
      }

      private void OnDestroy () {
         _onRemovedItem.RemoveAllListeners();
      }

      #region Private Variables

      // Callback called whenever we have removed an item from the editor window
      UnityEvent _onRemovedItem = new UnityEvent();

      // All variables below are just store temporarily the information that we are filling inside the editor window

      // Very basic BattleItemData that all items have
      private string _itemName = "ItemName";
      private string _itemDesc = "New item description - (This is a great item)";
      private int _itemID = -1;
      private int _levelRequirement = -1;
      private Sprite _itemIcon;

      // More battle related data
      private int _itemDamage = 10;
      private Element _itemElementType = Element.Physical;
      private AudioClip _hitAudioClip;
      private Sprite[] _hitVFXSprites;
      private Sprite[] _castVFXSprites;
      private BattleItemType _battleItemType = BattleItemType.UNDEFINED;

      // The class that the player needs to be to be able to use this item
      private Weapon.Class _classRequirement = Weapon.Class.Any;

      // Basic AbilityData combat values
      private int _abilityCost = 10;
      private bool _abilityCanBeBlocked = true;
      private AudioClip _abilityCastAudioclip;
      private AbilityActionType _abilityActionType = AbilityActionType.UNDEFINED;
      private AbilityType _abilityType = AbilityType.Standard;
      private float _cooldown = 4;
      private bool _hasShake = true;
      private bool _hasKnockup = false;
      private int _apChange = 3;
      private float _fxTimePerFrame;

      // Buff ability parameters
      private BuffActionType _buffActionType;
      private BuffType _buffType;
      private Sprite _buffIcon;
      private float _buffDuration;
      private int _buffValue;

      // Basic Weapon parameters
      private ColorType _primaryColor = ColorType.Black;
      private ColorType _secondaryColor = ColorType.Black;

      // Used for setting the message of what we are building in the editor window
      private string _builderTypeText;

      // Folder that we will place the created asset
      private string _builderFolder;

      // Flags used only for telling whenever we have switched an item directly in the item editor window
      // And adjust the values again for the new item
      private bool _hasPlacedItem = false;
      private int _lastPlacedItemID = -99;

      // Item that it is inside the "Edit item" object field
      private BattleItemData _itemToEdit = null;

      // Reference to the ItemBuilder window
      private static ItemBuilderWindow _window;

      // Used for handling the scrollbar in the window
      private Vector2 _scrollPosition = Vector2.zero;

      #endregion
   }
}