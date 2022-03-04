using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.IO;

public class ReplayPanel : Panel
{
   #region Public Variables

   // Text we update to show encoding progress
   public Text encodeProgressText = null;

   // Text we use to display recorder status message
   public Text recorderStatusText = null;

   // Toggle for enabling/disabling recorder
   public Toggle recorderToggle = null;

   #endregion

   public override void Update () {
      base.Update();

      if (!NetworkClient.active) {
         return;
      }

      if (_encodeProgress != null) {
         if (_encodeProgress.completed && !_encodeProgress.error) {
            try {
               encodeProgressText.text = "Saved GIF to: " + _encodeProgress.path + "\n" + "Encoding took: " + Mathf.RoundToInt(Time.time - _encodeProgress.startTime) + "s";
               _encodeProgress = null;
            } catch (Exception ex) {
               encodeProgressText.text = "There was an error saving the GIF: " + ex.Message;
            }
         } else {
            encodeProgressText.text = _encodeProgress.message;
         }
      }

      recorderStatusText.text = GIFReplayManager.self.getUserFriendlyStateMessage();
   }

   public static string getNewGifPath () {
      return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) +
         "/" + DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss");
   }

   public void encodeButtonClick () {
      if (_encodeProgress != null && !_encodeProgress.completed) {
         return;
      }

      _encodeProgress = GIFReplayManager.self.encode(getNewGifPath());
   }

   public void recorderToggleValueChanged () {
      GIFReplayManager.self.setIsRecording(recorderToggle.isOn);
   }

   public override void show () {
      base.show();

      recorderToggle.SetIsOnWithoutNotify(GIFReplayManager.self.isRecording());
   }

   #region Private Variables

   // Current encoding progress we care about
   private GIFReplayManager.EncodeProgress _encodeProgress = null;

   #endregion
}
