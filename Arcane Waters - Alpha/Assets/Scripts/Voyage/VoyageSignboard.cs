using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class VoyageSignboard : Signboard
{
   #region Public Variables

   #endregion

   protected override void onClick () {
      PanelManager.self.linkIfNotShowing(Panel.Type.NoticeBoard);
      PanelManager.self.get<NoticeBoardPanel>(Panel.Type.NoticeBoard).refreshPanel(NoticeBoardPanel.Mode.BiomeActivity);
   }

   #region Private Variables

   #endregion
}
