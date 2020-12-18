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

   // Shows a UI that notifies the user their character is being deleted
   public GameObject deleteIndicator;

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   void Update () {
      charCreateButton.transform.parent.gameObject.SetActive(character == null);
      charDeleteButton.transform.parent.gameObject.SetActive(character != null && !character.creationMode);
      charSelectButton.transform.parent.gameObject.SetActive(character != null && !CharacterScreen.self.isCreatingCharacter());
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

      // Show loading screen until player warps to map
      PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.MapCreation, PostSpotFader.self, PostSpotFader.self);
   }

   public void deleteButtonWasPressed () {
      lastInteractedSpot = this;

      // Turn off buttons until we receive a response from the server
      CharacterScreen.self.canvasGroup.interactable = false;

      // Deactive "Confirm" Button until player types in the word "Delete" into the inputfield
      PanelManager.self.confirmScreen.confirmButton.interactable = false;

      // Activate inputfield
      PanelManager.self.confirmScreen.goInputField.SetActive(true);

      // Wait for input
      PanelManager.self.confirmScreen.deleteInputField.onValueChanged.AddListener((deleteText) => {
         if (deleteText.ToUpper() == "DELETE") {
            PanelManager.self.confirmScreen.confirmButton.interactable = true;
         }
      });

      // Ask the player for confirmation. Reenable the canvas group if cancelled deletion.
      PanelManager.self.showConfirmationPanel("Are you sure you want to delete " + character.nameText.text + "?", () => sendDeleteUserRequest(character.userId), () => CharacterScreen.self.canvasGroup.interactable = true);
   }

   public void startNewCharacterButtonWasPressed () {
      lastInteractedSpot = this;

      // Remove any existing character creation stuff
      if (character != null && character.creationMode) {
         Destroy(character.gameObject);
      }

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

      CharacterScreen.self.myCamera.setSettings(_spotCameraSettings).OnComplete(() => {
         CharacterCreationSpotFader.self.fadeColorOnPosition(offlineChar.transform.position);
      });

      this.assignCharacter(offlineChar);
   }

   protected void sendDeleteUserRequest (int userId) {
      deleteIndicator.SetActive(true);
      character.gameObject.SetActive(false);

      // Disable the canvas group
      Util.disableCanvasGroup(CharacterScreen.self.canvasGroup);

      // Disable the buttons on the confirmation panel while we're doing stuff
      PanelManager.self.confirmScreen.canvasGroup.interactable = false;

      // Send off the request
      NetworkClient.Send(new DeleteUserMessage(Global.netId, userId));
   }

   #region Private Variables

   // The camera settings to focus on this spot
   private VirtualCameraSettings _spotCameraSettings;

   #endregion
}
