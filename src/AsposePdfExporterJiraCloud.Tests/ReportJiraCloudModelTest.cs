using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public class ReportJiraCloudModelTest
    {
        public Dictionary<string, string> ReadIssueFiles(string responsesFolder)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var infoFile in Directory.GetFiles(responsesFolder, "*.json"))
            {
                var props = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(infoFile));
                string responseDataFile = null == props.ResponseDataFile ? null : Path.Combine(responsesFolder, props.ResponseDataFile.ToString());

                result.Add(props.RequestUri.ToString(), File.ReadAllText(responseDataFile));
            }

            return result;
        }

        [Fact]
        public void Model_Test()
        {
            ReportJiraCloudModel model = new ReportJiraCloudModel("");
            // read some issues
            List<string> issueContent = ReadIssueFiles("responses").Where(x => x.Key.EndsWith("CCTES-1") || x.Key.EndsWith("DK-1"))
                .Select(x => x.Value).ToList();
            var result = model.issuesModel(JToken.FromObject(new
            {
                issues = issueContent.Select(JsonConvert.DeserializeObject).ToList()
            }) as JObject, null);
            var resultExpando = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(result));
            Assert.NotNull(resultExpando);
            Assert.NotEmpty(resultExpando.issues);
            var issues = resultExpando.issues as IEnumerable<dynamic>;
            var issue = issues.ElementAt(0);
            Assert.Equal("CCTES-1", issue.issueName.ToString());
            Assert.Equal("ccTest01", issue.projectName.ToString());
            Assert.Equal("https://mockjiracloud.com/browse/CCTES-1", issue.issueLink.ToString());
            var issueLinks = issue.issueLinks as IEnumerable<dynamic>;
            Assert.Equal("CCTES-5", issueLinks.ElementAt(0).linkedIssueName.ToString());
            Assert.Equal("CCTES-2", issueLinks.ElementAt(1).linkedIssueName.ToString());

            issue = issues.ElementAt(1);
            Assert.Equal("DK-1", issue.issueName.ToString());
            Assert.Equal("democc-kanban", issue.projectName.ToString());
            Assert.Equal("https://mockjiracloud.com/browse/DK-1", issue.issueLink.ToString());
            Assert.Equal("Epic", issue.issueType.ToString());
        }
    }
}
