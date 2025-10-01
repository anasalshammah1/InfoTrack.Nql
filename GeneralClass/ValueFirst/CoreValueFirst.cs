using System;
using System.Reflection;
using RestSharp;
using RestSharp.Deserializers;

namespace ValueFirst
{
    /// <summary>
    /// Otsdc rest client
    /// </summary>
    public abstract class ValueFirstClient
    {
        public string BaseUrl { get; private set; }
        //protected string AppSid { get; set; }
        protected RestClient Client;

        protected ValueFirstClient( string baseUrl)
        {
            BaseUrl = baseUrl;
            //AppSid = appSid;

            var assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(assembly.FullName);
            var version = assemblyName.Version;

            Client = new RestClient
            {
                UserAgent = "ValueFirst-csharp/" + version + " (.NET " + Environment.Version + ")",
                BaseUrl = new Uri(BaseUrl),
                Timeout = 60000
            };
            Client.AddHandler("text/html", new JsonDeserializer());
            //Client.AddDefaultParameter("AppSid", AppSid);
        }

        //public virtual T Execute<T>(IRestRequest request) where T : new()
        //public virtual void Execute(IRestRequest request) //where T : new()
        public virtual T Execute<T>(IRestRequest request) where T : new()
        {
            request.OnBeforeDeserialization = resp =>
            {
                if (((int)resp.StatusCode) >= 400)
                {
                    //RestSharp doesn't like data[]
                    resp.Content = resp.Content.Replace(",\"data\":[]", string.Empty);
                }
            };

            var response = Client.Execute<BaseResult<T>>(request);
            //if (response.Data != null && !response.Data.Success)
            //{
            //    var otsdcException = new RestException(response.Data.ErrorCode, response.Data.Message);
            //    throw otsdcException;
            //}
            //if (response.ErrorException != null)
            //{
            //    const string message = "Errors retrieving response.  Check inner details for more info.";
            //    var otsdcException = new ApplicationException(message, response.ErrorException);
            //    throw otsdcException;
            //}
            return response.Data != null ? response.Data.Data : default(T);
        }
    }

    public partial class ValueFirstRestClient : ValueFirstClient
    {
        /// <summary>
        /// Initializes a new client
        /// </summary>
        /// <param name="appSid">String that uniquely identifies your app, you will find your AppSid in "Dev Tools" after you login to OTS Digital Platform</param>
        public ValueFirstRestClient()
            : base("http://220.189.213.188:2017/")
        {
        }
    }
}
