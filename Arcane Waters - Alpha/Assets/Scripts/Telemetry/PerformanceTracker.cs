using Mirror;
using UnityEngine;

public class PerformanceTracker : MonoBehaviour {
   #region Public Variables
      
   #endregion

   private void Start () {
      _passedTime = 0;
      _passedFrames = 0;
   }

   private void Update () {
      // Collect telemetry for cloud server builds only
      if (!NetworkServer.active || !Util.isCloudBuild()) {
         return;
      }

      _passedTime += Time.unscaledDeltaTime;
      _passedFrames++;

      if (_passedTime >= INTERVAL) {
         float avgFps = 1f / (_passedTime / _passedFrames);
         _passedTime = 0;
         _passedFrames = 0;

         // Skip data with abnormal low fps (usually on game start) 
         if (avgFps < 10) {
            return;
         }

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.serverStatUpdateFps(System.Environment.MachineName, MyNetworkManager.getCurrentPort(), avgFps);
         });         
      }
   }
   
   #region Private Variables
   private const float INTERVAL = 10; // Interval in seconds to collect data before send to db
   private float _passedTime;
   private float _passedFrames;
   #endregion
}
