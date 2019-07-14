using UnityEngine;
using UnityEditor;
using System;

public class SoundImportSettings : AssetPostprocessor {
   public void OnPreprocessAudio () {
      AudioImporter ai = assetImporter as AudioImporter;
      ai.loadInBackground = true;
   }
}