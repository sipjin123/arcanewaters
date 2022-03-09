using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UTJ.FrameCapturer;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using UnityEngine.Rendering;

public class GIFReplayManager : ClientMonoBehaviour
{
   #region Public Variables

   // The key we use to store GIF recording setting
   public const string GIF_PRESET_SAVE_KEY = "gif-replay-settings-preset-index";

   // Singleton instance
   public static GIFReplayManager self;

   // Current encoding progress
   public EncodeProgress currentProgress;

   // Possible recorder settings presets, indexed by serialization ID
   public RecorderSettings[] possibleSettingsPresets = new RecorderSettings[] {
      new RecorderSettings { downscaleFactor = 1, fps = 0, length = 0, description = "Off (Recommended)" }, // Turned off
      new RecorderSettings { downscaleFactor = 1, fps = 10, length = 10, description = "Full Res, 10 FPS, 10s" },
      new RecorderSettings { downscaleFactor = 1, fps = 5, length = 20, description = "Full Res, 5 FPS, 20s" }
   };

   public struct RecorderSettings
   {
      // How much to downscale each dimension of screen
      public int downscaleFactor;

      // How many frames per second to record
      public int fps;

      // The length of replay to keep (seconds)
      public int length;

      // The user-friendly description of the preset
      public string description;
   }

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

      // Progress in range of 0 to 1
      public float progress;

      // Have we notified the user about the end of the encoding
      public bool sentChatMessage;
   }

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;

      // Initialize temp texture to whatever
      _tempRenderTexture = new RenderTexture(0, 0, 0);

      // Load preset
      int index = PlayerPrefs.GetInt(GIF_PRESET_SAVE_KEY, 0);
      setSettingsPresetIndex(index, false);
   }

   private void Start () {
      if (Util.isBatch()) {
         return;
      }

      StartCoroutine(CO_RecordRoutine());
   }

   private IEnumerator CO_RecordRoutine () {
      while (true) {
         yield return new WaitForEndOfFrame();

         if (currentProgress != null && (currentProgress.completed || currentProgress.error) && !currentProgress.sentChatMessage) {
            currentProgress.sentChatMessage = true;

            if (currentProgress.error) {
               ChatManager.self.addChat("GIF Error: " + currentProgress.message, ChatInfo.Type.System);
            } else {
               ChatManager.self.addChat("GIF Saved: " + currentProgress.path, ChatInfo.Type.System);
            }
         }

         // Check if screen resolution changed
         int targetWidth = Camera.main.pixelWidth / 1;// _resolutionDownscaleFactor;
         int targetHeight = Camera.main.pixelHeight / 1;// _resolutionDownscaleFactor;
         if (targetWidth != _outputWidth || targetHeight != _outputHeight) {
            clearRecordedFrames();
            _outputWidth = targetWidth;
            _outputHeight = targetHeight;
         }

         if (!_isRecording || isEncoding()) {
            continue;
         }

         if (Time.time - _lastRecordTime > _recordDelay) {
            _lastRecordTime = Time.time;

            if (_tempRenderTexture.width != Screen.width || _tempRenderTexture.height != _tempRenderTexture.height) {
               Destroy(_tempRenderTexture);
               _tempRenderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            }

            // Capture
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_tempRenderTexture);
            AsyncGPUReadback.Request(_tempRenderTexture, 0, _recordFormat, captureComplete);
         }
      }
   }

   private void captureComplete (AsyncGPUReadbackRequest request) {
      if (!_isRecording || isEncoding()) {
         return;
      }

      if (request.hasError) {
         D.error("Error recording frame");
      } else {
         // Keep frame count within max
         if (_currentFrames.Count >= _maxFrames) {
            Destroy(_currentFrames[0]);
            _currentFrames.RemoveAt(0);
         }

         Texture2D tex = new Texture2D(request.width, request.height, _recordFormat, false);
         tex.LoadRawTextureData(request.GetData<byte>());

         _currentFrames.Add(tex);
      }
   }

   private void encode () {
      currentProgress = new EncodeProgress {
         path = getNewGifReplayPath(),
         startTime = Time.time
      };

      if (_currentFrames.Count == 0) {
         currentProgress.completed = true;
         currentProgress.error = true;
         currentProgress.message = "There were 0 frames to encode";
         return;
      }

      ChatManager.self.addChat("Creating a GIF: " + Mathf.RoundToInt(_currentFrames.Count * _recordDelay) + "s recorded", ChatInfo.Type.System);

      currentProgress.message = "initializing...";

      try {
         // Take currently recorded frames for the encoder, wipe them for the recorder
         List<Texture2D> frames = _currentFrames;
         _currentFrames = new List<Texture2D>();

         ProGifTexturesToGIF encoder = ProGifTexturesToGIF.Create(currentProgress.path);
         encoder.m_Rotation = ImageRotator.Rotation.FlipY;
         encoder.Save(frames, _outputWidth, _outputHeight, _recordDelay, 0, 50,
            smooth_yieldPerFrame: true,
            destroyOriginTexture: true,
            onFileSaved: (i, path) => {
               try {
                  File.Move(path, currentProgress.path);
                  currentProgress.progress = 1f;
                  currentProgress.message = "Finished";
                  currentProgress.completed = true;
               } catch (Exception ex) {
                  currentProgress.message = ex.Message;
                  currentProgress.error = true;
                  D.warning("Error while encoding GIF: " + ex);
               }
            },
            onFileSaveProgress: (i, prog) => {
               currentProgress.completed = false;
               currentProgress.message = "Encoding...";
               currentProgress.progress = prog;
            });
      } catch (Exception ex) {
         currentProgress.message = ex.Message;
         currentProgress.error = true;
         D.warning("Error while encoding GIF: " + ex);
      }
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

   public void setSettingsPresetIndex (int index, bool saveToPrefs) {
      if (index < 0 || index >= possibleSettingsPresets.Length) {
         setSettingsPreset(possibleSettingsPresets[0]);
      } else {
         setSettingsPreset(possibleSettingsPresets[index]);
         if (saveToPrefs) {
            PlayerPrefs.SetInt(GIF_PRESET_SAVE_KEY, index);
         }
      }
   }

   public void setSettingsPreset (RecorderSettings settings) {
      _isRecording = settings.fps != 0 && settings.length != 0;

      _resolutionDownscaleFactor = settings.downscaleFactor == 0 ? 1 : settings.downscaleFactor;
      _maxFrames = settings.fps * settings.length;
      _recordDelay = settings.fps == 0 ? 0 : 1f / settings.fps;
   }

   private void clearRecordedFrames () {
      _currentFrames.Clear();
   }

   public void userRequestedGIF () {
      if (!_isRecording) {
         ChatManager.self.addChat("GIF replays are currently disabled", ChatInfo.Type.System);
      } else if (!isEncoding()) {
         encode();
      } else {
         ChatManager.self.addChat("Busy with another GIF right now...", ChatInfo.Type.System);
      }
   }

   public static string getNewGifReplayPath () {
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
          "Arcane-Replay-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")) + ".gif";
   }

   public bool isEncoding () {
      return currentProgress != null && !currentProgress.completed && !currentProgress.error;
   }

   public int getMemoryEstimationMB () {
      if (!_isRecording) {
         return 0;
      }

      return Mathf.RoundToInt(3 * ((float) _outputWidth * _outputHeight * _maxFrames) / 1024f / 1024f);
   }

   #region Private Variables

   // Texture format of the texture we use to record
   private TextureFormat _recordFormat = TextureFormat.RGB24;

   // The frames we have so far recorded
   private List<Texture2D> _currentFrames = new List<Texture2D>();

   // Temp textures we use for capturing
   private RenderTexture _tempRenderTexture;

   // -------------------------------
   // Properties of the replay system

   // Resolution at which we record
   private int _outputWidth = 1;
   private int _outputHeight = 1;

   // Last time we recorded a frame
   private float _lastRecordTime = 0f;

   // Downscale factor of the resolution
   private int _resolutionDownscaleFactor = 1;

   // Are we recording right now
   private bool _isRecording = false;

   // What's the delay between recording frames (inverse FPS)
   private float _recordDelay = 0.2f;

   // Maximum number of frames we can record
   private float _maxFrames = 300;

   #endregion
}