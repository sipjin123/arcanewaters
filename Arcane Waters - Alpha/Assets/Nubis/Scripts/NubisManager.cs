//#define NUBIS
#if NUBIS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System;
using static NubisLogger;

public class NubisManager : MonoBehaviour
{
   #region Public Variables

   #endregion

   /// <summary>
   /// Unity Start Message.
   /// </summary>
   public void Start () {
      Application.targetFrameRate = 1;
      i("NubisManager starting");
      _ = StartNubis();
   }
   
   public void OnDestroy () {
      Stop();
   }

   public bool Initialize () {
      bool IsSupported = CheckSystemRequirements();
      if (!IsSupported) return false;
      return true;
   }

   private static void Content (HttpListenerContext context, string message = "") {
      try {
         i("Replying to client...");
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 200;
            writer.WriteLine(message);
            writer.Flush();
         }
         context.Response.Close();
         i("Replying to client: DONE");
      } catch {
         i("Replying to client: FAILED");
      }
   }

   private static void OK (HttpListenerContext context, string message = "OK") {
      try {
         i("Replying to client...");
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 200;
            writer.WriteLine($"<html><p style=\"color:green;\">{message}</p></html>");
            writer.Flush();
         }
         context.Response.Close();
         i("Replying to client: DONE");
      } catch {
         i("Replying to client: FAILED");
      }
   }

   private static void NotFound (HttpListenerContext context, string message = "Not Found") {
      try {
         i("Replying to client...");
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 404;
            writer.WriteLine($"<html><p style=\"color:green;\">{message}</p></html>");
            writer.Flush();
         }
         context.Response.Close();
         i("Replying to client: DONE");
      } catch {
         i("Replying to client: FAILED");
      }
   }

   private static void InternalServerError (HttpListenerContext context, string message = "Internal Server Error") {
      try {
         i("Replying to client...");
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 500;
            writer.WriteLine($"<html><p style=\"color:green;\">{message}</p></html>");
            writer.Flush();
         }
         context.Response.Close();
         i("Replying to client: DONE");
      } catch {
         i("Replying to client: FAILED");
      }
   }
   private async Task ProcessRequestAsync (HttpListenerContext context) {
      await Task.Run(() => {
         try {
            if (context.Request.Url.Segments == null || context.Request.Url.Segments.Length < 1) {
            OK(context,"");
            return;
            }
            string endpoint = context.Request.Url.Segments[1].Replace("/", "");
            switch (endpoint) {
               case NubisEndpoints.RPC:
                  var result = NubisRelay.call(context.Request.Url.AbsoluteUri);
                  Content(context, result);
                  break;
               case NubisEndpoints.TERMINATE:
                  string msg = "Stop request received.";
                  i(msg);
                  OK(context, msg);
                  Stop();
                  break;
               case NubisEndpoints.LOG:
                  i("Log requsted.");
                  using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
                     foreach (string line in File.ReadAllLines(NubisConfiguration.LogFilePath()))
                        writer.WriteLine(line);
                  }
                  context.Response.StatusCode = 200;
                  context.Response.Close();
                  break;
               default:
                  NotFound(context);
                  break;
            }
         } catch (Exception ex) {
            e(ex);
            InternalServerError(context);
         }
         
      });
   }
   
   public async Task StartNubis () {
      if (Status == NubisStatus.Starting || Status == NubisStatus.Running) return;
      Status = NubisStatus.Starting;
      DefaultFolders.Initialize();
      i($"{NubisStatics.AppName} starting...");
      configuration = NubisConfiguration.LoadSafe();
      await CreateWebServerAsync(configuration.WebServerPort);
   }
   
   private bool CheckSystemRequirements () {
      i("Checking System Requirements...");
      bool httpListenerSupported = HttpListener.IsSupported;
      if (!httpListenerSupported) {
         i($"{NubisStatics.AppName} is not supported on this platform...");
         return false;
      }
      i($"{NubisStatics.AppName} can run on this platform!");
      i("Checking System Requirements: DONE.");
      return true;
   }
   
   private void WebServerLoop () {
      try {
         i($"{NubisStatics.AppName} waiting for requests.");
         do {
            HttpListenerContext context = httpServer.GetContext(); // blocks until a request is received.
            i($"Request received! -from: {context.Request.RemoteEndPoint.ToString()} -url: {context.Request.Url}");
            _ = ProcessRequestAsync(context);
         }
         while (httpServer.IsListening);
      } catch (InvalidOperationException ex) {
         // httpServer not started or Stopped or closed. or no URI to respond to. thus remember to add prefixes.
         e(ex);
      } catch (HttpListenerException ex) {
         // native call failed.
         i(ex.Message + " | errorCode: " + ex.ErrorCode + " | nativeErrorCode: " + ex.NativeErrorCode);
         if (ex.NativeErrorCode == 995) {
            i($"The error {ex.NativeErrorCode} can be safely ignored in most cases.");
         };
      }
      i($"{NubisStatics.AppName} web server stopped.");
   }
   
   private bool StartWebServer (HttpListener httpServer, int port) {
      // try to start web server.
      try {
         httpServer.Prefixes.Add($"http://*:{port.ToString()}/");
         i($"Trying to start {NubisStatics.AppName}...");
         httpServer.Start();
      } catch (InvalidOperationException ex) {
         // httpServer not started or Stopped or closed. or no URI to respond to. so remember to add prefixes.
         e(ex);
         i($"{NubisStatics.AppName} failed to start.");
         return false;
      } catch (HttpListenerException ex) {
         // native call failed.
         i(ex.Message + " | errorCode: " + ex.ErrorCode + " | nativeErrorCode: " + ex.NativeErrorCode);
         if (ex.ErrorCode == 5) {
            i($"Please, Run {NubisStatics.AppName} as an Administrator.");
         }
         i($"{NubisStatics.AppName} failed to start.");
         return false;
      }
      return true;
   }
   
   private async Task CreateWebServerAsync (int port) {
      try {
         // create web server.
         httpServer = new HttpListener();
         bool success = StartWebServer(httpServer, port);
         if (!success) return;
         await Task.Run(WebServerLoop);
      } catch (Exception ex) {
         e(ex);
      }
   }
   
   public void Stop () {
      if (Status == NubisStatus.Stopping || Status == NubisStatus.Idle) return;
      Status = NubisStatus.Stopping;
      i($"{NubisStatics.AppName} stopping...");
      httpServer?.Abort();
   }
 

#region Private Variables

   // Reference to the current instance of the configuration.
   private NubisConfiguration configuration;
   // Reference to the webServer the butler is listening with.
   private HttpListener httpServer;
   //// List of the processes that have been run.
   //private List<ProcessInfo> spawnedProcesses = new List<ProcessInfo>();
   // Reference to the current status of the Butler.
   public NubisStatus Status { get; private set; } = NubisStatus.Idle;

   #endregion

}
#endif