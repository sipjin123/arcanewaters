using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CharacterInfoPanel : Panel {
   #region Public Variables

   // The character info section
   public CharacterInfoSection characterInfo;

   // The equipment stats section
   public EquipmentStatsGrid equipmentStats;

   // The job xp bars
   public XPBar farmerXPBar;
   public XPBar traderXPBar;
   public XPBar crafterXPBar;
   public XPBar minerXPBar;
   public XPBar explorerXPBar;
   public XPBar sailorXPBar;

   #endregion

   public void refreshPanel (int userId) {
      Global.player.rpc.Cmd_RequestUserInfoForCharacterInfoPanelFromServer(userId);
   }

   public void receiveUserObjectsFromServer (UserObjects userObjects, Jobs jobXP) {
      characterInfo.setUserObjects(userObjects);
      equipmentStats.refreshStats(userObjects);

      farmerXPBar.setProgress(jobXP.farmerXP);
      traderXPBar.setProgress(jobXP.traderXP);
      crafterXPBar.setProgress(jobXP.crafterXP);
      minerXPBar.setProgress(jobXP.minerXP);
      explorerXPBar.setProgress(jobXP.explorerXP);
      sailorXPBar.setProgress(jobXP.sailorXP);

      // SFX
      SoundEffectManager.self.playGuiMenuOpenSfx();
   }

   #region Private Variables

   #endregion
}
