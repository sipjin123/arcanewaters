using UnityEngine;

public class ChatPanelObserver : MonoBehaviour {
   #region Public Variables
      
   #endregion

   private void Update () {
      // Skip update tic if chat manager or input master are not initialized yet
      if (!ChatManager.self || !InputManager.self) {
         return;
      }

      // If chat if focused but UI shortcuts are active
      if (ChatManager.self.chatPanel.inputField.isFocusedCached && InputManager.self.inputMaster.UIShotcuts.enabled) {
         _tickCount++;

         if (_tickCount >= FOCUS_CHECK_TICKS) {
            D.warning("Chat input is blocked");
         
            // Send bug report
            BugReportManager.self.sendBugReport("[Auto bug] Chat input is blocked");
         
            // Force chat on focus logic
            ChatManager.self.onChatGainedFocus();
         }
      }
      else {
         _tickCount = 0;
      }
   }
   
   #region Private Variables
   // Counter of ticks with focus problem
   private int _tickCount;
   // Ticks count in row when we decide the problem is exist, and chat is broken
   private const int FOCUS_CHECK_TICKS = 5;
   #endregion
}
