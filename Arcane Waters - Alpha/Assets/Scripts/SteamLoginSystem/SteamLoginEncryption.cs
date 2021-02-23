using System.Text;
using System.Security.Cryptography;
using System;

namespace SteamLoginSystem
{
   public class SteamLoginEncryption { 
      // The encryption key code (Do not Change or else the previous user entries will not work)
      public const string ENCRYPTION_KEY = "arcw-enz8-lxmq19";

      // The alpha numeric to be used for randomizing
      public const string ALPHA_NUMERIC = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
       
      // Length of the password generated for steam account
      public const int PASSWORD_LENGTH = 8;

      public static string Encrypt (string input) {
         byte[] inputArray = UTF8Encoding.UTF8.GetBytes(input);
         TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
         tripleDES.Key = UTF8Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
         tripleDES.Mode = CipherMode.ECB;
         tripleDES.Padding = PaddingMode.PKCS7;
         ICryptoTransform cTransform = tripleDES.CreateEncryptor();
         byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
         tripleDES.Clear();
         return Convert.ToBase64String(resultArray, 0, resultArray.Length);
      }

      public static string Decrypt (string input) {
         byte[] inputArray = Convert.FromBase64String(input);
         TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
         tripleDES.Key = UTF8Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
         tripleDES.Mode = CipherMode.ECB;
         tripleDES.Padding = PaddingMode.PKCS7;
         ICryptoTransform cTransform = tripleDES.CreateDecryptor();
         byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
         tripleDES.Clear();
         return UTF8Encoding.UTF8.GetString(resultArray);
      }
   }
}