using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI
{
    public class Notification
    {
        //public Notification(string MessTitle)
        //{
        //    //CodeID = code;
        //    Message = MessTitle;
        //    Message = GlobalVar.GV.GetLocalizationMessage(MessTitle);
        //}

        //private int codeid = 0;
        //public int CodeID
        //{
        //    get { return codeid; }
        //    set
        //    {
        //        codeid = value;
        //        //Message = GlobalVar.GV.GetLocalizationMessage("_1");
        //            //GlobalVar.GV.NotificationList[value].Message;
        //    }
        //}

        private string message = "";
        public string Message
        {
            get { return message; }
            set { message = value; }
        }
    }
}