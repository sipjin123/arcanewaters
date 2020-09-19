using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using DG.Tweening;
using TMPro;

public class CharacterCreationPanel : ClientMonoBehaviour
{
   #region Public Variables

   [Header("Settings")]
   // The color for the background of the spot
   public Color circleFaderBackgroundColor = new Color(0, 0, 0, .75f);
      
   [Header("References")]
   // The button to go to the next screen
   public Button nextButton;

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // Our toggle groups
   public ToggleGroup hairGroup1;
   public ToggleGroup hairGroup2;
   public ToggleGroup armorGroup1;
   public ToggleGroup armorGroup2;
   public ToggleGroup eyeGroup1;

   // The male gender toggle
   public Toggle maleToggle;

   // The female gender toggle
   public Toggle femaleToggle;

   // The Text that contains our name
   public TMP_InputField nameText;

   // The tabbed panel controller
   public TabbedPanelController tabbedPanel;

   // The perk questions grid
   public CreationPerksGrid perksGrid;

   // The hair styles that make use of two color palettes
   public List<HairLayer.Type> _multiplePaletteHairStyles;

   [Header("Random Initial Styles")]
   [Header("Female")]
   // The eye types to choose randomly from when the character starts being created
   public List<EyesLayer.Type> initialFemaleEyes;

   // The hair styles to choose randomly from when the character starts being created
   public List<HairLayer.Type> initialFemaleHair;
         
   [Header("Male")]
   // The eye types to choose randomly from when the character starts being created
   public List<EyesLayer.Type> initialMaleEyes;

   // The hair styles to choose randomly from when the character starts being created
   public List<HairLayer.Type> initialMaleHair;
      
   // Self
   public static CharacterCreationPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;

      _styleGrids = GetComponentsInChildren<CharacterStyleGrid>(true).ToList();

      // Get the RectTransforms of our different screens to animate them
      _rectTransform = transform as RectTransform;

      // Cache data to create character in background thread
      if (!Util.isBatch()) {
         PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Eyes, PaletteDef.Eyes.primary.name, PaletteDef.Tags.STARTER);
         PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Armor, PaletteDef.Armor.primary.name, PaletteDef.Tags.STARTER);
         PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Armor, PaletteDef.Armor.secondary.name, PaletteDef.Tags.STARTER);
         PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Hair, PaletteDef.Armor.primary.name, PaletteDef.Tags.STARTER);
         PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Hair, PaletteDef.Armor.secondary.name, PaletteDef.Tags.STARTER);
      }
   }

   private void Start () {
      // Only enable the "next" button if the name is valid
      nameText.onValueChanged.AddListener((name) => {
         nextButton.interactable = NameUtil.isValid(name);
      });

      hide();
   }

   private void initializeValues () {
      nextButton.interactable = false;

      // Clear the name input field
      nameText.text = "";

      foreach (CharacterStyleGrid grid in _styleGrids) {
         grid.initializeGrid();
      }

      tabbedPanel.initialize();
      perksGrid.initialize();
   }

   private void show () {
      initializeValues();
      Util.enableCanvasGroup(this.canvasGroup);
   }

   private void hide () {
      Util.disableCanvasGroup(this.canvasGroup);
   }

   public void setCharacterBeingCreated (OfflineCharacter offlineChar) {
      _char = offlineChar;

      this.genderSelected(Random.Range(1, 3));

      CharacterCreationSpotFader.self.setColor(circleFaderBackgroundColor);

      show();
   }

   private void randomizeSelectedEyes () {
      EyesLayer.Type eyes = getRandomEyes();
      setEyesType(eyes);
   }

   private void randomizeSelectedHair () {
      HairLayer.Type hair = getRandomHair();
      setHairType(hair);
   }

   private void randomizeSelectedArmor () {
      int armor = getRandomArmor();
      setArmor(armor);
   }

   public void submitCharacterCreation (bool ignorePerkQuestions = false) {
      Perk[] chosenPerks = perksGrid.getAssignedPoints().ToArray();
      int pointsSum = chosenPerks.Sum(perk => perk.points);

      /* Temporarily disabled as it's not working as intended
      if (!ignorePerkQuestions) {
         // If any of the questions hasn't been answered, ask the player to confirm skipping the perks
         if (pointsSum < CreationPerksGrid.AVAILABLE_POINTS) {
            confirmSkipQuestions();
            return;
         }
      }
      */

      PanelManager.self.showConfirmationPanel("Finish creating your character?", () => {
         canvasGroup.interactable = false;
         canvasGroup.blocksRaycasts = false;
         // Send the creation request to the server
         NetworkClient.Send(new CreateUserMessage(Global.netId,
            _char.getUserInfo(), _char.armor.equipmentId, _char.armor.getPalettes(), chosenPerks));

         // Show loading screen until player warps to map
         StartCoroutine(showLoadingScreen());
      });
   }

   private IEnumerator showLoadingScreen() {
      PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.CharacterCreation, SpotFader.self, SpotFader.self);

      while (Global.player == null || AreaManager.self.getArea(Area.STARTING_TOWN) == null || Global.player.transform.parent != AreaManager.self.getArea(Area.STARTING_TOWN).userParent) {
         yield return new WaitForEndOfFrame();
      }

      PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.CharacterCreation, 1);
      PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.CharacterCreation);
   }

   public void onCharacterCreationValid () {      
      hide();

      CharacterCreationSpotFader.self.closeAndHide();
      
      SpotFader.self.fadeBackgroundColor(Color.black, 0.1f);
      SpotFader.self.closeSpot();

      // Show loading screen while starting map is being created
      PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.MapCreation, CameraManager.defaultCamera.getPixelFadeEffect(), SpotFader.self);

      // Return camera to its original position
      CharacterScreen.self.myCamera.setDefaultSettings();
   }

   public void onCharacterCreationFailed () {
      Util.enableCanvasGroup(canvasGroup);
   }

   public void onCancelButtonClicked () {
      PanelManager.self.showConfirmationPanel("Are you sure you want to cancel the character creation?", () => cancelCreating());
   }

   public void confirmSkipQuestions () {
      PanelManager.self.showConfirmationPanel("You haven't finished assigning your perk points.\nThey will remain unassigned until you assign them in the game." +
         "\n\nDo you want to continue?", () => submitCharacterCreation(true));
   }

   public void cancelCreating () {
      CharacterScreen.self.myCamera.setDefaultSettings();
      Destroy(_char.gameObject);
      CharacterCreationSpotFader.self.openSpotToMaxSize();
      hide();
   }

   #region Character Appearance Customization

   public void setHairType (HairLayer.Type hairType) {
      UserInfo info = _char.getUserInfo();
      info.hairType = hairType;
      _char.setBodyLayers(info);

      updateStyleIcons();

      // Enable or disable the secondary color row based on the chosen style
      hairGroup2.gameObject.SetActive(_multiplePaletteHairStyles.Contains(hairType));
   }

   public void setEyesType (EyesLayer.Type type) {
      UserInfo info = _char.getUserInfo();
      info.eyesType = type;
      _char.setBodyLayers(info);

      updateStyleIcons();
   }

   public void setBodyType (BodyLayer.Type type) {
      UserInfo info = _char.getUserInfo();
      info.bodyType = type;
      _char.setBodyLayers(info);

      updateStyleIcons();
   }

   public void setBodyType (int type) {      
      if (_char == null) {
         return;
      }

      UserInfo info = _char.getUserInfo();
      Gender.Type gender = getGender();
      int finalType = gender == Gender.Type.Female ? 200 : 100;
      finalType += type;

      info.bodyType = (BodyLayer.Type)finalType;
      _char.setBodyLayers(info);

      updateStyleIcons();
   }

   public void setArmor (int armorId) {
      _char.setArmor(armorId, getUserObjects().armorPalettes);
      refreshArmor();

      updateStyleIcons();
   }

   public UserObjects getUserObjects () {
      return new UserObjects {
         userInfo = _char.getUserInfo(),
         weapon = _char.getWeapon(),
         armor = _char.getArmor(),
         hat = _char.getHat(),
         armorPalettes = _char.armor.getPalettes(),
      };
   }

   public HairLayer.Type getHairType () {
      return _char.getUserInfo().hairType;
   }

   public EyesLayer.Type getEyesType () {
      return _char.getUserInfo().eyesType;
   }

   public BodyLayer.Type getBodyType () {
      return _char.getUserInfo().bodyType;
   }

   public int getArmorId () {
      return _char.armor.getType();
   }

   public Gender.Type getGender () {      
      return _char != null ? _char.getUserInfo().gender : Gender.Type.Female;
   }

   public void genderSelected (int newGender) {
      Gender.Type gender = (Gender.Type) newGender;

      if (_char == null) {
         return;
      }

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.gender = gender;
      _char.setBodyLayers(info);

      updateBodyTypeGender();

      // The gender is special, in that we need to update the other options afterwards
      randomizeSelectedEyes();
      randomizeSelectedHair();
      randomizeSelectedArmor();

      updateColorBoxes(info.gender);
      refreshHair();
      refreshEyes();
      refreshBody();
      refreshArmor();

      // We have to redo the colors do, since they're different for male and female
      onHairColorChanged();
      onArmorColorChanged();
      onEyeColorChanged();

      updateStyleIcons();

      // Show the right toggle as enabled (without triggering the OnValueChanged event)
      maleToggle.SetIsOnWithoutNotify(gender == Gender.Type.Male);      
      femaleToggle.SetIsOnWithoutNotify(gender == Gender.Type.Female);
   }

   private void updateStyleIcons () {
      foreach (CharacterStyleGrid grid in _styleGrids) {
         grid.updateAllStacks();
      }
   }

   private void updateBodyTypeGender () {
      Gender.Type gender = getGender();
      BodyLayer.Type bodyType = getBodyType();

      int bodyId = ((int) bodyType) % 10;
      int genderId = gender == Gender.Type.Female ? 200 : 100;

      bodyType = (BodyLayer.Type) (genderId + bodyId);
      setBodyType(bodyType);
   }

   public void refreshHair () {
      List<HairLayer.Type> list = getOrderedHairList();

      // Adjust the index
      int currentIndex = list.IndexOf(_char.hairFront.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }

      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.hairType = list[currentIndex];
      _char.setBodyLayers(info);
   }

   public void refreshEyes () {
      List<EyesLayer.Type> list = getEyeList();

      // Adjust the index
      int currentIndex = list.IndexOf(_char.eyes.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }

      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.eyesType = list[currentIndex];
      _char.setBodyLayers(info);
   }

   public void refreshBody () {
      List<BodyLayer.Type> list = getBodyList();

      // Adjust the index
      int currentIndex = list.IndexOf(_char.body.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }
            
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.bodyType = list[currentIndex];
      _char.setBodyLayers(info);
   }

   public void refreshArmor () {
      List<int> list = getArmorList();

      // Adjust the index
      int currentIndex = list.IndexOf(_char.armor.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      Armor armor = _char.getArmor();

      if (CharacterScreen.self.startingArmorData.Count > 0) {
         CharacterScreen.StartingArmorData armorData = CharacterScreen.self.startingArmorData[currentIndex];

         int armorSpriteId = armorData.spriteId;
         _char.armor.equipmentId = armorData.equipmentId;
         _char.setArmor(armorSpriteId, armor.paletteNames);
      } else {
         armor.itemTypeId = list[currentIndex];
         _char.setArmor(armor.itemTypeId, armor.paletteNames);
      }
   }

   public void onHairColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      string palettes = Item.parseItmPalette(new string[2] { getSelected(hairGroup1), getSelected(hairGroup2) });

      // Update the character stack image
      _char.hairBack.recolor(palettes);
      _char.hairFront.recolor(palettes);
   }

   public void onArmorColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      string palettes = Item.parseItmPalette(new string[2] { getSelected(armorGroup1), getSelected(armorGroup2) });

      // Update the character stack image
      _char.armor.recolor(palettes);
   }

   public void onEyeColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      string palette = getSelected(eyeGroup1);

      // Update the character stack image
      _char.eyes.recolor(palette);
   }

   public void updateColorBoxes (Gender.Type genderType) {
      // Fill in the color boxes
      List<PaletteToolManager.PaletteRepresentation> eyes = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Eyes, PaletteDef.Eyes.primary.name, PaletteDef.Tags.STARTER);
      List<PaletteToolManager.PaletteRepresentation> primaryHair = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Hair, PaletteDef.Armor.primary.name, PaletteDef.Tags.STARTER);
      List<PaletteToolManager.PaletteRepresentation> secondaryHair = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Hair, PaletteDef.Armor.secondary.name, PaletteDef.Tags.STARTER);
      List<PaletteToolManager.PaletteRepresentation> armorPrimary = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Armor, PaletteDef.Armor.primary.name, PaletteDef.Tags.STARTER);
      List<PaletteToolManager.PaletteRepresentation> armorSecondary = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Armor, PaletteDef.Armor.secondary.name, PaletteDef.Tags.STARTER);


      fillInColorBoxes(eyeGroup1, eyes);
      fillInColorBoxes(hairGroup1, primaryHair);
      fillInColorBoxes(hairGroup2, secondaryHair);
      fillInColorBoxes(armorGroup1, armorPrimary);
      fillInColorBoxes(armorGroup2, armorSecondary);
   }

   protected string getSelected (ToggleGroup toggleGroup) {
      foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
         if (toggle.isOn) {
            if (toggle.GetComponent<Text>() == null) {
               return "";
            }
            return toggle.GetComponent<Text>().text;
         }
      }

      return "";
   }

   private HairLayer.Type getRandomHair () {
      Gender.Type gender = getGender();

      if (gender == Gender.Type.Female) {
         return initialFemaleHair[Random.Range(0, initialFemaleHair.Count)];
      } else {
         return initialMaleHair[Random.Range(0, initialMaleHair.Count)];
      }
   }

   private EyesLayer.Type getRandomEyes () {
      Gender.Type gender = getGender();

      if (gender == Gender.Type.Female) {
         return initialFemaleEyes[Random.Range(0, initialFemaleEyes.Count)];
      } else {
         return initialMaleEyes[Random.Range(0, initialMaleEyes.Count)];
      }
   }

   private int getRandomArmor () {      
      List<int> armors = getArmorList();
      return armors[Random.Range(0, armors.Count)];
   }


   public List<HairLayer.Type> getOrderedHairList () {
      List<HairLayer.Type> newList = new List<HairLayer.Type>();

      // Do some custom sorting for the female
      if (_char.genderType == Gender.Type.Female) {
         newList.Add(HairLayer.Type.Female_Hair_1);
         newList.Add(HairLayer.Type.Female_Hair_6);
         newList.Add(HairLayer.Type.Female_Hair_7);
         newList.Add(HairLayer.Type.Female_Hair_8);
         newList.Add(HairLayer.Type.Female_Hair_9);
         newList.Add(HairLayer.Type.Female_Hair_10);
      }

      // Do some custom sorting for the male
      if (_char.genderType == Gender.Type.Male) {
         newList.Add(HairLayer.Type.Male_Hair_1);
         newList.Add(HairLayer.Type.Male_Hair_4);
         newList.Add(HairLayer.Type.Male_Hair_5);
         newList.Add(HairLayer.Type.Male_Hair_2);
         newList.Add(HairLayer.Type.Male_Hair_8);
         newList.Add(HairLayer.Type.Male_Hair_7);
      }

      return newList;
   }

   public List<BodyLayer.Type> getBodyList () {
      if (_char.genderType == Gender.Type.Female) {
         return new List<BodyLayer.Type>() {
            BodyLayer.Type.Female_Body_1, BodyLayer.Type.Female_Body_2, BodyLayer.Type.Female_Body_3, BodyLayer.Type.Female_Body_4
         };
      } else {
         return new List<BodyLayer.Type>() {
            BodyLayer.Type.Male_Body_1, BodyLayer.Type.Male_Body_2, BodyLayer.Type.Male_Body_3, BodyLayer.Type.Male_Body_4
         };
      }
   }

   public List<EyesLayer.Type> getEyeList () {
      if (_char.genderType == Gender.Type.Female) {
         return new List<EyesLayer.Type>() { EyesLayer.Type.Female_Eyes_1, EyesLayer.Type.Female_Eyes_2, EyesLayer.Type.Female_Eyes_3 };
      } else {
         return new List<EyesLayer.Type>() { EyesLayer.Type.Male_Eyes_1, EyesLayer.Type.Male_Eyes_2, EyesLayer.Type.Male_Eyes_3 };
      }
   }

   public List<int> getArmorList () {
      if (CharacterScreen.self.startingArmorData.Count > 0) {         
         List<int> list = new List<int>();

         foreach (CharacterScreen.StartingArmorData armorData in CharacterScreen.self.startingArmorData) {
            list.Add(armorData.spriteId);
         }

         return list;
      } else {
         return new List<int>() { 1, 2, 3 };
      }
   }

   protected void fillInColorBoxes (ToggleGroup toggleGroup, List<PaletteToolManager.PaletteRepresentation> paletteList) {
      int index = 0;

      foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
         if (paletteList.Count > index) {
            string paletteName = paletteList[index].name;
            toggle.image.color = paletteList[index].color;
            toggle.group = toggleGroup;

            // Ensure colors can only be clicked on non-transparent areas instead of using the whole rect
            toggle.image.alphaHitTestMinimumThreshold = 0.1f;
            Text text = toggle.GetComponent<Text>() ? toggle.GetComponent<Text>() : toggle.gameObject.AddComponent<Text>();
            text.enabled = false;
            text.text = paletteName;

            index++;
         }
      }
   }

   #endregion

   #region Private Variables

   // The character associated with this panel
   protected OfflineCharacter _char;

   // The list containing all the grids for the different styles
   private List<CharacterStyleGrid> _styleGrids = new List<CharacterStyleGrid>();

   // The transform as a rect transform
   private RectTransform _rectTransform;

   #endregion
}
