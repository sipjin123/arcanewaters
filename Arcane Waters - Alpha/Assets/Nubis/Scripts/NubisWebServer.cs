//#define NUBIS
#if NUBIS
using System.Net;
using System.Threading.Tasks;
using System;
using static NubisLogger;

public class NubisWebServer
{
   public NubisWebServer (NubisManager nubisManager, int port) {
      this.nubisManager = nubisManager;
      this.port = port;
   }

   public void init () {
      if (!create()) return;
      if (!start()) return;
      loop();
   }

   public void stop () {
      httpServer?.Abort();
   }

   private void loop () {
      Task.Run(() => {
         try {
            i($"{NubisStatics.APP_NAME} waiting for requests.");
            do {
               HttpListenerContext context = httpServer.GetContext(); // blocks until a request is received.            
               _ = nubisManager.ProcessRequestAsync(context);
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
         i($"{NubisStatics.APP_NAME} web server stopped.");
      });
   }

   private bool start () {
      // try to start web server.
      try {
         httpServer.Prefixes.Add($"http://*:{port.ToString()}/");
         i($"Trying to start {NubisStatics.APP_NAME}...");
         httpServer.Start();
         return true;
      } catch (InvalidOperationException ex) {
         // httpServer not started or Stopped or closed. or no URI to respond to. so remember to add prefixes.
         e(ex);
         i($"{NubisStatics.APP_NAME} failed to start.");
      } catch (HttpListenerException ex) {
         // native call failed.
         i(ex.Message + " | errorCode: " + ex.ErrorCode + " | nativeErrorCode: " + ex.NativeErrorCode);
         if (ex.ErrorCode == 5) {
            i($"Please, Run {NubisStatics.APP_NAME} as an Administrator.");
         }
         i($"{NubisStatics.APP_NAME} failed to start.");
      }
      return false;
   }

   private bool create () {
      try {
         // create web server.
         httpServer = new HttpListener();
         return true;
      } catch (Exception ex) {
         e(ex);
      }
      return false;
   }

   #region Private Variables

   // Reference to the internal HttpServer instance.
   private HttpListener httpServer;

   // Reference to NubisManager.
   private NubisManager nubisManager;

   // Port on which to listen for incoming messages.
   private int port;

   #endregion
}
#endif