using System.Net;
using Xunit;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public class Misc_Tests
    {
        [Fact]
        public void ControllerException_Test()
        {
            ControllerException ex = new ControllerException("test exception");
            Assert.Equal(HttpStatusCode.InternalServerError, ex.Code);
            Assert.Equal("test exception", ex.Message);
        }
    }
}
