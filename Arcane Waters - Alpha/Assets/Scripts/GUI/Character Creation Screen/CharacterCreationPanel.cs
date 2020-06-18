using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using DG.Tweening;
using TMPro;
using System;

public class CharacterCreationPanel : ClientMonoBehaviour
{
   #region Public Variables

   // The different sections in the screen
   public enum Section
   {
      Appearance = 1,
      Questions = 2
   }

   // The text to display on the "next" button to indicate there are more steps after the current one
   public const string NEXT_STEP_BUTTON_TEXT = "Next";

   // The text to display on the "next" button to indicate confirming the answer will finish the questionnaire
   public const string CONFIRM_QUESTIONNAIRE_BUTTON_TEXT = "Finish";

   // The text to display on the "back" button to indicate the player can go to the previous step
   public const string BACK_STEP_BUTTON_TEXT = "Back";

   // The text to display on the "back" button to indicate clicking it will cancel character creation
   public const string CANCEL_CREATION_BUTTON_TEXT = "Cancel";

   // The time (in seconds) for the moving panel animations
   public const float WINDOW_TRANSITION_TIME = 0.25f;

   [Header("References")]
   // The button to go to the next screen
   public Button nextButton;

   // The button to go to the previous screen
   public Button backButton;

   // The button to skip the perk questions
   public Button skipButton;

   // The text of the "next"/"finish" button
   public Text nextButtonText;

   // The text of the "back"/"cancel" button
   public Text backButtonText;

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // Our toggle groups
   public ToggleGroup hairGroup1;
   public ToggleGroup hairGroup2;
   public ToggleGroup armorGroup1;
   public ToggleGroup armorGroup2;
   public ToggleGroup eyeGroup1;

   // The Text that contains our name
   public InputField nameText;

   // The perk questions screen
   public CharacterCreationQuestionsScreen questionsScreen;

   // The appearance screen
   public GameObject appearanceScreen;

   [Header("Settings")]
   // The color for the background of the spot
   public Color circleFaderBackgroundColor = new Color(0, 0, 0, .75f);

   // The material used for the "cancel" button
   public Material cancelButtonMaterial;

   // The material used for the "confirm" button
   public Material confirmButtonMaterial;

   // The material used for buttons when they're not cancel/confirm
   public Material generalButtonMaterial;

   // Self
   public static CharacterCreationPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;

      // Get the RectTransforms of our different screens to animate them
      _rectTransform = transform as RectTransform;
      _appearanceScreenRectTransform = appearanceScreen.transform as RectTransform;
   }

   private void Start () {
      // Only enable the "next" button if the name is valid
      nameText.onValueChanged.AddListener((name) => {
         nextButton.interactable = NameUtil.isValid(name);
      });

      questionsScreen.gameObject.SetActive(false);
      initializeValues();
      hide();
   }

   private void initializeValues () {
      _leftPanelPosition = -_appearanceScreenRectTransform.rect.width;
      _rightPanelPosition = questionsScreen.rectTransform.rect.width;
            
      questionsScreen.rectTransform.anchoredPosition = new Vector2(_rightPanelPosition, questionsScreen.rectTransform.anchoredPosition.y);
      _appearanceScreenRectTransform.anchoredPosition = Vector2.zero;

      // "Next" button is disabled by default
      nextButton.interactable = false;

      // The "Skip" button isn't shown until the perk questions step
      skipButton.gameObject.SetActive(false);

      // Set up our next/back buttons
      setNextButtonToNormal();
      setBackButtonToCancel();

      // Clear the name input field
      nameText.text = "";

      canvasGroup.alpha = 1;
      canvasGroup.interactable = true;

      _currentSection = Section.Appearance;
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

      this.genderSelected((int) _char.genderType);

      questionsScreen.gameObject.SetActive(true);
      questionsScreen.startQuestionnaire();

      SpotFader.self.setColor(circleFaderBackgroundColor);

      show();
   }

   public void submitCharacterCreation (bool ignorePerkQuestions = false) {
      canvasGroup.interactable = false;

      List<int> chosenAnswers = questionsScreen.chosenAnswers;

      // If the perk questions are skipped, send a list with "unassigned perk" values
      if (ignorePerkQuestions) {
         chosenAnswers = new List<int>();

         for (int i = 0; i < questionsScreen.questions.Count; i++) {
            chosenAnswers.Add(-1);
         }
      }

      // Send the creation request to the server
      NetworkClient.Send(new CreateUserMessage(Global.netId,
         _char.getUserInfo(), _char.armor.equipmentId, _char.armor.getPalette1(), _char.armor.getPalette2(), chosenAnswers));
   }

   public void onCharacterCreationValid () {
      questionsScreen.gameObject.SetActive(false);
      hide();

      SpotFader.self.fadeBackgroundColor(Color.black, 0.25f);
      SpotFader.self.closeSpot();
      PanelManager.self.loadingScreen.show();

      // Return camera to its original position
      CharacterScreen.self.myCamera.setDefaultSettings();
   }

   public void onCharacterCreationFailed () {
      canvasGroup.interactable = true;
   }

   public void onNextButtonClicked () {
      if (_currentSection == Section.Appearance) {

         // Cache the user info
         Global.userObjects = new UserObjects {
            userInfo = _char.getUserInfo(),
            weapon = _char.getWeapon(),
            armor = _char.getArmor(),
            hat = _char.getHat()
         };
         goToQuestions();
      } else {
         questionsScreen.confirmAnswerClicked();
      }
   }

   public void onBackButtonClicked () {
      if (_currentSection == Section.Questions) {
         questionsScreen.previousQuestionClicked();
      } else {
         PanelManager.self.showConfirmationPanel("Are you sure you want to cancel the character creation?", () => cancelCreating());
      }
   }

   public void onSkipButtonClicked () {
      if (_currentSection == Section.Questions) {
         PanelManager.self.showConfirmationPanel("Are you sure you want to skip the questions?\n\nYour perk points will remain unassigned until you assign them in the game.", () => submitCharacterCreation(true));
      }
   }

   public void goToQuestions () {
      _currentSequence?.Kill();

      _currentSequence = DOTween.Sequence();
      _currentSequence.Join(questionsScreen.rectTransform.DOAnchorPosX(0.0f, WINDOW_TRANSITION_TIME));
      _currentSequence.Join(_appearanceScreenRectTransform.DOAnchorPosX(_leftPanelPosition, WINDOW_TRANSITION_TIME));

      _currentSequence.AppendCallback(() => {
         // Enable the "back" button so we can return if we want to change appearance
         setBackButtonToNormal();

         skipButton.gameObject.SetActive(true);

         // Update the current section
         _currentSection = Section.Questions;

         // Tell the questions screen it's being shown
         questionsScreen.onShown();
      });

      _currentSequence.Play();
   }

   public void returnToAppearanceScreen () {
      _currentSequence?.Kill();

      _currentSequence = DOTween.Sequence();
      _currentSequence.Join(questionsScreen.rectTransform.DOAnchorPosX(_rightPanelPosition, WINDOW_TRANSITION_TIME));
      _currentSequence.Join(_appearanceScreenRectTransform.DOAnchorPosX(0.0f, WINDOW_TRANSITION_TIME));

      _currentSequence.AppendCallback(() => {
         setBackButtonToCancel();

         // Re-enable the "next" button
         nextButton.interactable = NameUtil.isValid(nameText.text);

         // Disable the "skip" button in the appearance section
         skipButton.gameObject.SetActive(false);

         _currentSection = Section.Appearance;
      });

      _currentSequence.Play();
   }

   public void cancelCreating () {
      CharacterScreen.self.myCamera.setDefaultSettings();
      Destroy(_char.gameObject);
      SpotFader.self.openSpotToMaxSize();
      hide();
   }

   #region Buttons Personalization

   public void setNextButtonToNormal () {
      nextButton.targetGraphic.material = generalButtonMaterial;
      nextButtonText.text = NEXT_STEP_BUTTON_TEXT;
   }

   public void setNextButtonToFinish () {
      nextButton.targetGraphic.material = confirmButtonMaterial;
      nextButtonText.text = CONFIRM_QUESTIONNAIRE_BUTTON_TEXT;
   }

   public void setBackButtonToNormal () {
      backButton.targetGraphic.material = generalButtonMaterial;
      backButtonText.text = BACK_STEP_BUTTON_TEXT;
   }

   public void setBackButtonToCancel () {
      backButton.targetGraphic.material = cancelButtonMaterial;
      backButtonText.text = CANCEL_CREATION_BUTTON_TEXT;
   }

   #endregion

   #region Character Appearance Customization

   public void genderSelected (int newGender) {
      Gender.Type gender = (Gender.Type) newGender;

      if (_char == null) {
         return;
      }

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.gender = gender;
      _char.setBodyLayers(info);

      // The gender is special, in that we need to update the other options afterwards
      updateColorBoxes(info.gender);
      changeHair(0);
      changeEyes(0);
      changeBody(0);
      changeArmor(0);

      // We have to redo the colors do, since they're different for male and female
      onHairColorChanged();
      onArmorColorChanged();
      onEyeColorChanged();
   }

   public void changeHair (int offset) {
      List<HairLayer.Type> list = getOrderedHairList();

      // Adjust the index
      int currentIndex = list.IndexOf(_char.hairFront.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex += offset;
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.hairType = list[currentIndex];
      _char.setBodyLayers(info);
   }

   public void changeEyes (int offset) {
      List<EyesLayer.Type> list = getEyeList();

      // Adjust the index
      int currentIndex = list.IndexOf(_char.eyes.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex += offset;
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.eyesType = list[currentIndex];
      _char.setBodyLayers(info);
   }

   public void changeBody (int offset) {
      List<BodyLayer.Type> list = getBodyList();

      // Adjust the index
      int currentIndex = list.IndexOf(_char.body.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex += offset;
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      UserInfo info = _char.getUserInfo();
      info.bodyType = list[currentIndex];
      _char.setBodyLayers(info);
   }

   public void changeArmor (int offset) {
      List<int> list = new List<int>() { 1, 2, 3 };
      if (CharacterScreen.self.startingArmorData.Count > 0) {
         list.Clear();
         foreach (CharacterScreen.StartingArmorData armorData in CharacterScreen.self.startingArmorData) {
            list.Add(armorData.spriteId);
         }
      }

      // Adjust the index
      int currentIndex = list.IndexOf(_char.armor.getType());
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex += offset;
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the Info and apply it to the character
      Armor armor = _char.getArmor();

      if (CharacterScreen.self.startingArmorData.Count > 0) {
         CharacterScreen.StartingArmorData armorData = CharacterScreen.self.startingArmorData[currentIndex];

         int armorSpriteId = armorData.spriteId;
         _char.armor.equipmentId = armorData.equipmentId;
         _char.setArmor(armorSpriteId, armor.paletteName1, armor.paletteName2);
      } else {
         armor.itemTypeId = list[currentIndex];
         _char.setArmor(armor.itemTypeId, armor.paletteName1, armor.paletteName2);
      }
   }

   public void onHairColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      string palette1 = getSelected(hairGroup1);
      //string palette2 = getSelected(hairGroup2);

      // Update the character stack image
      _char.hairBack.recolor(palette1);
      _char.hairFront.recolor(palette1);
   }

   public void onArmorColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      string palette1 = getSelected(armorGroup1);
      string palette2 = getSelected(armorGroup2);

      // Update the character stack image
      _char.armor.recolor(palette1, palette2);
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
      fillInColorBoxes(eyeGroup1, _eyePalettes);
      fillInColorBoxes(hairGroup1, (genderType == Gender.Type.Male) ? _maleHairPalettes : _femaleHairPalettes);
      fillInColorBoxes(hairGroup2, (genderType == Gender.Type.Male) ? _maleHairPalettes : _femaleHairPalettes);
      fillInColorBoxes(armorGroup1, (genderType == Gender.Type.Male) ? _maleArmorPalettes1 : _femaleArmorPalettes1);
      fillInColorBoxes(armorGroup2, (genderType == Gender.Type.Male) ? _maleArmorPalettes2 : _femaleArmorPalettes2);
   }

   protected string getSelected (ToggleGroup toggleGroup) {
      foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
         if (toggle.isOn) {
            return toggle.GetComponent<Text>().text;
         }
      }

      return "";
   }

   protected List<HairLayer.Type> getOrderedHairList () {
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

   protected List<BodyLayer.Type> getBodyList () {
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

   protected List<EyesLayer.Type> getEyeList () {
      if (_char.genderType == Gender.Type.Female) {
         return new List<EyesLayer.Type>() { EyesLayer.Type.Female_Eyes_1, EyesLayer.Type.Female_Eyes_2, EyesLayer.Type.Female_Eyes_3 };
      } else {
         return new List<EyesLayer.Type>() { EyesLayer.Type.Male_Eyes_1, EyesLayer.Type.Male_Eyes_2, EyesLayer.Type.Male_Eyes_3 };
      }
   }

   protected void fillInColorBoxes (ToggleGroup toggleGroup, List<string> paletteList) {
      int index = 0;

      foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
         if (paletteList.Count > index) {
            string paletteName = paletteList[index++];
            toggle.image.color = PaletteSwapManager.getRepresentingColor(paletteName);

            Text text = toggle.GetComponent<Text>() ? toggle.GetComponent<Text>() : toggle.gameObject.AddComponent<Text>();
            text.enabled = false;
            text.text = paletteName;
         }
      }
   }

   #endregion

   #region Private Variables

   // The character associated with this panel
   protected OfflineCharacter _char;

   // The allowed colors we can choose from
   protected List<string> _eyePalettes = new List<string>() { PaletteDef.Eyes.Blue, PaletteDef.Eyes.Brown, PaletteDef.Eyes.Green, PaletteDef.Eyes.Purple, PaletteDef.Eyes.Black };
   protected List<string> _maleHairPalettes = new List<string>() { PaletteDef.Hair.Brown, PaletteDef.Hair.Red, PaletteDef.Hair.Black, PaletteDef.Hair.Yellow, PaletteDef.Hair.White };
   protected List<string> _femaleHairPalettes = new List<string>() { PaletteDef.Hair.Brown, PaletteDef.Hair.Red, PaletteDef.Hair.Black, PaletteDef.Hair.Yellow, PaletteDef.Hair.Blue };
   protected List<string> _maleArmorPalettes1 = new List<string>() { PaletteDef.Armor1.Brown, PaletteDef.Armor1.Red, PaletteDef.Armor1.Blue, PaletteDef.Armor1.White, PaletteDef.Armor1.Green };
   protected List<string> _maleArmorPalettes2 = new List<string>() { PaletteDef.Armor2.Brown, PaletteDef.Armor2.White, PaletteDef.Armor2.Blue, PaletteDef.Armor2.Red, PaletteDef.Armor2.Green };
   protected List<string> _femaleArmorPalettes1 = new List<string>() { PaletteDef.Armor1.Brown, PaletteDef.Armor1.Red, PaletteDef.Armor1.Yellow, PaletteDef.Armor1.Blue, PaletteDef.Armor1.Teal };
   protected List<string> _femaleArmorPalettes2 = new List<string>() { PaletteDef.Armor2.Brown, PaletteDef.Armor2.White, PaletteDef.Armor2.Yellow, PaletteDef.Armor2.Blue, PaletteDef.Armor2.Teal };

   // The DOTween Squence that animates the different panels
   private Sequence _currentSequence;

   // The rect transform of the appearance screen
   private RectTransform _appearanceScreenRectTransform;

   // The transform as a rect transform
   private RectTransform _rectTransform;

   // The position at which a panel is invisible at the right
   private float _rightPanelPosition = 236.0f;

   // The position at which a panel is invisible at the left
   private float _leftPanelPosition = -236.0f;

   // The current customization section
   private Section _currentSection = Section.Appearance;

   #endregion
}
