using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter
{
    /// <summary>
    /// Transforms issues data received from JiraCloud to data model suitable for reports generation
    /// </summary>
    public class ReportJiraCloudModel : Report.YamlReportModel
    {
        public string EpicNameField { get; set; } = null;
        public string EpicLinkField { get; set; } = null;

        public ReportJiraCloudModel(string yamlTemplateContent) : base(yamlTemplateContent) { }

        /// <summary>
        /// Transforms Jira issues into report's model
        /// </summary>
        /// <param name="issues"></param>
        /// <returns></returns>
        public dynamic issuesModel(JObject issues, Dictionary<string, JArray> epicStories)
        {
            bool listIsNotEmpty(JToken j) => j != null && j is JArray && j.Count() > 0;
            
            string stringItem(JToken token) => token?.ToString()?.Trim();

            string getContentItemText(JToken obj)
            {
                var l = new List<string>() { stringItem(obj?.SelectToken("$.text")) };
                var content = obj?.SelectToken("$.content");
                if (content != null)
                    l.AddRange(content?.Select(j => getContentItemText(j)?.Trim()).ToArray());
                //Trace.WriteLine($"\n------------ {string.Join(",", l.Where(x => !string.IsNullOrEmpty(x)))}");
                return string.Join(",", l.Where(x => !string.IsNullOrEmpty(x)));
            }
            List <string> getContentItemTexts(JToken obj)
            {
                var texts = new List<string>{ stringItem(obj?.SelectToken("$.text")) };
                var url = stringItem(obj?.SelectToken("$.attrs.url"));
                if (!string.IsNullOrWhiteSpace(url))
                    texts.Add($"url: {url}");
                var attrs_text = stringItem(obj?.SelectToken("$.attrs.text"));
                if (!string.IsNullOrWhiteSpace(attrs_text))
                    texts.Add(attrs_text);
                var content = obj?.SelectToken("$.content");
                if (content != null)
                    texts.AddRange(content.SelectMany(getContentItemTexts));
                //l.Where(x => !string.IsNullOrEmpty(x)).ToList().ForEach(x => Trace.WriteLine($"{x}"));
                return texts.Where(x => !string.IsNullOrWhiteSpace(x))
                    .SelectMany(x => x.Split(new[] { '\n' }))
                    .Select(s => s.Trim(new[] { '\r' }))
                    .ToList();
            }
            JToken issue_name(JToken j) => j.SelectToken("$.key");
            string issueBrowseLink(JToken self, JToken name)
            {
                var uri = new Uri(self.Value<string>());
                return $"{uri.GetLeftPart(UriPartial.Authority)}/browse/{name}";
            }
            List<string> getComments(JToken obj) => getContentItemTexts(obj);

            string toShortDate(JToken obj)
            {
                DateTime? dt = (DateTime?)obj;
                if (obj == null || !dt.HasValue)
                    return null;
                return dt?.ToShortDateString();
            }
            dynamic property<T>(T o, Func<T, bool> ne) => new
            {
                Value = o,
                NotEmpty = ne(o),
                Empty = !ne(o),
            };
            bool isEpic(JToken obj) => !string.IsNullOrEmpty(EpicNameField) && obj.SelectToken($"$.fields.{EpicNameField}") != null;

            return new
            {
                issues = issues["issues"].Select(i => new
                {
                    issueName = issue_name(i),
                    projectName = i.SelectToken("$.fields.project.name"),
                    issueType = i.SelectToken("$.fields.issuetype.name"),
                    priority = i.SelectToken("$.fields.priority.name"),
                    summary = i.SelectToken("$.fields.summary"),
                    reporter = i.SelectToken("$.fields.reporter.displayName"),
                    assignee = i.SelectToken("$.fields.assignee.displayName"),
                    status = i.SelectToken("$.fields.status.name"),
                    detailsLines = property(getComments(i.SelectToken("$.fields.description")), x => x.Count > 0),

                    created = toShortDate(i.SelectToken("$.fields.created")),
                    updated = toShortDate(i.SelectToken("$.fields.updated")),
                    duedate = toShortDate(i.SelectToken("$.fields.duedate")),

                    resolutiondate = toShortDate(i.SelectToken("$.fields.resolutiondate")),
                    resolution = i.SelectToken("$.fields.resolution.name"),

                    epicLink = string.IsNullOrEmpty(EpicLinkField) ? null : i.SelectToken($"$.fields.{EpicLinkField}"),

                    estimate = i.SelectToken("$.fields.timetracking.originalEstimate"),
                    timespent = i.SelectToken("$.fields.timetracking.timeSpent"),


                    issueLabelsList = property(i.SelectToken("$.fields.labels").Select(l => l.ToString()).ToList(), x => x.Count > 0),
                    issueFixVersionsList = property(i.SelectToken("$.fields.fixVersions").Select(l => l.SelectToken("$.name")).ToList(), x => x.Count > 0),
                    issueComponentsList = property(i.SelectToken("$.fields.components").Select(l => l.SelectToken("$.name")).ToList(), x => x.Count > 0),

                    issueQrImage = QueryHelpers.AddQueryString("file://issue-link-qr", "link", issueBrowseLink(i.SelectToken("$.self"), issue_name(i))),
                    issueQrImageVisible = GenerateQRCode,
                    issueLink = issueBrowseLink(i.SelectToken("$.self"), issue_name(i)),

                    issueLinksNotEmpty = listIsNotEmpty(i.SelectToken("$.fields.issuelinks")),
                    issueLinks = i.SelectToken("$.fields.issuelinks").Select(s =>
                        s.SelectToken("$.inwardIssue") != null ? new
                        {
                            linkType = s.SelectToken("$.type.inward"),
                            linkedIssueName = s.SelectToken("$.inwardIssue.key"),
                            linkedIssueSummary = s.SelectToken("$.inwardIssue.fields.summary"),
                            linkedIssueStatus = s.SelectToken("$.inwardIssue.fields.status.name"),
                        } : new
                        {
                            linkType = s.SelectToken("$.type.outward"),
                            linkedIssueName = s.SelectToken("$.outwardIssue.key"),
                            linkedIssueSummary = s.SelectToken("$.outwardIssue.fields.summary"),
                            linkedIssueStatus = s.SelectToken("$.outwardIssue.fields.status.name"),
                        }),
                    subtasksName = isEpic(i) ? "Epic Stories" : "Subtasks",
                    subtasksNotEmpty = isEpic(i) ? listIsNotEmpty(epicStories[issue_name(i)?.ToString()]) : listIsNotEmpty(i.SelectToken("$.fields.subtasks")),
                    subtasks = isEpic(i) ? (dynamic)epicStories[issue_name(i)?.ToString()].Select(s => new
                    {
                        subtaskIssueName = s.SelectToken("$.key"),
                        subtaskSummary = s.SelectToken("$.fields.summary"),
                        subtaskStatus = s.SelectToken("$.fields.status.name"),
                    })
                    : i.SelectToken("$.fields.subtasks").Select(s => new
                    {
                        subtaskIssueName = s.SelectToken("$.key"),
                        subtaskSummary = s.SelectToken("$.fields.summary"),
                        subtaskStatus = s.SelectToken("$.fields.status.name"),
                    }),

                    commentsNotEmpty = listIsNotEmpty(i.SelectToken("$.fields.comment.comments")),
                    comments = i.SelectToken("$.fields.comment.comments").Select(c => new
                    {
                        commentAuthor = c.SelectToken("$.author.displayName"),
                        commentCreated = toShortDate(c.SelectToken("$.created")),
                        commentLines = property(getComments(c.SelectToken("$.body")), x => x.Count > 0),
                    }),

                    worklogsNotEmpty = listIsNotEmpty(i.SelectToken("$.fields.worklog.worklogs")),
                    worklogs = i.SelectToken("$.fields.worklog.worklogs").Select(w => new
                    {
                        worklogAuthor = w.SelectToken("$.author.displayName"),
                        worklogStarted = toShortDate(w.SelectToken("$.started")),
                        worklogTimeSpent = w.SelectToken("$.timeSpent"),
                        worklogCommentLines = property(getComments(w.SelectToken("$.comment")), x => x.Count > 0),
                    })
                })
            };
        }
    }
}
