using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;

public class VoyageGroupMemberCell : MonoBehaviour
{
   #region Public Variables

   // The character portrait
   public CharacterPortrait characterPortrait;

   // The user name
   public Text userNameText;

   #endregion

   public void Awake () {
      // Set the portrait pointer events
      characterPortrait.pointerEnterEvent.RemoveAllListeners();
      characterPortrait.pointerExitEvent.RemoveAllListeners();
      characterPortrait.pointerEnterEvent.AddListener(() => onPointerEnterPortrait());
      characterPortrait.pointerExitEvent.AddListener(() => onPointerExitPortrait());
   }

   public void setCellForGroupMember (UserObjects userObjects) {
      _userId = userObjects.userInfo.userId;

      // Set the user name
      userNameText.text = userObjects.userInfo.username;

      // Update the character portrait
      characterPortrait.setPortrait(userObjects);

      // Hide the user name
      userNameText.enabled = false;
   }

   public void enable () {
      characterPortrait.enable();
   }

   public void disable () {
      characterPortrait.disable();
   }

   public void onPointerEnterPortrait () {
      userNameText.enabled = false;
   }

   public void onPointerExitPortrait () {
      userNameText.enabled = true;
   }

   public int getUserId () {
      return _userId;
   }

   #region Private Variables

   // The id of the displayed user
   private int _userId = -1;

   #endregion
}
