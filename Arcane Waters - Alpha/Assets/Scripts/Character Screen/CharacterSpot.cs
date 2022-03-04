using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using DG.Tweening;
using TMPro;

public class CharacterSpot : ClientMonoBehaviour {
   #region Public Variables

   // The number associated with this spot
   public int number;

   // The Offline Character in this spot, if any
   public OfflineCharacter character;

   // The character create Button for this spot
   public Button charCreateButton;

   // The character select Button for this spot
   public Button charSelectButton;

   // The character delete Button for this spot
   public Button charDeleteButton;

   // The transform values for the camera when creating a character for this spot
   public Transform spotCamera;

   // Last CharacterSpot that the user interacted with
   public static CharacterSpot lastInteractedSpot;

   // Buttons for rotating character
   public GameObject rotationButtons;

   // A reference to the canvas group containing the buttons for this character spot
   public CanvasGroup buttonsCanvasGroup;

   // The duration of the fading of UI elements for character spots
   public static float FADE_TIME = 0.5f;

   // Is the character displaying a deleted character?
   public bool isDeletedCharacter = false;

   // Reference to the button that restores characters
   public Button charRestoreButton;

   // Reference to the container of the restore button
   public RectTransform charRestoreButtonContainer;

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   void Update () {
      charCreateButton.transform.parent.gameObject.SetActive(character == null || isDeletedCharacter);
      charDeleteButton.transform.parent.gameObject.SetActive(character != null && !character.creationMode && !isDeletedCharacter);
      charSelectButton.transform.parent.gameObject.SetActive(character != null && !CharacterScreen.self.isCreatingCharacter() && !isDeletedCharacter);

      charRestoreButton.gameObject.SetActive(isDeletedCharacter);
      charRestoreButtonContainer.sizeDelta = new Vector2(charRestoreButtonContainer.sizeDelta.x, isDeletedCharacter ? 0.65f : 0.4f);

      // Sync canvas visibility with canvas group interactable flag. UINavigation requirement.
      buttonsCanvasGroup.gameObject.SetActive(!(!CharacterScreen.self.canvasGroup.interactable || CharacterScreen.self.canvasGroup.alpha < 0.01f));
   }

   public void assignCharacter (OfflineCharacter character) {
      // Destroy any existing character
      if (this.character != null) {
         Destroy(this.character.gameObject);
      }

      // Store the character
      this.character = character;

      // Keep it parented under this spot
      character.transform.SetParent(this.transform, true);
   }

   public void selectButtonWasPressed () {
      lastInteractedSpot = this;

      // Turn off buttons until we receive a response from the server
      CharacterScreen.self.canvasGroup.interactable = false;
      
      // Set post spot fader to isLoggingIn every character login so that pixel camera effect can trigger every login
      PostSpotFader.self.isLoggingIn = true;

      // Show loading screen until player warps to map
      PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.MapCreation);

      LoadingUtil.executeAfterFade(() => {
         // Update our currently selected user ID
         Global.currentlySelectedUserId = character.userId;

         // Cache the user info
         Global.setUserObject(new UserObjects {
            userInfo = character.getUserInfo(),
            weapon = character.getWeapon(),
            armor = character.getArmor(),
            hat = character.getHat()
         });

         // Now go ahead and call ClientScene.AddPlayer() along with our currently selected user ID
         ClientManager.sendAccountNameAndUserId();
      });
   }

   public void deleteButtonWasPressed () {
      lastInteractedSpot = this;

      // Turn off buttons until we receive a response from the server
      CharacterScreen.self.canvasGroup.interactable = false;

      PanelManager.self.confirmScreen.hideEvent.RemoveAllListeners();
      PanelManager.self.confirmScreen.hideEvent.AddListener(() => {
         CharacterScreen.self.canvasGroup.interactable = true;
      });

      // Deactive "Confirm" Button until player types in the word "Delete" into the inputfield
      PanelManager.self.confirmScreen.enableConfirmInputField("DELETE");

      // Ask the player for confirmation. Reenable the canvas group if cancelled deletion.
      PanelManager.self.showConfirmationPanel("Are you sure you want to delete " + character.nameText.text + "?", () => sendDeleteUserRequest(character.userId), () => CharacterScreen.self.canvasGroup.interactable = true);
   }

   public void restoreButtonWasPressed () {
      lastInteractedSpot = this;

      // Turn off buttons until we receive a response from the server
      CharacterScreen.self.canvasGroup.interactable = false;

      PanelManager.self.confirmScreen.hideEvent.RemoveAllListeners();
      PanelManager.self.confirmScreen.hideEvent.AddListener(() => {
         CharacterScreen.self.canvasGroup.interactable = true;
      });

      // Ask the player for confirmation. Reenable the canvas group if cancelled deletion.
      PanelManager.self.showConfirmationPanel("Are you sure you want to restore " + character.nameText.text + "?", () => sendRestoreUserRequest(character.userId), () => CharacterScreen.self.canvasGroup.interactable = true);
   }

   public void startNewCharacterButtonWasPressed () {
      lastInteractedSpot = this;

      // Remove any existing character creation stuff
      if (character != null && character.creationMode) {
         Destroy(character.gameObject);
      }

      StartCoroutine(CO_SetupCharacter());
   }

   private IEnumerator CO_SetupCharacter () {
      setButtonVisiblity(false);
      
      // Fade to loading overlay
      CameraFader.self.fadeIn(0.5f);
      CameraFader.self.setLoadingIndicatorVisibility(true);
      CharacterScreen.self.toggleCharacters(show: false);
      Util.fadeCanvasGroup(CharacterScreen.self.canvasGroup, false, FADE_TIME);

      yield return new WaitForSeconds(FADE_TIME);

      // Create a new character at this spot
      OfflineCharacter offlineChar = Instantiate(CharacterScreen.self.offlineCharacterPrefab, this.transform.position, Quaternion.identity);
      offlineChar.creationMode = true;
      offlineChar.spot = this;

      // Set up the initial values
      UserInfo userInfo = new UserInfo();
      userInfo.gender = Gender.Type.Female;
      userInfo.hairType = HairLayer.Type.Female_Hair_1;
      userInfo.hairPalettes = Item.parseItmPalette(new string[2] { PaletteDef.Hair.Red, PaletteDef.Hair.Black });
      userInfo.eyesType = EyesLayer.Type.Female_Eyes_1;
      userInfo.eyesPalettes = PaletteDef.Eyes.Green;
      userInfo.bodyType = BodyLayer.Type.Female_Body_1;

      Weapon weapon = new Weapon();
      weapon.itemTypeId = 0;
      Armor armor = new Armor();
      if (CharacterScreen.self.startingArmorData.Count < 1) {
         D.debug("Needs Investigation! Starting armor data is empty!");
      } else {
         armor.itemTypeId = CharacterScreen.self.startingArmorData[0].equipmentId;
         armor.paletteNames = Item.parseItmPalette(new string[2] { PaletteDef.Armor1.Brown, PaletteDef.Armor2.Blue });
      }

      Hat hat = new Hat();

      offlineChar.setDataAndLayers(userInfo, weapon, armor, hat, armor.paletteNames);

      _spotCameraSettings = new VirtualCameraSettings();
      _spotCameraSettings.position = spotCamera.position;
      _spotCameraSettings.ppuScale = MyCamera.getCharacterCreationPPUScale();

      this.assignCharacter(offlineChar);
      offlineChar.gameObject.SetActive(false);

      CharacterCreationPanel.self.setCharacterBeingCreated(offlineChar);

      // Remove loading overlay
      CameraFader.self.fadeOut(0.5f);
      yield return new WaitForSeconds(CameraFader.FADE_DURATION);
      CameraFader.self.setLoadingIndicatorVisibility(false);

      // Show character and creation panel, and zoom in
      CharacterCreationPanel.self.show();
      offlineChar.gameObject.SetActive(true);

      CharacterScreen.self.myCamera.setSettings(_spotCameraSettings).OnComplete(() => {
         CharacterCreationSpotFader.self.fadeColorOnPosition(offlineChar.transform.position);
      });
   }

   protected void sendDeleteUserRequest (int userId) {
      
      // Fade and show a loading indicator
      CameraFader.self.fadeIn(0.5f);
      CameraFader.self.setLoadingIndicatorVisibility(true);
      CharacterScreen.self.toggleCharacters(show: false);

      // Disable the canvas group
      Util.fadeCanvasGroup(CharacterScreen.self.canvasGroup, false, FADE_TIME);

      // Disable the buttons on the confirmation panel while we're doing stuff
      Util.fadeCanvasGroup(PanelManager.self.confirmScreen.canvasGroup, false, FADE_TIME);

      // Send off the request
      NetworkClient.Send(new DeleteUserMessage(userId));
   }

   protected void sendRestoreUserRequest (int userId) {

      // Fade and show a loading indicator
      CameraFader.self.fadeIn(0.5f);
      CameraFader.self.setLoadingIndicatorVisibility(true);
      CharacterScreen.self.toggleCharacters(show: false);

      // Disable the canvas group
      Util.fadeCanvasGroup(CharacterScreen.self.canvasGroup, false, FADE_TIME);

      // Disable the buttons on the confirmation panel while we're doing stuff
      Util.fadeCanvasGroup(PanelManager.self.confirmScreen.canvasGroup, false, FADE_TIME);

      // Send off the request
      NetworkClient.Send(new RestoreUserMessage(userId));
   }

   public void setButtonVisiblity (bool isVisible) {
      Util.fadeCanvasGroup(buttonsCanvasGroup, isVisible, FADE_TIME);
   }

   #region Private Variables

   // The camera settings to focus on this spot
   private VirtualCameraSettings _spotCameraSettings;

   #endregion
}
