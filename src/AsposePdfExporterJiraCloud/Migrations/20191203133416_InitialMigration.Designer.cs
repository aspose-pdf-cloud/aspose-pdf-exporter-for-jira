﻿// <auto-generated />
using System;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20191203133416_InitialMigration")]
    partial class InitialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:HiLoSequenceName", "hilo_seq")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo)
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("Relational:Sequence:.hilo_seq", "'hilo_seq', '', '1', '10', '', '', 'Int64', 'False'");

            modelBuilder.Entity("AsposePdfExporter.Model.AccessLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasComment("Unique ID")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo);

                    b.Property<DateTime?>("Accessed")
                        .HasColumnName("accessed")
                        .HasColumnType("timestamp")
                        .HasComment("access time");

                    b.Property<int>("ClientId")
                        .HasColumnName("client_reg_id")
                        .HasColumnType("integer")
                        .HasComment("client registration id");

                    b.Property<string>("Url")
                        .HasColumnName("url")
                        .HasColumnType("text")
                        .HasComment("accessed url");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.ToTable("access_log");

                    b.HasComment("client access log");
                });

            modelBuilder.Entity("AsposePdfExporter.Model.ClientRegistration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasComment("Unique ID")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo);

                    b.Property<string>("BaseUrl")
                        .HasColumnName("base_url")
                        .HasColumnType("text")
                        .HasComment("URL prefix for this Atlassian product instance. All of its REST endpoints begin with this `baseUrl`");

                    b.Property<string>("ClientKey")
                        .HasColumnName("client_key")
                        .HasColumnType("text")
                        .HasComment("Identifying key for the Atlassian product instance that the app was installed into. This will never change for a given instance, and is unique across all Atlassian product tenants");

                    b.Property<int>("ClientStateId")
                        .HasColumnName("client_state_id")
                        .HasColumnType("integer")
                        .HasComment("client state id");

                    b.Property<DateTime?>("Created")
                        .HasColumnName("created")
                        .HasColumnType("timestamp")
                        .HasComment("Row created");

                    b.Property<DateTime?>("Deleted")
                        .HasColumnName("deleted")
                        .HasColumnType("timestamp")
                        .HasComment("Row deleted");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text")
                        .HasComment("The host product description - this is customisable by an instance administrator");

                    b.Property<string>("Key")
                        .HasColumnName("key")
                        .HasColumnType("text")
                        .HasComment("App key that was installed into the Atlassian Product, as it appears in your app's descriptor");

                    b.Property<DateTime?>("Modified")
                        .HasColumnName("modified")
                        .HasColumnType("timestamp")
                        .HasComment("Row modified");

                    b.Property<string>("PluginsVersion")
                        .HasColumnName("plugins_version")
                        .HasColumnType("text")
                        .HasComment("This is a semver compliant version of Atlassian Connect which is running on the host server");

                    b.Property<string>("ProductType")
                        .HasColumnName("product_type")
                        .HasColumnType("text")
                        .HasComment("Identifies the category of Atlassian product, e.g. jira or confluence");

                    b.Property<string>("PublicKey")
                        .HasColumnName("public_key")
                        .HasColumnType("text")
                        .HasComment("This is the public key for this Atlassian product instance. This field is deprecated and should not be used");

                    b.Property<string>("ServerVersion")
                        .HasColumnName("server_version")
                        .HasColumnType("text")
                        .HasComment("This is a string representation of the host product's version. Generally you should not need it");

                    b.Property<string>("SharedSecret")
                        .IsRequired()
                        .HasColumnName("shared_secret")
                        .HasColumnType("text")
                        .HasComment("Use this string to sign outgoing JWT tokens and validate incoming JWT tokens");

                    b.HasKey("Id");

                    b.HasIndex("ClientKey");

                    b.HasIndex("ClientStateId");

                    b.ToTable("client_registration");

                    b.HasComment("data payload with important tenant information");
                });

            modelBuilder.Entity("AsposePdfExporter.Model.ClientState", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasComment("Unique ID")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo);

                    b.Property<string>("Text")
                        .HasColumnName("text")
                        .HasColumnType("text")
                        .HasComment("state description");

                    b.HasKey("Id");

                    b.ToTable("client_state");

                    b.HasComment("client states");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Text = "app-installed"
                        },
                        new
                        {
                            Id = 2,
                            Text = "app-uninstalled"
                        },
                        new
                        {
                            Id = 3,
                            Text = "app-enabled"
                        },
                        new
                        {
                            Id = 4,
                            Text = "app-disabled"
                        });
                });

            modelBuilder.Entity("AsposePdfExporter.Model.ReportFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasComment("Unique ID")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo);

                    b.Property<DateTime?>("Accessed")
                        .HasColumnName("accessed")
                        .HasColumnType("timestamp")
                        .HasComment("accessed time");

                    b.Property<int>("ClientId")
                        .HasColumnName("client_reg_id")
                        .HasColumnType("integer")
                        .HasComment("client registration id");

                    b.Property<string>("ContentType")
                        .HasColumnName("content_type")
                        .HasColumnType("text")
                        .HasComment("report content type (application/pdf, etc)");

                    b.Property<DateTime?>("Created")
                        .HasColumnName("created")
                        .HasColumnType("timestamp")
                        .HasComment("created time");

                    b.Property<DateTime?>("Expired")
                        .HasColumnName("expired")
                        .HasColumnType("timestamp")
                        .HasComment("expire time");

                    b.Property<string>("FileName")
                        .HasColumnName("file_name")
                        .HasColumnType("text")
                        .HasComment("report file name");

                    b.Property<long>("FileSize")
                        .HasColumnName("file_size")
                        .HasColumnType("bigint")
                        .HasComment("report file size in bytes");

                    b.Property<string>("ReportType")
                        .HasColumnName("report_type")
                        .HasColumnType("text")
                        .HasComment("report type: pdf, xlsx, docx, etc");

                    b.Property<string>("StorageFileName")
                        .HasColumnName("storage_file_name")
                        .HasColumnType("text")
                        .HasComment("report file name in storage");

                    b.Property<string>("StorageFolder")
                        .HasColumnName("storage_folder")
                        .HasColumnType("text")
                        .HasComment("folder for storage_file_name");

                    b.Property<string>("UniqueId")
                        .HasColumnName("unique_id")
                        .HasColumnType("text")
                        .HasComment("Some Unique string to access directly by url");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.HasIndex("UniqueId");

                    b.ToTable("report_file");

                    b.HasComment("report files");
                });

            modelBuilder.Entity("AsposePdfExporter.Model.AccessLog", b =>
                {
                    b.HasOne("AsposePdfExporter.Model.ClientRegistration", "Client")
                        .WithMany("AccessLog")
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("AsposePdfExporter.Model.ClientRegistration", b =>
                {
                    b.HasOne("AsposePdfExporter.Model.ClientState", "ClientState")
                        .WithMany("ClientRegistrations")
                        .HasForeignKey("ClientStateId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();
                });

            modelBuilder.Entity("AsposePdfExporter.Model.ReportFile", b =>
                {
                    b.HasOne("AsposePdfExporter.Model.ClientRegistration", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
