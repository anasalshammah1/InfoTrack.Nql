using System;
using System.Globalization;
using RestSharp;
using RestSharp.Extensions;
using RestSharp.Validation;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace ValueFirst
{
    public partial class ValueFirstRestClient
    {
        public virtual SendSmsMessageResult SendMessage(string to, string messageText)
        {
            //Require.Argument("recipient", recipient);
            //Require.Argument("body", body);

            var request = new RestRequest(Method.POST) { Resource = "tracking/notify?trackings=" };

            //if (senderId.HasValue()) request.AddParameter("SenderID", senderId);

            //header head = new header();
            //head.shippingNo = "60481548";
            //head.clientID = "9017690";
            //head.clientPassword = "1233123";
            ////List<details> myList = new List<details>();
            //details x = new details();
            //x.date = DateTime.Now;
            //x.activity = "Data Received, Record Created";
            //x.eventCode = 0;
            //x.stationCode = "Riyadh";
            ////myList.Add(x);
            //head.list.Add(x);

            //var json = JsonConvert.SerializeObject(head);
            //var jsonSerialiser = new JavaScriptSerializer();
            //var json = jsonSerialiser.Serialize(head);

            List<header> data = new List<header>() 
            { 
                new header() 
                { 
                    shippingNo = "60481548", 
                    clientID= "9017690",
                    clientPassword = "1233123"
                }
            };

            var json = JsonConvert.SerializeObject(new
            {
                operations = data
            });

            request.AddJsonBody(json);
            return Execute<SendSmsMessageResult>(request);
        }



    }

    public class header
    {
        public string shippingNo { get; set; }
        public string clientID { get; set; }
        public string clientPassword { get; set; }

        //public List<details> list = new List<details>();
    }

    public class details
    {
        public DateTime date { get; set; }
        public string activity { get; set; }
        public int eventCode { get; set; }
        public string stationCode { get; set; }
        public string comments { get; set; }
        public string message { get; set; }
    }
}