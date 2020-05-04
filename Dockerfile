FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/*.sln ./
COPY src/AsposePdfExporterJiraCloud/*.csproj ./AsposePdfExporterJiraCloud/
COPY src/AsposePdfExporterJiraCloud.Tests/*.csproj ./AsposePdfExporterJiraCloud.Tests/
COPY src/AsposePdfExporterJiraCloud.IntegrationTests/*.csproj ./AsposePdfExporterJiraCloud.IntegrationTests/

COPY src/modules/application-services/src/AppCommon/*.csproj ./modules/application-services/src/AppCommon/
COPY src/modules/application-services/src/AppMiddleware/*.csproj ./modules/application-services/src/AppMiddleware/
COPY src/modules/application-services/src/TemplateExporter/*.csproj ./modules/application-services/src/TemplateExporter/
COPY src/modules/application-services/src/PdfExporter/*.csproj ./modules/application-services/src/PdfExporter/
COPY src/modules/application-services/src/ConfigurationExpression/*.csproj ./modules/application-services/src/ConfigurationExpression/
COPY src/modules/application-services/src/ElasticsearchLogging/*.csproj ./modules/application-services/src/ElasticsearchLogging/
COPY src/modules/application-services/src/Atlassian.Connect/*.csproj ./modules/application-services/src/Atlassian.Connect/

COPY src/modules/application-services/src/Tests/PdfExporter.Tests/*.csproj ./modules/application-services/src/Tests/PdfExporter.Tests/
COPY src/modules/application-services/src/Tests/Atlassian.Connect.Tests/*.csproj ./modules/application-services/src/Tests/Atlassian.Connect.Tests/

RUN dotnet restore

# Copy everything else and build
COPY src/ ./

RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/sdk:3.1
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
     && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "acm.AsposePdfExporterJiraCloud.dll"]