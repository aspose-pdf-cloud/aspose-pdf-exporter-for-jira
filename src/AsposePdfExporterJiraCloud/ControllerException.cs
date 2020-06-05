using System;
using System.Net;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter
{
    public class ControllerException : Exception
    {
        public HttpStatusCode Code { get; }
        public byte[] CustomData { get; set; }
        public ControllerException(string message, HttpStatusCode? code = null, Exception innerException = null, byte[] customData = null) : base(message, innerException) 
        {
            Code = code ?? StatusCode(innerException);
            CustomData = customData;
        }
        public static HttpStatusCode StatusCode(Exception ex)
        {
            HttpStatusCode code = HttpStatusCode.InternalServerError;
            if (null == ex)
                return code;
            if (ex is Aspose.Pdf.Cloud.Sdk.Client.ApiException pex) code = (HttpStatusCode) pex.ErrorCode;
            //else if (ex is Aspose.BarCode.Cloud.Sdk.ApiException bex) code = (HttpStatusCode)bex.ErrorCode;
            return code;
        }
    }
}
