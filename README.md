# Aspose.PDF Exporter

Aspose.PDF Exporter is a free  application for [Atlassian Marketplace](https://marketplace.atlassian.com/) that allow users to export their issues to pdf. Powered by [Aspose.PDF Cloud](https://products.aspose.cloud/pdf/family) and [Aspose.BarCode Cloud](https://products.aspose.cloud/barcode/family).

## Project structure

Aspose.PDF Exporter app has following structure:
* *Controllers* - controllers for user registration/deregistration (**CallbackController**) and  PDF exporter controller (**JiraExporterController**).

* *DbContext* - DB context class for postgresql database.

* *Job* - misc workers (background jobs).

* *Migrations* - [database migrations](docs/development.MD#migrations).

* *Model* - lasses for database and Report model.

* *Pages* - Razor pages (exporter UI widgets).

* *Report* - classes to perform PDF report export.

* *template* - template mustache files to build [atlassian connect descriptor](https://developer.atlassian.com/cloud/jira/platform/app-descriptor/) and report template file.

## Setup
You must prepare `appsettings.Development.json` file with required options to run
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "<PG_CONNECTION_STRING>"
  },
  "Settings": {
    "AppName": "aspose-pdf-exporter-app",
    "BaseAppUrl": "<BASE_APP_URL>"
  },
  "Elasticsearch": {
    "Uris": [ "<ELASTICSEARCH_URI>" ],
    "apiId": "<ELASTICSEARCH_APIID>",
    "apiKey": "<ELASTICSEARCH_APPKEY>"
  },
  "AsposeCloud": {
    "ApiKey": "<ASPOSE_CLOUD_APIKEY>",
    "AppSid": "<ASPOSE_CLOUD_APPSID>"
  }
}
```
Section "Elasticsearch" is optional, you may enable it if you want to get application reports in your ELK instance.

`<PG_CONNECTION_STRING>` - PostgreSQL connection string. Usually looks like `User ID=postgres;Password=password;Host=localhost;Port=5432;Database=jira_pdf_exporter_db;Pooling=true;`.

`<BASE_APP_URL>` - Application base URL. All URLs that application generates will respect this setting.  
`<ASPOSE_CLOUD_APIKEY>` and `<ASPOSE_CLOUD_APPSID>` - used to authenticate with [Aspose.Cloud](https://www.aspose.cloud) products. You may register and receive those keys using [dashboard](https://dashboard.aspose.cloud).


## Project description
Please read how to [integrate applications with Jira Cloud](https://developer.atlassian.com/cloud/jira/platform/integrating-with-jira-cloud/) first in order to get overview how to integrate web applications to Jira Cloud.  

Aspose.PDF Exporter defines *atlassian-connect.json* endpoint to provide application descriptor for Jira Cloud. Check [Aspose.PDF Exporter development](docs/development.MD) for future details.  




