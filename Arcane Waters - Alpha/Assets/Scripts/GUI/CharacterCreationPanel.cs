using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class CharacterCreationPanel : ClientMonoBehaviour {
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // Icons we manage
   public Image classIcon;
   public Image specialtyIcon;
   public Image factionIcon;

   // Our toggle groups
   public ToggleGroup hairGroup1;
   public ToggleGroup hairGroup2;
   public ToggleGroup armorGroup1;
   public ToggleGroup armorGroup2;
   public ToggleGroup eyeGroup1;

   // The Text that contains our name
   public Text nameText;

   // The Text that contains our class type
   public Text classText;
   public Text classDescriptionText;

   // The Text that contains our specialty
   public Text specialtyText;
   public Text specialtyDescriptionText;

   // The Text that contains our faction
   public Text factionText;
   public Text factionDescriptionText;

   // Self
   public static CharacterCreationPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Update () {
      // By default, hide the character creation canvas group
      Util.disableCanvasGroup(this.canvasGroup);

      // If the Character screen is not showing, we don't need to do anything
      if (!CharacterScreen.self.isShowing()) {
         return;
      }

      // If a character is being created, then we show the panel
      if (_char != null && _char.creationMode) {
         Util.enableCanvasGroup(this.canvasGroup);
      }
   }

   public void setCharacterBeingCreated (OfflineCharacter offlineChar) {
      _char = offlineChar;

      this.genderSelected((int) _char.genderType);

      // Update the text for our selections
      changeClass(0);
      changeSpecialty(0);
      changeFaction(0);
   }

   public void doneCreatingCharacterButtonWasPressed () {
      // Temporarily turn off the buttons
      StartCoroutine(CO_temporarilyDisableInput());

      // Send the creation request to the server
      NetworkClient.Send(new CreateUserMessage(Global.netId,
         _char.getUserInfo(), _char.armor.equipmentId, _char.armor.getColor1(), _char.armor.getColor2()));
   }

   public void cancelCreating () {
      Destroy(_char.gameObject);
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
         MaterialType armorMaterialType = armorData.materialType;
         _char.armor.equipmentId = armorData.equipmentId;
         _char.setArmor(armorSpriteId, armor.color1, armor.color2, armorMaterialType);
      } else {
         armor.itemTypeId = list[currentIndex];
         _char.setArmor(armor.itemTypeId, armor.color1, armor.color2);
      }
   }

   public void changeClass (int offset) {
      List<Class.Type> list = new List<Class.Type>();
      foreach (Class.Type classType in System.Enum.GetValues(typeof(Class.Type))) {
         if (classType != Class.Type.None) {
            list.Add(classType);
         }
      }

      // Adjust the index
      int currentIndex = list.IndexOf(_char.classType);
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex += offset;
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the data
      _char.classType = list[currentIndex];
      classText.text = _char.classType + "";
      classDescriptionText.text = Class.getDescription(_char.classType);

      // Update the icon
      classIcon.sprite = ImageManager.getSprite("Icons/Classes/class_" + _char.classType);
   }

   public void changeSpecialty (int offset) {
      List<Specialty.Type> list = new List<Specialty.Type>();
      foreach (Specialty.Type type in System.Enum.GetValues(typeof(Specialty.Type))) {
         if (type != Specialty.Type.None) {
            list.Add(type);
         }
      }

      // Adjust the index
      int currentIndex = list.IndexOf(_char.specialty);
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex += offset;
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the data
      _char.specialty = list[currentIndex];
      specialtyText.text = Specialty.toString(_char.specialty);
      specialtyDescriptionText.text = Specialty.getDescription(_char.specialty);

      // Update the icon
      specialtyIcon.sprite = Specialty.getIcon(_char.specialty);
   }

   public void changeFaction (int offset) {
      List<Faction.Type> list = new List<Faction.Type>();
      foreach (Faction.Type type in System.Enum.GetValues(typeof(Faction.Type))) {
         if (type != Faction.Type.None) {
            list.Add(type);
         }
      }

      // Adjust the index
      int currentIndex = list.IndexOf(_char.faction);
      if (currentIndex == -1) {
         currentIndex = 0;
      }
      currentIndex += offset;
      currentIndex = (currentIndex + list.Count) % list.Count;

      // Update the data
      _char.faction = list[currentIndex];
      factionText.text = Faction.toString(_char.faction);
      factionDescriptionText.text = Faction.getDescription(_char.faction);

      // Update the icon
      factionIcon.sprite = Faction.getIcon(_char.faction);
   }

   public void onHairColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      ColorType newColor1 = getSelected(hairGroup1);
      ColorType newColor2 = getSelected(hairGroup2);

      // Update the character stack image
      _char.hairBack.recolor(newColor1, newColor2);
      _char.hairFront.recolor(newColor1, newColor2);
   }

   public void onArmorColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      ColorType newColor1 = getSelected(armorGroup1);
      ColorType newColor2 = getSelected(armorGroup2);

      // Update the character stack image
      _char.armor.recolor(newColor1, newColor2);
   }

   public void onEyeColorChanged () {
      if (_char == null) {
         return;
      }

      // Figure out which ColorType that corresponds to
      ColorType newColor1 = getSelected(eyeGroup1);

      // Update the character stack image
      _char.eyes.recolor(newColor1, newColor1);
   }

   public void updateColorBoxes (Gender.Type genderType) {
      // Fill in the color boxes
      fillInColorBoxes(eyeGroup1, _eyeColors);
      fillInColorBoxes(hairGroup1, (genderType == Gender.Type.Male) ? _maleHairColors : _femaleHairColors);
      fillInColorBoxes(hairGroup2, (genderType == Gender.Type.Male) ? _maleHairColors : _femaleHairColors);
      fillInColorBoxes(armorGroup1, (genderType == Gender.Type.Male) ? _maleArmorColors1 : _femaleArmorColors1);
      fillInColorBoxes(armorGroup2, (genderType == Gender.Type.Male) ? _maleArmorColors2 : _femaleArmorColors2);
   }

   protected ColorType getSelected (ToggleGroup toggleGroup) {
      foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
         if (toggle.isOn) {
            return ColorManager.getColor(toggle.image.color);
         }
      }

      return ColorType.None;
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
         return new List<EyesLayer.Type>() { EyesLayer.Type.Female_Eyes_1, EyesLayer.Type.Female_Eyes_2 , EyesLayer.Type.Female_Eyes_3 };
      } else {
         return new List<EyesLayer.Type>() { EyesLayer.Type.Male_Eyes_1, EyesLayer.Type.Male_Eyes_2, EyesLayer.Type.Male_Eyes_3 };
      }
   }

   protected void fillInColorBoxes (ToggleGroup toggleGroup, List<ColorType> colorTypeList) {
      int index = 0;

      foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>()) {
         if (colorTypeList.Count > index) {
            ColorType colorType = colorTypeList[index++];
            toggle.image.color = ColorDef.get(colorType).color;
         }
      }
   }

   protected IEnumerator CO_temporarilyDisableInput () {
      this.canvasGroup.interactable = false;

      yield return new WaitForSeconds(1.25f);

      this.canvasGroup.interactable = true;
   }

   #region Private Variables

   // The character associated with this panel
   protected OfflineCharacter _char;

   // The allowed colors we can choose from
   protected List<ColorType> _eyeColors = new List<ColorType>() { ColorType.Blue, ColorType.Brown, ColorType.GreenEyes, ColorType.PurpleEyes, ColorType.BlackEyes };
   protected List<ColorType> _maleHairColors = new List<ColorType>() { ColorType.Brown, ColorType.Red, ColorType.Black, ColorType.Yellow, ColorType.White };
   protected List<ColorType> _femaleHairColors = new List<ColorType>() { ColorType.Brown, ColorType.Red, ColorType.Black, ColorType.Yellow, ColorType.Blue };
   protected List<ColorType> _maleArmorColors1 = new List<ColorType>() { ColorType.Brown, ColorType.Red, ColorType.Blue, ColorType.White, ColorType.Green };
   protected List<ColorType> _maleArmorColors2 = new List<ColorType>() { ColorType.Brown, ColorType.White, ColorType.Blue, ColorType.Red, ColorType.Green };
   protected List<ColorType> _femaleArmorColors1 = new List<ColorType>() { ColorType.Brown, ColorType.Red, ColorType.Yellow, ColorType.Blue, ColorType.Teal };
   protected List<ColorType> _femaleArmorColors2 = new List<ColorType>() { ColorType.Brown, ColorType.White, ColorType.Yellow, ColorType.Blue, ColorType.Teal };

   #endregion
}
