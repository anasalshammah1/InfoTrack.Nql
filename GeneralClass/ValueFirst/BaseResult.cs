using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ValueFirst
{
    public class BaseResult<T>
    {
        /// <summary>
        /// Result of calling API,True if success,otherwise False
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Error Message if any
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Error code if any
        /// </summary>
        public string ErrorCode { get; set; }
        /// <summary>
        /// The type of object to create and populate with the returned data
        /// </summary>
        public T Data { get; set; }
    }

    public class SendSmsMessageResult 
    {
        /// <summary>
        /// Number of unit in a message
        /// </summary>
        public string result { get; set; }
    }
}