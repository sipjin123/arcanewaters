using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class CharacterScreen : MonoBehaviour {
   #region Public Variables

   // Our virtual camera
   public CinemachineVirtualCamera virtualCam;

   // For some reason the static lookup of this object reference is null in test builds
   public MessageManager messageManager;

   // The prefab we use for creating offline characters
   public OfflineCharacter offlineCharacterPrefab;

   // The Canvas Group
   [HideInInspector]
   public CanvasGroup canvasGroup;

   // Self
   public static CharacterScreen self;

   #endregion

   void Awake () {
      self = this;

      // Look up components
      this.canvasGroup = GetComponent<CanvasGroup>();

      // Look up our Character Spots
      foreach (CharacterSpot spot in GetComponentsInChildren<CharacterSpot>()) {
         _spots[spot.number] = spot;
      }
   }

   public bool isShowing () {
      return Vector2.Distance(Camera.main.transform.position, virtualCam.transform.position) < .5f;
   }

   public bool isCreatingCharacter () {
      foreach (OfflineCharacter offlineChar in GetComponentsInChildren<OfflineCharacter>()) {
         if (offlineChar.creationMode) {
            return true;
         }
      }

      return false;
   }

   public void initializeScreen (UserInfo[] userArray, Armor[] armorArray, Weapon[] weaponArray, int[] armorColors1, int[] armorColors2, MaterialType[] materialArray) {
      // Store the data we receive for later reference
      _userArray = userArray;
      _armorArray = armorArray;
      _weaponArray = weaponArray;

      // Clear out any existing offline characters
      foreach (OfflineCharacter offlineChar in GetComponentsInChildren<OfflineCharacter>(true)) {
         Destroy(offlineChar.gameObject);
      }

      for (int i = 0; i < 3; i++) {
         // If they don't have a character in that spot, move on
         if (i > userArray.Length - 1 || userArray[i] == null) {
            continue;
         }

         int charSpotNumber = userArray[i].charSpot;

         // Create the offline character object
         if (_spots.ContainsKey(charSpotNumber)) {
            CharacterSpot spot = _spots[charSpotNumber];
            OfflineCharacter offlineChar = Instantiate(offlineCharacterPrefab, spot.transform.position, Quaternion.identity);
            offlineChar.setDataAndLayers(userArray[i], weaponArray[i], armorArray[i], (ColorType) armorColors1[i], (ColorType) armorColors2[i], materialArray[i]);
            spot.assignCharacter(offlineChar);
         }
      }

      // Sometimes we just want to auto-select a character when debugging
      if (Util.isAutoStarting()) {
         _spots[1].selectButtonWasPressed();
      } else if (Global.isFastLogin && Global.fastLoginCharacterSpotIndex != -1) {
         _spots[Global.fastLoginCharacterSpotIndex].selectButtonWasPressed();
      }
   }

   #region Private Variables

   // The array of UserInfo we received
   protected UserInfo[] _userArray;

   // The array of Armor we received
   protected Armor[] _armorArray;

   // The array of Weapons we received
   protected Weapon[] _weaponArray;

   // Our various character spots, indexed by their assigned number
   protected Dictionary<int, CharacterSpot> _spots = new Dictionary<int, CharacterSpot>();

   #endregion
}
