using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System;

public class GuildCreatePanel : Panel {
   #region Public Variables

   // The main guild icon
   public GuildIcon guildIcon;

   // The guild icon used to display the background selection
   public GuildIcon backgroundSelection;

   // The guild icon used to display the sigil selection
   public GuildIcon sigilSelection;

   // The prefab we use to create color toggles
   public Toggle colorTogglePrefab;

   // The color toggle groups and containers
   public ToggleGroup backgroundColorGroup1;
   public ToggleGroup backgroundColorGroup2;
   public ToggleGroup sigilColorGroup1;
   public ToggleGroup sigilColorGroup2;

   // The icon displaying the valid or invalid guild name icon
   public Image nameValidIcon;

   // The sprites to use when the guild name is valid or invalid
   public Sprite nameValidSprite;
   public Sprite nameInvalidSprite;

   // The message indicating why the guild name is invalid
   public Text nameErrorText;

   #endregion

   public override void Awake () {
      base.Awake(); 

      _inputField = GetComponentInChildren<InputField>();

      // Keep a list of all sprites for guild icon layers
      _borders.Clear();
      foreach (ImageManager.ImageData imgData in ImageManager.getSpritesInDirectory(GuildIcon.BORDER_PATH)) {
         _borders.Add(imgData.imageName);
      }

      _backgrounds.Clear();
      foreach (ImageManager.ImageData imgData in ImageManager.getSpritesInDirectory(GuildIcon.BACKGROUND_PATH)) {
         _backgrounds.Add(imgData.imageName);
      }

      _sigils.Clear();
      foreach (ImageManager.ImageData imgData in ImageManager.getSpritesInDirectory(GuildIcon.SIGIL_PATH)) {
         _sigils.Add(imgData.imageName);
      }
   }

   public override void Start () {
      base.Start();

      // Set default values
      _borderIndex = UnityEngine.Random.Range(0, _borders.Count);
      _backgroundIndex = UnityEngine.Random.Range(0, _backgrounds.Count);
      _sigilIndex = UnityEngine.Random.Range(0, _sigils.Count);

      // Clear out any old info
      backgroundColorGroup1.gameObject.DestroyChildren();
      backgroundColorGroup2.gameObject.DestroyChildren();
      sigilColorGroup1.gameObject.DestroyChildren();
      sigilColorGroup2.gameObject.DestroyChildren();

      instantiateColorToggles(PaletteDef.guildIcon1, backgroundColorGroup1, onBackgroundColor1TogglePress);
      instantiateColorToggles(PaletteDef.guildIcon2, backgroundColorGroup2, onBackgroundColor2TogglePress);
      instantiateColorToggles(PaletteDef.guildIcon1, sigilColorGroup1, onSigilColor1TogglePress);
      instantiateColorToggles(PaletteDef.guildIcon2, sigilColorGroup2, onSigilColor2TogglePress);
      refreshBorder();

      nameErrorText.text = "";
      nameValidIcon.enabled = false;
   }

   private void instantiateColorToggles (Dictionary<string, Color> palettes, ToggleGroup group, Action<bool, string> onValueChangedAction) {
      int selectedToggleIndex = UnityEngine.Random.Range(0, palettes.Count);
      int k = 0;

      foreach (KeyValuePair<string, Color> KV in palettes) {
         // Make sure the value is captured for the click event
         string paletteName = KV.Key;

         Toggle colorToggle = Instantiate(colorTogglePrefab, group.transform, false);
         colorToggle.group = group;
         colorToggle.onValueChanged.AddListener((_) => onValueChangedAction(_, paletteName));
         colorToggle.image.color = KV.Value;

         // Initialize a random toggle as selected
         if (k == selectedToggleIndex) {
            colorToggle.isOn = true;
            onValueChangedAction(true, KV.Key);
         }
         k++;
      }
   }

   public void onCreateButtonPressed () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(createGuildConfirmed);

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to create the guild " + _inputField.text + "?");
   }

   public void onCancelButtonPressed () {
      PanelManager.self.popPanel();
   }

   public void createGuildConfirmed () {
      PanelManager.self.confirmScreen.hide();
      Global.player.rpc.Cmd_CreateGuild(_inputField.text, _borders[_borderIndex], _backgrounds[_backgroundIndex],
         _sigils[_sigilIndex], Item.parseItmPalette(new string[2] { _backgroundPalette1, _backgroundPalette2 }), Item.parseItmPalette(new string[2]{_sigilPalette1, _sigilPalette2}));
   }

   public void onGuildNameChange() {
      if (_inputField.text == "") {
         nameValidIcon.enabled = false;
         nameErrorText.text = "";
      } else {
         if (!nameValidIcon.enabled) {
            nameValidIcon.enabled = true;
         }

         if (!GuildManager.self.isGuildNameValid(_inputField.text, out string errorMessage)) {
            nameErrorText.text = errorMessage;
            nameValidIcon.sprite = nameInvalidSprite;
         } else {
            nameErrorText.text = "The name is valid.";
            nameValidIcon.sprite = nameValidSprite;
         }
      }
   }

   public void onPreviousBorderPress () {
      _borderIndex--;
      if (_borderIndex < 0) {
         _borderIndex = _borders.Count - 1;
      }
      refreshBorder();
   }

   public void onPreviousBackgroundPress () {
      _backgroundIndex--;
      if (_backgroundIndex < 0) {
         _backgroundIndex = _backgrounds.Count - 1;
      }
      refreshBackground();
   }

   public void onPreviousSigilPress () {
      _sigilIndex--;
      if (_sigilIndex < 0) {
         _sigilIndex = _sigils.Count - 1;
      }
      refreshSigil();
   }

   public void onNextBorderPress () {
      _borderIndex++;
      if (_borderIndex >= _borders.Count) {
         _borderIndex = 0;
      }
      refreshBorder();
   }

   public void onNextBackgroundPress () {
      _backgroundIndex++;
      if (_backgroundIndex >= _backgrounds.Count) {
         _backgroundIndex = 0;
      }
      refreshBackground();
   }

   public void onNextSigilPress () {
      _sigilIndex++;
      if (_sigilIndex >= _sigils.Count) {
         _sigilIndex = 0;
      }
      refreshSigil();
   }

   public void onBackgroundColor1TogglePress(bool isOn, string paletteName) {
      if (isOn) {
         _backgroundPalette1 = paletteName;
         refreshBackground();
      }
   }

   public void onBackgroundColor2TogglePress (bool isOn, string paletteName) {
      if (isOn) {
         _backgroundPalette2 = paletteName;
         refreshBackground();
      }
   }

   public void onSigilColor1TogglePress (bool isOn, string paletteName) {
      if (isOn) {
         _sigilPalette1 = paletteName;
         refreshSigil();
      }
   }

   public void onSigilColor2TogglePress (bool isOn, string paletteName) {
      if (isOn) {
         _sigilPalette2 = paletteName;
         refreshSigil();
      }
   }

   private void refreshBorder () {
      guildIcon.setBorder(_borders[_borderIndex]);
   }

   private void refreshBackground () {
      guildIcon.setBackground(_backgrounds[_backgroundIndex], Item.parseItmPalette(new string[2] { _backgroundPalette1, _backgroundPalette2 }));
      backgroundSelection.setBackground(_backgrounds[_backgroundIndex], Item.parseItmPalette(new string[2] { _backgroundPalette1, _backgroundPalette2 }));
   }

   private void refreshSigil () {
      guildIcon.setSigil(_sigils[_sigilIndex], Item.parseItmPalette(new string[2] { _sigilPalette1, _sigilPalette2 }));
      sigilSelection.setSigil(_sigils[_sigilIndex], Item.parseItmPalette(new string[2] { _sigilPalette1, _sigilPalette2 }));
   }

   #region Private Variables

   // Our Input Field
   protected InputField _inputField;

   // The lists of all icon layer sprites
   private List<string> _borders = new List<string>();
   private List<string> _backgrounds = new List<string>();
   private List<string> _sigils = new List<string>();

   // The indexes of the selected layer sprites
   private int _borderIndex = 0;
   private int _backgroundIndex = 0;
   private int _sigilIndex = 0;

   // The names of the selected palettes
   private string _backgroundPalette1 = PaletteDef.guildIcon1.RandomKey();
   private string _backgroundPalette2 = PaletteDef.guildIcon2.RandomKey();
   private string _sigilPalette1 = PaletteDef.guildIcon1.RandomKey();
   private string _sigilPalette2 = PaletteDef.guildIcon2.RandomKey();

   #endregion
}
