using System;
using UnityEngine;

[CreateAssetMenu(menuName = "FMOD/My FMOD Callback Handler")]
public class MyFMODCallbackHandler : FMODUnity.PlatformCallbackHandler
{
   public override void PreInitialize (FMOD.Studio.System studioSystem, Action<FMOD.RESULT, string> reportResult) {
      if (Util.isBatch()) {
         FMOD.RESULT result;
         FMOD.System coreSystem;

         result = studioSystem.getCoreSystem(out coreSystem);
         reportResult(result, "studioSystem.getCoreSystem");

         coreSystem.setOutput(FMOD.OUTPUTTYPE.NOSOUND);
      } else {
         base.PreInitialize(studioSystem, reportResult);
      }
   }
}