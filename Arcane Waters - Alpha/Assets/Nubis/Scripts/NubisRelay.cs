﻿//#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NubisRelay
{

   private static string getFunctionNameFromUri (Uri uri) {
      if (uri == null) return string.Empty;
      if (uri.Segments == null || uri.Segments.Length < 3) return string.Empty;
      string funcName = uri.Segments[2].Replace("/", "");
      return System.Net.WebUtility.UrlDecode(funcName);
   }

   private static Dictionary<string, string> getArgumentsFromUri (Uri uri) {
      if (uri == null) return new Dictionary<string, string>();
      if (string.IsNullOrEmpty(uri.Query)) return new Dictionary<string, string>();
      string[] keyValuePairs = uri.Query.Substring(1).Split(new[] { '&' });
      Dictionary<string, string> arguments = new Dictionary<string, string>();
      foreach (string keyValuePair in keyValuePairs) {
         if (string.IsNullOrEmpty(keyValuePair)) continue;
         string[] keyValue = keyValuePair.Split(new[] { '=' });
         if (keyValue == null || keyValue.Length < 2) continue;
         string key = keyValue[0];
         string value = keyValue[1];
         key = System.Net.WebUtility.UrlDecode(key);
         value = System.Net.WebUtility.UrlDecode(value);
         arguments.Add(key, value);
      }
      return arguments;
   }

   private static string callImpl (string function, params string[] args) {
      string argString = (args != null && args.Length > 0) ? string.Join(",", args) : string.Empty;
      string typeName = "DB_Main";
      NubisLogger.i($"Invoking '{typeName}.{function}({argString})' ...");
      try {
         System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
         if (assembly == null) {
            NubisLogger.i($"Invoking '{typeName}.{function}({argString})' : FAILED - Couldn't locate assembly.");
            return string.Empty;
         }
         Type db_main_type = assembly.GetType(typeName);
         if (db_main_type == null) {
            NubisLogger.i($"Invoking '{typeName}.{function}({argString})' : FAILED - Couldn't locate type.");
            return string.Empty;
         }
         System.Reflection.MethodInfo methodInfo = db_main_type.GetMethod(function);
         if (methodInfo == null) {
            NubisLogger.i($"Invoking '{typeName}.{function}({argString})' : FAILED - Couldn't locate method.");
            return string.Empty;
         }
         object result = methodInfo.Invoke(null, args);
         if (result == null) {
            NubisLogger.i($"Invoking '{typeName}.{function}({argString})' : FAILED - The call to the method returned null.");
            return string.Empty;
         }
         NubisLogger.i($"Invoking '{typeName}.{function}({argString})' : OK");
         return result.ToString();
      } catch (Exception ex) {
         NubisLogger.i($"Invoking '{typeName}.{function}({argString})' : FAILED - There was an exception.");
         NubisLogger.e(ex);
      }
      return string.Empty;
   }

   public static string call (string url) {
      NubisLogger.i($"New url received. url: '{url}");
      Uri uri = new Uri(url);
      string function = getFunctionNameFromUri(uri);
      Dictionary<string, string> arguments = getArgumentsFromUri(uri);
      string[] argsValues = new string[arguments.Values.Count];
      arguments.Values.CopyTo(argsValues, 0);
      string result = callImpl(function, argsValues);
      return result;
   }

}
#endif

