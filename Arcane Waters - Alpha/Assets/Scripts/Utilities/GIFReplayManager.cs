using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UTJ.FrameCapturer;
using System;
using System.Threading.Tasks;

public class GIFReplayManager : ClientMonoBehaviour
{
   #region Public Variables

   // Singleton instance
   public static GIFReplayManager self;

   // Class we use to cache info about the encoding progress
   public class EncodeProgress
   {
      // The current user-friendly message of the progress
      public string message;

      // Has the encoding completed
      public bool completed;

      // Is there an error with the encoding
      public bool error;

      // Path we will save the texture into
      public string path;

      // The time the encoding was started at
      public float startTime;
   }

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;

      // Initialize temp texture to whatever
      _tempCaptureTexture = new Texture2D(0, 0);
      _tempRenderTexture = new RenderTexture(0, 0, 0);
   }

   private void Update () {
      if (!_isRecording) {
         return;
      }

      // Don't record if replay panel is showing
      if (PanelManager.self.get<ReplayPanel>(Panel.Type.ReplayPanel).isShowing()) {
         return;
      }

      if (Time.time - _lastRecordTime > _recordDelay) {
         _lastRecordTime = Time.time;

         // Keep frame count within max
         if (_currentFrames.Count >= _maxFrames) {
            _currentFrames.RemoveAt(0);
         }

         _currentFrames.Add(capture());
      }
   }

   public EncodeProgress encode (string path) {
      EncodeProgress result = new EncodeProgress {
         message = "There were 0 frames to encode",
         path = path,
         startTime = Time.time
      };

      if (_currentFrames.Count == 0) {
         result.completed = true;
         result.error = false;
         return result;
      }

      result.message = "initializing...";

      Task.Run(() => {
         GifEncoder encoder = new GifEncoder();

         try {
            fcAPI.fcGifConfig config = fcAPI.fcGifConfig.default_value;
            config.width = _outputWidth;
            config.height = _outputHeight;

            encoder.Initialize(config, path);

            result.message = "Starting...";

            for (int i = 0; i < _currentFrames.Count; i++) {
               encoder.AddVideoFrame(_currentFrames[i], fcAPI.fcGetPixelFormat(_recordFormat), _recordDelay * i);
               result.completed = false;
               result.message = "Encoded: " + i + " out of " + _currentFrames.Count + " frames";
            }

            encoder.Release();
            result.message = "Finished";
            result.completed = true;
         } catch (Exception ex) {
            result.message = ex.Message;
            result.error = true;
            D.warning("Error while encoding GIF: " + ex);
         } finally {
            if (encoder != null) {
               encoder.Release();
            }
         }
      });

      return result;
   }

   private byte[] capture () {
      RenderTexture prevActive = RenderTexture.active;

      // [TODO] Update camera size to record correct region of screen
      _recordCamera.transform.position = Camera.main.transform.position;
      _recordCamera.orthographicSize = -Camera.main.orthographicSize;

      // Check if we need to update textures
      if (_outputWidth != _tempCaptureTexture.width || _outputHeight != _tempCaptureTexture.height) {
         Destroy(_tempCaptureTexture);
         Destroy(_tempRenderTexture);

         _tempCaptureTexture = new Texture2D(_outputWidth, _outputHeight, _recordFormat, false);
         _tempRenderTexture = new RenderTexture(_outputWidth, _outputHeight, 0);
         _tempRenderTexture.wrapMode = TextureWrapMode.Repeat;
         _tempRenderTexture.Create();
      }

      _recordCamera.targetTexture = _tempRenderTexture;
      RenderTexture.active = _tempRenderTexture;

      // Render image
      _recordCamera.Render();
      _tempCaptureTexture.ReadPixels(new Rect(0, 0, _outputWidth, _outputHeight), 0, 0);

      // Clean up
      RenderTexture.active = prevActive;

      return _tempCaptureTexture.GetRawTextureData();
   }

   public bool isRecording () {
      return _isRecording;
   }

   public void setIsRecording (bool value) {
      if (value != _isRecording) {
         _isRecording = value;
         clearRecordedFrames();
      }
   }

   public string getUserFriendlyStateMessage () {
      int count = 0;
      for (int i = 0; i < _currentFrames.Count; i++) {
         count += _currentFrames[i].Length;
      }

      return $"Recorder is currently { (_isRecording ? "ON" : "OFF") }{(_isRecording && PanelManager.self.get<ReplayPanel>(Panel.Type.ReplayPanel).isShowing() ? " (Paused while this panel is showing)" : "")}\n" +
         $"{ _currentFrames.Count } number of frames are recorded ({Mathf.RoundToInt(_currentFrames.Count * _recordDelay)} seconds) ({count} bytes)";
   }

   private void clearRecordedFrames () {
      _currentFrames.Clear();
   }

   #region Private Variables

   // Texture format of the texture we use to record
   private TextureFormat _recordFormat = TextureFormat.RGB24;

   [Tooltip("Camera we use to record a frame")]
   [SerializeField] private Camera _recordCamera = null;

   // The frames we have so far recorded
   private List<byte[]> _currentFrames = new List<byte[]>();

   // Temp textures we use for capturing
   private Texture2D _tempCaptureTexture;
   private RenderTexture _tempRenderTexture;

   // -------------------------------
   // Properties of the replay system

   // Last time we recorded a frame
   private float _lastRecordTime = 0f;

   // Resolution of the output image
   private int _outputWidth = 320;
   private int _outputHeight = 240;

   // Are we recording right now
   private bool _isRecording = false;

   // What's the delay between recording frames (inverse FPS)
   private float _recordDelay = 0.2f;

   // Maximum number of frames we can record
   private float _maxFrames = 300;

   #endregion
}

// This code was used to manually try to quanitse/dither colors with an encoder that doesn't do that
//public EncodeProgress encode () {
//   if (_currentFrames.Count == 0) {
//      return new EncodeProgress {
//         completed = true,
//         error = true,
//         message = "There were 0 frames to encode"
//      };
//   }

//   SimpleGif.Gif gif = new SimpleGif.Gif(_currentFrames.Select(t =>
//   new SimpleGif.Data.GifFrame { Texture = createGifTextureFrom(t), Delay = _recordDelay }).ToList());

//   EncodeProgress result = new EncodeProgress { message = "initializing..." };
//   gif.EncodeParallel(p => {
//      if (p.Exception != null) {
//         result.completed = true;
//         result.error = true;
//         result.message = "Encountered an error: " + p.Exception.Message;
//      } else if (!p.Completed) {
//         result.completed = false;
//         result.message = "Encoded: " + p.Progress + " out of " + p.FrameCount + " frames";
//      } else {
//         result.completed = true;
//         result.result = p.Bytes;
//      }
//   });

//   return result;
//}

//private void capture (Texture2D into) {
//   RenderTexture prevActive = RenderTexture.active;

//   // [TODO] Update camera size to record correct region of screen
//   // _recordCamera.orthographicSize = whatever
//   _recordCamera.transform.position = Camera.main.transform.position;
//   _recordCamera.orthographicSize = -Camera.main.orthographicSize;

//   // Create render texture
//   RenderTexture renTex = new RenderTexture(into.width, into.height, 0);
//   renTex.wrapMode = TextureWrapMode.Repeat;
//   renTex.Create();
//   _recordCamera.targetTexture = renTex;
//   RenderTexture.active = renTex;

//   // Render image
//   _recordCamera.Render();
//   into.ReadPixels(new Rect(0, 0, renTex.width, renTex.height), 0, 0);

//   // Clean up
//   RenderTexture.active = prevActive;
//   Destroy(renTex);
//}

//private SimpleGif.Data.Texture2D createGifTextureFrom (Texture2D tex) {
//   // Makes a recorded texture have less than 256 colors, dithers it, etc.,
//   // for it to be ready to be used in a GIF

//   Color32[] colors = tex.GetPixels32();
//   int width = tex.width;
//   int height = tex.height;

//   for (int i = 0; i < width; i++) {
//      for (int j = 0; j < height; j++) {
//         Color32 before = colors[j * width + i];

//         // Quantise our current pixel
//         Color32 quantised = quantise(before);
//         colors[j * width + i] = quantised;

//         // Transfer quantise error to next pixels
//         (int r, int g, int b) error = (before.r - quantised.r, before.g - quantised.g, before.b - quantised.b);

//         if (j < height - 1) {
//            colors[(j + 1) * width + i] = addError(colors[(j + 1) * width + i], error, 7);
//         }
//         if (j < height - 1 && i < width - 1) {
//            colors[(j + 1) * width + i + 1] = addError(colors[(j + 1) * width + i + 1], error, 1);
//         }
//         if (i < width - 1) {
//            colors[j * width + i + 1] = addError(colors[j * width + i + 1], error, 5);
//         }
//         if (j > 0 && i < width - 1) {
//            colors[(j - 1) * width + i + 1] = addError(colors[(j - 1) * width + i + 1], error, 3);
//         }
//      }
//   }

//   SimpleGif.Data.Texture2D resultTex = new SimpleGif.Data.Texture2D(width, height);
//   SimpleGif.Data.Color32[] resultCols = new SimpleGif.Data.Color32[colors.Length];
//   for (int i = 0; i < resultCols.Length; i++) {
//      resultCols[i] = new SimpleGif.Data.Color32(colors[i].r, colors[i].g, colors[i].b, colors[i].a);
//   }
//   resultTex.SetPixels32(resultCols);

//   return resultTex;
//}

//private Color32 addError (Color32 col, (int r, int g, int b) toAdd, int mult) {
//   return new Color32(
//      (byte) Mathf.Clamp(col.r + toAdd.r * mult / 16, 0, 255),
//      (byte) Mathf.Clamp(col.g + toAdd.g * mult / 16, 0, 255),
//      (byte) Mathf.Clamp(col.b + toAdd.b * mult / 16, 0, 255),
//      255);
//}

//private Color32 quantise (Color32 color) {
//   return new Color32(
//      (byte) (color.r / 51 * 51),
//      (byte) (color.g / 51 * 51),
//      (byte) (color.b / 51 * 51),
//      255);
//}

//private SimpleGif.Data.Texture2D createTextureFromCapturedFrame (Texture2D source) {
//   SimpleGif.Data.Texture2D texture = new SimpleGif.Data.Texture2D(source.width, source.height);


//   Color32[] pixels = source.GetPixels32();
//   SimpleGif.Data.Color32[] newPixels = new SimpleGif.Data.Color32[pixels.Length];

//   for (int i = 0; i < pixels.Length; i++) {
//      // Make there are less than 256 possible colors
//      SimpleGif.Data.Color32 newCol = new SimpleGif.Data.Color32(
//         (byte) ((pixels[i].r / 42) * 42),
//         (byte) ((pixels[i].g / 42) * 42),
//         (byte) ((pixels[i].b / 42) * 42),
//         255);
//      newPixels[i] = newCol;
//   }

//   texture.SetPixels32(newPixels);
//   texture.Apply();

//   return texture;
//}