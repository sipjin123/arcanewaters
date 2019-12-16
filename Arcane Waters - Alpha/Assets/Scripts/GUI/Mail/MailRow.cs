using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;

public class MailRow : MonoBehaviour
{
   #region Public Variables

   // The icon displayed when the mail has not yet been read
   public GameObject newMailIcon;

   // The name of the sender
   public TextMeshProUGUI senderName;

   // The date of reception
   public TextMeshProUGUI receptionDateText;

   // The mail subject
   public TextMeshProUGUI subjectText;

   // The row sprite to use when the mail has been read
   public Sprite mailReadRowSprite;

   // The row image
   public Image rowImage;

   #endregion

   public void setRowForMail (MailInfo mail) {
      _mailId = mail.mailId;
      senderName.SetText(mail.senderUserName);
      subjectText.SetText(mail.mailSubject);

      // Set the new mail icon
      if (mail.isRead) {
         newMailIcon.SetActive(false);
         rowImage.sprite = mailReadRowSprite;
      } else {
         newMailIcon.SetActive(true);
      }

      // Get the reception date in local time
      DateTime localReceptionDate = DateTime.FromBinary(mail.receptionDate).ToLocalTime();

      // Set the reception date text, depending on how much time has passed
      if (localReceptionDate.Year == DateTime.Now.Year) {
         if (localReceptionDate.Month == DateTime.Now.Month &&
            localReceptionDate.Day == DateTime.Now.Day) {
            // Same day: 05:50 AM
            receptionDateText.SetText(localReceptionDate.ToString("hh:mm tt"));
         } else {
            // Same year: Nov 5
            receptionDateText.SetText(localReceptionDate.ToString("MMM dd"));
         }
      } else {
         // Before the current year: 11/26/2000
         receptionDateText.SetText(localReceptionDate.ToString("MM/dd/yyyy"));
      }
   }

   public void onRowButtonPress () {
      MailPanel.self.displayMail(_mailId);
   }

   #region Private Variables

   // The ID of the mail being displayed
   private int _mailId;

   #endregion
}
