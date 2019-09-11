using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System.IO;

// Christopher Palacios

namespace ItemEditor
{
   public class ItemBuilderWindow : EditorWindow
   {
      #region Public Variables

      // Holder for the stances that can be used for the ability that is being created in the AbilityBuilder
      public Battler.Stance[] abilityAllowedStances = { Battler.Stance.Attack, Battler.Stance.Balanced, Battler.Stance.Defense };

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

         if(_battleItemType == BattleItemType.UNDEFINED) {
            ItemEditorLayout.header("ITEM BUILDER - " + "Select an item type");

         } else {
            ItemEditorLayout.header("ITEM BUILDER - " + "Building " + _builderTypeText + ":");
         }

         string editMsg = isItemInSlot ? "Editing item..." : "Drag or select an item to edit";
         EditorGUILayout.LabelField(editMsg, GUILayout.MinWidth(100));

         _itemToEdit = (BattleItemData) EditorGUILayout.ObjectField(_itemToEdit, typeof(BattleItemData), true);

         if (isItemInSlot) {
            EditorGUILayout.HelpBox("Item in slot, now editting " + _itemToEdit.getName() + " properties...", MessageType.Info);
         }

         EditorGUILayout.BeginHorizontal("box");
         EditorGUILayout.BeginVertical();

         drawDefault();

         // Item in slot to edit, we fill the data to the item we placed in the slot.
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
                        setAbilityItemValues((AbilityData) _itemToEdit);
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
               drawAllAbilityBlocks();
               break;
            case BattleItemType.Weapon:
               drawWeaponBlock();
               break;
         }

         if (GUILayout.Button("Build " + _builderTypeText)) {
            // Basic data set
            BattleItemData basicData = BattleItemData.CreateInstance(_itemID, _itemName, _itemDesc, _itemDamage, _itemElementType, _hitAudioClip,
                _hitParticle, _battleItemType, _itemIcon, _levelRequirement);

            switch (_battleItemType) {
               case BattleItemType.Ability:

                  AbilityData newAbility = AbilityData.CreateInstance(basicData, _abilityCost, _abilityCanBeBlocked,
                      _abilityCastParticle, _abilityCastAudioclip, abilityAllowedStances, _classRequirement, _abilityType, 
                      _cooldown, _hasKnockup, _hasShake, _apChange);

                  createAbilityAsset(_builderFolder, newAbility);

                  break;
               case BattleItemType.Weapon:

                  WeaponData newWeapon = WeaponData.CreateInstance(basicData, _classRequirement, _primaryColor, _secondaryColor);
                  createWeaponAsset(_builderFolder, newWeapon);

                  break;
            }
         }

         EditorGUILayout.EndHorizontal();
         EditorGUILayout.EndVertical();
      }

      private void drawDefault () {
         EditorGUILayout.BeginHorizontal("box");

         ItemEditorLayout.centeredLabel("Item ID");
         _itemID = EditorGUILayout.IntField(_itemID, GUILayout.MinWidth(20), GUILayout.MaxWidth(132));

         ItemEditorLayout.centeredLabel("Item Name");
         _itemName = EditorGUILayout.TextArea(_itemName, GUILayout.MinWidth(20));

         EditorGUILayout.EndHorizontal();

         EditorGUILayout.BeginHorizontal("box");

         ItemEditorLayout.centeredLabel("Item Icon");
         _itemIcon = (Sprite) EditorGUILayout.ObjectField(_itemIcon, typeof(Sprite), true, GUILayout.MaxWidth(120));

         ItemEditorLayout.centeredLabel("Item Damage");
         _itemDamage = EditorGUILayout.IntField(_itemDamage, GUILayout.MinWidth(20));

         EditorGUILayout.EndHorizontal();
         EditorGUILayout.Space();

         EditorGUILayout.PrefixLabel("Level requirement");
         _levelRequirement = EditorGUILayout.IntField(_levelRequirement, GUILayout.MinWidth(20));
         EditorGUILayout.Space();

         EditorGUILayout.PrefixLabel("Item Description");
         _itemDesc = EditorGUILayout.TextArea(_itemDesc, GUILayout.MinWidth(20), GUILayout.MinHeight(40));
         EditorGUILayout.Space();

         EditorGUILayout.PrefixLabel("Item Element");
         _itemElementType = (Element) EditorGUILayout.EnumPopup(_itemElementType);
         EditorGUILayout.Space();

         EditorGUILayout.PrefixLabel("Hit Parameters");
         _hitAudioClip = (AudioClip) EditorGUILayout.ObjectField(_hitAudioClip, typeof(AudioClip), true);
         _hitParticle = (ParticleSystem) EditorGUILayout.ObjectField(_hitParticle, typeof(ParticleSystem), true);
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

         if (_battleItemType == BattleItemType.Ability) {
            // Prepare item array.
            ItemBuilderWindow target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty stancesProperties = so.FindProperty("abilityAllowedStances");

            // Stances
            ItemEditorLayout.centeredLabel("Allowed stances");
            EditorGUILayout.PropertyField(stancesProperties, true);
            so.ApplyModifiedProperties();
         }

         EditorGUILayout.Space();
      }

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

      private void drawAbilitySecondBlock() {
         ItemEditorLayout.centeredLabel("Can be blocked");
         _abilityCanBeBlocked = EditorGUILayout.Toggle(_abilityCanBeBlocked, GUILayout.MaxWidth(40));

         ItemEditorLayout.centeredLabel("Has Knockup");
         _hasKnockup = EditorGUILayout.Toggle(_hasKnockup, GUILayout.MaxWidth(40));

         ItemEditorLayout.centeredLabel("Has Shake");
         _hasKnockup = EditorGUILayout.Toggle(_hasKnockup, GUILayout.MaxWidth(40));
      }

      private void drawAbilityThirdBlock () {
         ItemEditorLayout.centeredLabel("Cast audioclip");
         _abilityCastAudioclip = (AudioClip) EditorGUILayout.ObjectField(_abilityCastAudioclip, typeof(AudioClip), true);

         ItemEditorLayout.centeredLabel("Cast Particle");
         _abilityCastParticle = (ParticleSystem) EditorGUILayout.ObjectField(_abilityCastParticle, typeof(ParticleSystem), true);

         ItemEditorLayout.centeredLabel("Ability Type");
         _abilityType = (AbilityType) EditorGUILayout.EnumPopup(_abilityType, GUILayout.MaxWidth(120));
      }

      private void drawAllAbilityBlocks () {
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

      private void setMainItemValues (BattleItemData item) {
         BattleItemType itemType = item.getBattleItemType();

         _itemName = item.getName();
         _itemDesc = item.getDescription();
         _itemID = item.getItemID();
         _itemIcon = item.getItemIcon();

         _itemDamage = item.getBaseDamage();
         _itemElementType = item.getElementType();

         _hitAudioClip = item.getHitAudioClip();
         _hitParticle = item.getHitParticle();
         _classRequirement = item.getClassRequirement();
      }

      private void setAbilityItemValues (AbilityData item) {
         _abilityCost = item.getAbilityCost();
         _abilityCanBeBlocked = item.getBlockStatus();

         _abilityCastParticle = item.getCastParticle();
         _abilityCastAudioclip = item.getCastAudioClip();

         abilityAllowedStances = item.getAllowedStances();
      }

      private void cleanInputs () {
         _itemName = string.Empty;
         _itemDesc = string.Empty;

         // Basic Item Values, all weapons and abilities have a name and description.
         _itemName = "NewItem";
         _itemDesc = "Item new Description";
         _itemID = -1;
         _itemIcon = null;

         _itemDamage = 10;
         _itemElementType = Element.Physical;
         _hitAudioClip = null;
         _hitParticle = null;
         _battleItemType = BattleItemType.UNDEFINED;

         _classRequirement = Weapon.Class.Any;

         // Ability parameters
         _abilityCost = 10;
         _abilityCanBeBlocked = true;
         _abilityCastParticle = null;
         _abilityCastAudioclip = null;

         _cooldown = 4;
         _hasShake = false;
         _hasKnockup = false;
         _apChange = 3;

         // Weapon parameters
         _primaryColor = ColorType.Black;
         _secondaryColor = ColorType.Black;
      }

      public static void createAbilityAsset (string folder, AbilityData itemToBuild) {
         AbilityData asset = AbilityData.CreateInstance(itemToBuild);

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

      private bool isItemInSlot{
         get { return _itemToEdit != null; }
      }

      private BattleItemType typeInSlot{
         get { return _itemToEdit.getBattleItemType(); }
      }

      private void OnDestroy () {
         _onRemovedItem.RemoveAllListeners();
         Debug.Log("removed events");
      }

      #region Private Variables

      // Callback called whenever we have removed an item from the editor window.
      UnityEvent _onRemovedItem = new UnityEvent();

      // All variables below are just store temporarily the information that we are filling inside the editor window

      // Very basic BattleItemData that all items have.
      private string _itemName = "ItemName";
      private string _itemDesc = "ItemDescription";
      private int _itemID = -1;
      private int _levelRequirement = -1;
      private Sprite _itemIcon;

      // More battle related data
      private int _itemDamage = 10;
      private Element _itemElementType = Element.Physical;
      private AudioClip _hitAudioClip;
      private ParticleSystem _hitParticle;
      private BattleItemType _battleItemType = BattleItemType.UNDEFINED;

      // The class that the player needs to be to be able to use this item.
      private Weapon.Class _classRequirement = Weapon.Class.Any;

      // Basic AbilityData combat values
      private int _abilityCost = 10;
      private bool _abilityCanBeBlocked = true;
      private ParticleSystem _abilityCastParticle;
      private AudioClip _abilityCastAudioclip;
      private AbilityType _abilityType = AbilityType.UNDEFINED;
      private float _cooldown = 4;
      private bool _hasShake = true;
      private bool _hasKnockup = false;
      private int _apChange = 3;

      // Basic Weapon parameters
      private ColorType _primaryColor = ColorType.Black;
      private ColorType _secondaryColor = ColorType.Black;

      // Used for setting the message of what we are building in the editor window.
      private string _builderTypeText;

      // Folder that we will place the created asset.
      private string _builderFolder;

      // Flags used only for telling whenever we have switched an item directly in the item editor window. 
      // And adjust the values again for the new item.
      private bool _hasPlacedItem = false;
      private int _lastPlacedItemID = -99;

      // Item that it is inside the "Edit item" object field.
      private BattleItemData _itemToEdit = null;

      // Reference to the ItemBuilder window.
      private static ItemBuilderWindow _window;

      #endregion
   }
}