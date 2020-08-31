//#define NUBIS
#if NUBIS
using System.Net;
using System.IO;
using static NubisLogger;

public class NubisResponse
{
   public static void Content (HttpListenerContext context, string message = "") {
      try {

         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 200;
            writer.WriteLine(message);
            writer.Flush();
         }
         context.Response.Close();
      } catch {
         i("Replying to client: FAILED");
      }
   }

   public static void OK (HttpListenerContext context, string message = "OK") {
      try {
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 200;
            writer.WriteLine($"<html><p style=\"color:green;\">{message}</p></html>");
            writer.Flush();
         }
         context.Response.Close();
      } catch {
         i("Replying to client: FAILED");
      }
   }

   public static void NotFound (HttpListenerContext context, string message = "Not Found") {
      try {
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 404;
            writer.WriteLine($"<html><p style=\"color:green;\">{message}</p></html>");
            writer.Flush();
         }
         context.Response.Close();
      } catch {
         i("Replying to client: FAILED");
      }
   }

   public static void InternalServerError (HttpListenerContext context, string message = "Internal Server Error") {
      try {
         using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
            context.Response.StatusCode = 500;
            writer.WriteLine($"<html><p style=\"color:green;\">{message}</p></html>");
            writer.Flush();
         }
         context.Response.Close();
      } catch {
         i("Replying to client: FAILED");
      }
   }

   public static void LOG(HttpListenerContext context) {
      context.Response.StatusCode = 200;
      using (StreamWriter writer = new StreamWriter(context.Response.OutputStream)) {
         foreach (string line in File.ReadAllLines(NubisConfiguration.LogFilePath()))
            writer.WriteLine(line);
      }
      context.Response.Close();
   }
}
#endif