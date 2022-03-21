using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
      new RecorderSettings { downscaleFactor = 2, fps = 10, length = 10, description = "1/2 Res, 10 FPS, 10s" },
      new RecorderSettings { downscaleFactor = 4, fps = 10, length = 10, description = "1/4 Res, 10 FPS, 10s" },
      new RecorderSettings { downscaleFactor = 1, fps = 10, length = 10, description = "Full Res, 10 FPS, 10s" },
      new RecorderSettings { downscaleFactor = 1, fps = 10, length = 20, description = "Full Res, 10 FPS, 20s" },
      new RecorderSettings { downscaleFactor = 1, fps = 10, length = 30, description = "Full Res, 10 FPS, 30s" },
      new RecorderSettings { downscaleFactor = 1, fps = 30, length = 10, description = "Full Res, 30 FPS, 10s" },
      new RecorderSettings { downscaleFactor = 8, fps = 10, length = 60, description = "1/8 Res, 10 FPS, 60s" }

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
         // Fetch the captured data
         int sourceW = request.width;
         int sourceH = request.height;
         byte[] sourceData = request.GetData<byte>().ToArray();

         // Create new frame
         int f = (_resolutionDownscaleFactor <= 0 ? 1 : _resolutionDownscaleFactor);
         int targetW = sourceW / f;
         int targetH = sourceH / f;

         CustomFrame frame = new CustomFrame {
            Width = targetW,
            Height = targetH
         };

         if (f == 1) {
            frame.Data = sourceData;
         } else {
            frame.Data = new byte[frame.Width * frame.Height * 3];
            int n = 0;

            for (int j = 0; j < frame.Height; j++) {
               for (int i = 0; i < frame.Width; i++) {
                  frame.Data[n++] = sourceData[(i * f + j * f * sourceW) * 3];
                  frame.Data[n++] = sourceData[(i * f + j * f * sourceW) * 3 + 1];
                  frame.Data[n++] = sourceData[(i * f + j * f * sourceW) * 3 + 2];
               }
            }
         }

         // Make sure all frames are of same size
         if (_currentFrames.Count > 0 && (_currentFrames[0].Width != frame.Width || _currentFrames[0].Height != frame.Height)) {
            _currentFrames.Clear();
         }

         // Keep frame count within max
         if (_currentFrames.Count >= _maxFrames) {
            _currentFrames.RemoveAt(0);
         }

         _currentFrames.Add(frame);
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
         List<CustomFrame> frames = _currentFrames;
         _currentFrames = new List<CustomFrame>();

         StartEncode(frames, (int) (1 / _recordDelay), 0, 50);
      } catch (Exception ex) {
         currentProgress.message = ex.Message;
         currentProgress.error = true;
         D.warning("Error while encoding GIF: " + ex);
      }
   }

   // We are taking the code from the custom package we are using
   // And heavily messing with it
   // For performance and better results
   private void StartEncode (List<CustomFrame> frames, int fps, int loop, int quality) {
      quality = (int) Mathf.Clamp(quality, 1, 100);

      int frameCountFinal = frames.Count;

      // Multithreaded encoding starts here.
      _encodingNumberOfThreads = SystemInfo.processorCount - 1;
      if (_encodingNumberOfThreads <= 0) {
         _encodingNumberOfThreads = 1;
      }

      // Start of by creating a palette from all frames
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         CustomQuant quantizer = new CustomQuant(frames, quality, currentProgress);
         byte[] sharedPalette = quantizer.Process();

#if UNITY_EDITOR
         Debug.Log("GIF: Number of threads: " + _encodingNumberOfThreads);
#endif

         _encodingFramesFinished = 0;
         _encodingJobsFinished = 0;
         _encodingTotalFrames = frames.Count;

         // Split frames
         _encodingWorkers = new List<ProGifWorker>();
         List<Frame>[] framesArray = new List<Frame>[_encodingNumberOfThreads];

         int framesOnEachThread = Mathf.FloorToInt((float) frameCountFinal / (float) _encodingNumberOfThreads);
         int leftOverFrames = frameCountFinal % _encodingNumberOfThreads;

         int startIndex = 0;
         for (int threadIndex = 0; threadIndex < _encodingNumberOfThreads; threadIndex++) {
            int leftOverFrameAvg = (leftOverFrames > 0 ? 1 : 0);

            framesArray[threadIndex] = new List<Frame>();
            for (int i = startIndex; i < startIndex + framesOnEachThread + leftOverFrameAvg; i++) {
               framesArray[threadIndex].Add(frames[i]);
            }
            //The leftover frames are added to the first thread.
            startIndex += framesOnEachThread + leftOverFrameAvg;
            if (leftOverFrames > 0) leftOverFrames--;
         }

         for (int i = 0; i < _encodingNumberOfThreads; i++) {
            // Setup a worker thread for GIF encoding and save file -----------------
            CustomGIFEncoder encoder = new CustomGIFEncoder(loop, quality, i, EncoderFinished);

            encoder.setQuantizer(quantizer, sharedPalette);

            // Check if apply the Override Frame Delay value
            float timePerFrame = 1f / fps;
            encoder.SetDelay(Mathf.RoundToInt(timePerFrame * 1000f));

            ProGifWorker worker = new ProGifWorker(System.Threading.ThreadPriority.BelowNormal) {
               m_Encoder = encoder,
               m_Frames = framesArray[i],
               m_OnFileSaveProgress = FileSaveProgress
            };

            _encodingWorkers.Add(worker);

            // Make sure only the first encoder writes the beginning
            worker.m_Encoder.m_IsFirstFrame = i == 0;

            // Make sure only the last encoder appends the trail.
            worker.m_Encoder.m_IsLastEncoder = i == _encodingNumberOfThreads - 1;

            worker.Start();
         }
      });
   }

   private void EncoderFinished (int encoderIndex) {
#if UNITY_EDITOR
      //UnityThreadHelper.UnityDispatcher.Dispatch(() => {
      //   Debug.Log("GIF: Thread finished - " + encoderIndex + " out of " + _encodingNumberOfThreads);
      //});
#endif
      try {
         _encodingJobsFinished++;
         if (_encodingJobsFinished == _encodingNumberOfThreads) {
#if UNITY_EDITOR
            //UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            //   Debug.Log("GIF: All threads finished.");
            //});
#endif
            FileStream fileStream = new FileStream(currentProgress.path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            for (int i = 0; i < _encodingWorkers.Count; i++) {
               MemoryStream stream = _encodingWorkers[i].m_Encoder.GetMemoryStream();
               stream.Position = 0;
               stream.WriteTo(fileStream);
               stream.Close();
            }
            fileStream.Close();
            _encodingWorkers.Clear();

            currentProgress.progress = 1f;
            currentProgress.message = "Finished";
            currentProgress.completed = true;
         }
      } catch (Exception ex) {
         currentProgress.message = ex.Message;
         currentProgress.error = true;
         D.warning("Error while encoding GIF: " + ex);
      }
   }

   private void FileSaveProgress (int id) {
      _encodingFramesFinished++;

#if UNITY_EDITOR
      //UnityThreadHelper.UnityDispatcher.Dispatch(() => {
      //   Debug.Log("GIF: Frame finished - " + _encodingFramesFinished + " " + id);
      //});
#endif

      currentProgress.message = "Encoding...";
      currentProgress.progress = 0.6f + ((float) _encodingFramesFinished / _encodingTotalFrames) * 0.4f;
   }

   public bool isRecording () {
      return _isRecording;
   }

   public void setIsRecording (bool value) {
      if (value != _isRecording) {
         _isRecording = value;
         _currentFrames.Clear();
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

   #region Private Variables

   // Texture format of the texture we use to record
   private TextureFormat _recordFormat = TextureFormat.RGB24;

   // The frames we have so far recorded
   private List<CustomFrame> _currentFrames = new List<CustomFrame>();

   // Temp textures we use for capturing
   private RenderTexture _tempRenderTexture;

   // -------------------------------
   // Properties of the replay system

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

   // Status of current encoding process
   private int _encodingTotalFrames = 0;
   private int _encodingFramesFinished = 0;
   private int _encodingJobsFinished = 0;
   private int _encodingNumberOfThreads = 0;
   private List<ProGifWorker> _encodingWorkers;

   #endregion
}