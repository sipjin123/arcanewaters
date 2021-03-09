using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CharacterInfoPanel : Panel {
   #region Public Variables

   // The info column
   public CharacterInfoColumn infoColumn;

   #endregion

   public override void show () {
      if (_currentPlayer == null) {
         D.warning("Using default show method for character info panel without setting a player first. Either use show(NetEntity) or call setPlayer(NetEntity) before showing.");
         return;
      }

      base.show();
   }

   public void setPlayer (NetEntity player) {
      infoColumn.setPlayer(player);
      _currentPlayer = player;
   }

   public void show (NetEntity player) {
      if (player == null) {
         D.error("Trying to show info column for null player.");
         return;
      }

      setPlayer(player);
      PanelManager.self.linkIfNotShowing(Panel.Type.CharacterInfo);      
   }

   #region Private Variables

   // The player we're currently showing
   private NetEntity _currentPlayer;

   #endregion
}
