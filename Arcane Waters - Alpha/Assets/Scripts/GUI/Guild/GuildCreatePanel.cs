using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GuildCreatePanel : Panel {
   #region Public Variables

   #endregion

   public override void Awake () {
      base.Awake(); 

      _inputField = GetComponentInChildren<InputField>();
   }

   public void createGuildConfirmed () {
      Global.player.rpc.Cmd_CreateGuild(_inputField.text, Faction.Type.Neutral);
   }

   #region Private Variables

   // Our Input Field
   protected InputField _inputField;

   #endregion
}
