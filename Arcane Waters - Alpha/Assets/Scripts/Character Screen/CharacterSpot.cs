using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   #endregion

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
      // Temporarily turn off the buttons
      StartCoroutine(CO_temporarilyDisableInput());

      // Update our currently selected user ID
      Global.currentlySelectedUserId = character.userId;

      // Now go ahead and call ClientScene.AddPlayer() along with our currently selected user ID
      ClientManager.sendAccountNameAndUserId();
   }

   public void deleteButtonWasPressed () {
      // Temporarily turn off the buttons
      StartCoroutine(CO_temporarilyDisableInput());

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => sendDeleteUserRequest(character.userId));

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to delete " + character.nameText.text + "?");
   }

   public void startNewCharacterButtonWasPressed () {
      // Remove any existing character creation stuff
      if (character != null && character.creationMode) {
         Destroy(character.gameObject);
      }

      // Create a new character at this spot
      OfflineCharacter offlineChar = Instantiate(CharacterScreen.self.offlineCharacterPrefab, this.transform.position, Quaternion.identity);
      offlineChar.creationMode = true;

      // Set up the initial values
      UserInfo userInfo = new UserInfo();
      userInfo.gender = Gender.Type.Female;
      userInfo.hairType = HairLayer.Type.Female_Hair_1;
      userInfo.hairColor1 = ColorType.Red;
      userInfo.hairColor2 = ColorType.Black;
      userInfo.eyesType = EyesLayer.Type.Female_Eyes_1;
      userInfo.eyesColor1 = ColorType.Green;
      userInfo.eyesColor2 = ColorType.Green;
      userInfo.bodyType = BodyLayer.Type.Female_Body_1;
      Weapon weapon = new Weapon();
      weapon.type = Weapon.Type.Pitchfork;
      Armor armor = new Armor();
      armor.type = Armor.Type.Sash;
      armor.color1 = ColorType.Brown;
      armor.color2 = ColorType.Black;
      offlineChar.setDataAndLayers(userInfo, weapon, armor, armor.color1, armor.color2);

      this.assignCharacter(offlineChar);
   }

   public void doneCreatingCharacterButtonWasPressed () {
      // Temporarily turn off the buttons
      StartCoroutine(CO_temporarilyDisableInput());

      // Send the creation request to the server
      NetworkClient.Send(new CreateUserMessage(Global.netId,
         character.getUserInfo(), character.armor.getType(), character.armor.getColor1(), character.armor.getColor2()));
   }

   protected void sendDeleteUserRequest (int userId) {
      // Disable the buttons on the confirmation panel while we're doing stuff
      PanelManager.self.confirmScreen.canvasGroup.interactable = false;

      // Send off the request
      NetworkClient.Send(new DeleteUserMessage(Global.netId, userId));
   }

   protected IEnumerator CO_temporarilyDisableInput () {
      CharacterScreen.self.canvasGroup.interactable = false;

      yield return new WaitForSeconds(1.25f);

      CharacterScreen.self.canvasGroup.interactable = true;
   }

   #region Private Variables

   #endregion
}
