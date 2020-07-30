using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using DG.Tweening;

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

   #endregion

   protected override void Awake () {
      base.Awake();

      _spotCameraSettings = new VirtualCameraSettings();
      _spotCameraSettings.position = spotCamera.position;
      _spotCameraSettings.ppuScale = MyCamera.CHARACTER_CREATION_PPU_SCALE;
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
      // Turn off buttons until we receive a response from the server
      CharacterScreen.self.canvasGroup.interactable = false;

      // Update our currently selected user ID
      Global.currentlySelectedUserId = character.userId;

      // Cache the user info
      Global.userObjects = new UserObjects {
         userInfo = character.getUserInfo(),
         weapon = character.getWeapon(),
         armor = character.getArmor(),
         hat = character.getHat()
      };

      // Now go ahead and call ClientScene.AddPlayer() along with our currently selected user ID
      ClientManager.sendAccountNameAndUserId();
   }

   public void deleteButtonWasPressed () {
      // Turn off buttons until we receive a response from the server
      CharacterScreen.self.canvasGroup.interactable = false;

      // Ask the player for confirmation. Reenable the canvas group if cancelled deletion.
      PanelManager.self.showConfirmationPanel("Are you sure you want to delete " + character.nameText.text + "?", () => sendDeleteUserRequest(character.userId), () => CharacterScreen.self.canvasGroup.interactable = true);
   }

   public void startNewCharacterButtonWasPressed () {
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
      userInfo.hairPalette1 = PaletteDef.Hair.Red;
      userInfo.hairPalette2 = PaletteDef.Hair.Black;
      userInfo.eyesType = EyesLayer.Type.Female_Eyes_1;
      userInfo.eyesPalette1 = PaletteDef.Eyes.Green;
      userInfo.eyesPalette2 = PaletteDef.Eyes.Green;
      userInfo.bodyType = BodyLayer.Type.Female_Body_1;
      Weapon weapon = new Weapon();
      weapon.itemTypeId = 0;
      Armor armor = new Armor();
      armor.itemTypeId = CharacterScreen.self.startingArmorData[0].equipmentId;
      armor.paletteName1 = PaletteDef.Armor1.Brown;
      armor.paletteName2 = PaletteDef.Armor2.Blue;
      Hat hat = new Hat();

      offlineChar.setDataAndLayers(userInfo, weapon, armor, hat, armor.paletteName1, armor.paletteName2);

      CharacterScreen.self.myCamera.setSettings(_spotCameraSettings).OnComplete(() => {
         SpotFader.self.closeTowardsOfflineChar(offlineChar.transform.position);
      });

      this.assignCharacter(offlineChar);
   }

   protected void sendDeleteUserRequest (int userId) {
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
