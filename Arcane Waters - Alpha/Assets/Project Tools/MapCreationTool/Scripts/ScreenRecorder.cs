using System;
using UnityEngine;
using Assets.GifAssets.PowerGif;
using System.Collections;
using System.Collections.Generic;

namespace MapCreationTool
{
   public class ScreenRecorder : MonoBehaviour
   {
      const int COLOR_DEPTH = 16;

      private static ScreenRecorder instance;

      [SerializeField]
      private int frameCount = 10;
      [SerializeField]
      private float frameDelay = 0.1f;

      private Camera cam;

      private void Awake () {
         instance = this;

         cam = GetComponentInChildren<Camera>();
      }

      private void Start () {
         updateRecordingSize();
      }

      private void OnEnable () {
         Tools.AnythingChanged += updateRecordingSize;
      }

      private void OnDisable () {
         Tools.AnythingChanged -= updateRecordingSize;
      }

      private void updateRecordingSize () {
         cam.orthographicSize = Tools.boardSize.x / 2;
      }

      public static void recordGif (Action<byte[]> callback) {
         if (Tools.boardSize.x > 128) {
            UI.messagePanel.displayWarning("Recording GIFS of large maps is not supported");
         } else {
            instance.StartCoroutine(instance.recordGifRoutine(callback));
         }
      }

      private IEnumerator recordGifRoutine (Action<byte[]> callback) {
         UI.loadingPanel.display($"Recording frames 0/{ frameCount }");

         List<GifFrame> frames = new List<GifFrame>(frameCount);

         for (int i = 0; i < frameCount; i++) {
            frames.Add(new GifFrame(recordScreen(), frameDelay));
            UI.loadingPanel.display($"Recording frames { i + 1 }/{ frameCount }");
            yield return new WaitForSeconds(frameDelay);
         }

         UI.loadingPanel.display($"Encoding frames { 0 }/{ frameCount }");
         Gif gif = new Gif(frames);
         SimpleGif.Data.EncodeProgress progress = new SimpleGif.Data.EncodeProgress();
         gif.EncodeParallel(prog => progress = prog, conversionPalette);

         while (!progress.Completed && progress.Exception == null) {
            UI.loadingPanel.display($"Encoding frames { progress.Progress }/{ frameCount }");
            yield return null;
         }

         if (progress.Exception != null) {
            UI.messagePanel.displayError("Error encoding image data. Exception:\n" + progress.Exception);
            yield break;
         } else {
            callback(progress.Bytes);
         }

         foreach (GifFrame frame in frames) {
            Destroy(frame.Texture);
         }

         UI.loadingPanel.close();
      }

      public static byte[] recordPng () {
         Texture2D tex = instance.recordScreen();
         byte[] bytes = tex.EncodeToPNG();
         Destroy(tex);
         return bytes;
      }

      public static Texture2D recordTexture () {
         return instance.recordScreen();
      }

      private Texture2D recordScreen () {
         RenderTexture renTex = new RenderTexture(recordingPixelSize.x, recordingPixelSize.y, COLOR_DEPTH);
         cam.targetTexture = renTex;
         RenderTexture.active = renTex;
         cam.Render();
         Texture2D result = new Texture2D(renTex.width, renTex.height);
         result.ReadPixels(new Rect(0, 0, renTex.width, renTex.height), 0, 0);
         result.Apply();
         RenderTexture.active = null;
         Destroy(renTex);
         return result;
      }

      private SimpleGif.Enums.MasterPalette conversionPalette
      {
         get
         {
            switch (Tools.biome) {
               case Biome.Type.Desert:
                  return SimpleGif.Enums.MasterPalette.Levels884;
               case Biome.Type.Forest:
                  return SimpleGif.Enums.MasterPalette.Levels884;
               case Biome.Type.Lava:
                  return SimpleGif.Enums.MasterPalette.Levels685;
               case Biome.Type.Mushroom:
                  return SimpleGif.Enums.MasterPalette.Levels666;
               case Biome.Type.Pine:
                  return SimpleGif.Enums.MasterPalette.Levels666;
               case Biome.Type.Snow:
                  return SimpleGif.Enums.MasterPalette.Levels676;
               default:
                  return SimpleGif.Enums.MasterPalette.Levels666;
            }
         }
      }

      private Vector2Int recordingPixelSize
      {
         get
         {
            return new Vector2Int(Tools.boardSize.x * Overlord.TILE_PIXEL_WIDTH, Tools.boardSize.y * Overlord.TILE_PIXEL_WIDTH);
         }
      }
   }
}
