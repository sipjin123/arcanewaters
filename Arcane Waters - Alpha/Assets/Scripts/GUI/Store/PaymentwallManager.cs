using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Paymentwall;

public class PaymentwallManager : MonoBehaviour {
   #region Public Variables

   // The canvas we use for the Paymentwall panel
   public Canvas canvas;

   // Convenient self reference
   public static PaymentwallManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public void openWebsite (int accountId) {
      PWBase.SetApiType(PWBase.API_GOODS);
      PWBase.SetAppKey(_publicKey); // your Project Public key - available in your Paymentwall merchant area
      PWBase.SetSecretKey(_privateKey); // your Project Private key - available in your Paymentwall merchant area

      List<PWProduct> productList = new List<PWProduct>();
      PWProduct product = new PWProduct(
         _defaultProduct // id of the product in your system
      );
      productList.Add(product);

      Dictionary<string, string> dictionary = new Dictionary<string, string>() {
         { "testParameter", "testValue" },
         { "email", Global.lastAccountEmail },
         { "history[registration_date]", ((System.DateTimeOffset)Global.lastAccountCreationTime).ToUnixTimeSeconds() + "" }
      };

      PWWidget widget = new PWWidget(
         accountId + "", // id of the end-user who's making the payment
         _widgetCode, // widget code, e.g. p1; can be picked inside of your merchant account
         productList,
         dictionary
      );

      PWUnityWidget unity = new PWUnityWidget(widget);
      StartCoroutine(unity.callWidgetWebView(gameObject, canvas)); // call this function to display widget
   }

   #region Private Variables

   // The public Paymentall key
   private string _publicKey = "91c86be9edb34a53b70c9e9687c88a1e";

   // The private Paymentwall key
   private string _privateKey = "1d5aa3921b56c1a28abb55dca87deaae";

   // A default  product to select when we show the panel
   private string _defaultProduct = "gems_10";

   // The widget code we want to use, as defined in the Paymentwall website
   private string _widgetCode = "p1_1";

   #endregion
}
