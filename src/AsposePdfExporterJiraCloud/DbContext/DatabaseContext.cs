using Aspose.Cloud.Marketplace.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AtlConnect = Aspose.Cloud.Marketplace.App.Atlassian.Connect;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.DbContext
{
    public partial class DatabaseContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationExpression _configurationExpression;

        public DatabaseContext()
        {
        }

        public DatabaseContext(DbContextOptions<DatabaseContext> options, IConfiguration configuration, IConfigurationExpression configurationExpression)
            : base(options)
        {
            _configuration = configuration;
            _configurationExpression = configurationExpression;
        }

        public DbSet<Model.ClientRegistration> ClientRegistration { get; set; }
        public DbSet<Model.ReportFile> ReportFile { get; set; }
        public DbSet<Model.ClientState> ClientState { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseNpgsql(_configurationExpression.Get("ConnectionStrings:DefaultConnection"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.1-servicing-10028");
            //modelBuilder.UseIdentityColumns();
            modelBuilder.UseHiLo("hilo_seq");

            //modelBuilder.HasSequence<int>("UniqueSeq").StartsAt(1)
            //    .IncrementsBy(1);

            modelBuilder.Entity<Model.ClientRegistration>(entity =>
            {
                var table = entity.ToTable("client_registration");
                table.HasKey(e => e.Id);
                table.HasComment("data payload with important tenant information");
                entity.HasOne(e => e.ClientState)
                    .WithMany(c => c.ClientRegistrations)
                    .OnDelete(DeleteBehavior.ClientNoAction)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.ClientKey);

                entity.Ignore(e => e.EventType);
                /*
                entity.HasRequired<Model.ClientState>(s => s.CurrentGrade)
                    .WithMany(g => g.Students)
                    .HasForeignKey<int>(s => s.CurrentGradeId);
                */
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasComment("Unique ID");
                
                entity.Property(e => e.UserAccountId)
                    .HasColumnName("user_account_id")
                    .HasComment("user_account_id");

                entity.Property(e => e.ClientStateId)
                    .IsRequired()
                    .HasColumnName("client_state_id").HasComment("client state id");
                
                entity.Property(e => e.Key)
                    .HasColumnName("key")
                    .HasComment("App key that was installed into the Atlassian Product, as it appears in your app's descriptor");
                
                entity.Property(e => e.ClientKey)
                    .HasColumnName("client_key")
                    .HasComment("Identifying key for the Atlassian product instance that the app was installed into. This will never change for a given instance, and is unique across all Atlassian product tenants");
                
                entity.Property(e => e.PublicKey)
                    .HasColumnName("public_key")
                    .HasComment("This is the public key for this Atlassian product instance. This field is deprecated and should not be used");
                
                entity.Property(e => e.SharedSecret)
                    .IsRequired()
                    .HasColumnName("shared_secret")
                    .HasComment("Use this string to sign outgoing JWT tokens and validate incoming JWT tokens");
                
                entity.Property(e => e.ServerVersion)
                    .HasColumnName("server_version")
                    .HasComment("This is a string representation of the host product's version. Generally you should not need it");
                
                entity.Property(e => e.PluginsVersion)
                    .HasColumnName("plugins_version")
                    .HasComment("This is a semver compliant version of Atlassian Connect which is running on the host server");
                
                entity.Property(e => e.BaseUrl)
                    .HasColumnName("base_url")
                    .HasComment("URL prefix for this Atlassian product instance. All of its REST endpoints begin with this `baseUrl`");
                
                entity.Property(e => e.ProductType)
                    .HasColumnName("product_type")
                    .HasComment("Identifies the category of Atlassian product, e.g. jira or confluence");
                
                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasComment("The host product description - this is customisable by an instance administrator");
                
                entity.Property(e => e.Created)
                    .HasColumnName("created")
                    .HasColumnType("timestamp")
                    .HasComment("Row created");
            });

            modelBuilder.Entity<Model.ClientState>(entity =>
            {
                var table = entity.ToTable("client_state");
                table.HasKey(e => e.Id);
                table.HasComment("client states");
                entity.Property(e => e.Id).HasColumnName("id").HasComment("Unique ID");
                entity.Property(e => e.Text).HasColumnName("text").HasComment("state description");

                /*entity.HasData(new Model.ClientState() { Id = (int)AtlConnect.Enum.eRegTypes.AppInstalled, Text = "app-installed" });
                entity.HasData(new Model.ClientState() { Id = (int)AtlConnect.Enum.eRegTypes.AppUninstalled, Text = "app-uninstalled" });
                entity.HasData(new Model.ClientState() { Id = (int)AtlConnect.Enum.eRegTypes.AppEnabled, Text = "app-enabled" });
                entity.HasData(new Model.ClientState() { Id = (int)AtlConnect.Enum.eRegTypes.AppDisabled, Text = "app-disabled" });
                */
            });

            modelBuilder.Entity<Model.ReportFile>(entity =>
            {
                var table = entity.ToTable("report_file");
                table.HasKey(e => e.Id);
                table.HasComment("report files");

                entity.Property(e => e.Id).HasColumnName("id").HasComment("Unique ID");
                entity.Property(e => e.UniqueId).HasColumnName("unique_id").HasComment("Some Unique string to access directly by url");
                entity.HasIndex(e => e.UniqueId);
                entity.Property(e => e.ReportType).HasColumnName("report_type").HasComment("report type: pdf, xlsx, docx, etc");
                entity.Property(e => e.ContentType).HasColumnName("content_type").HasComment("report content type (application/pdf, etc)");
                entity.Property(e => e.FileName).HasColumnName("file_name").HasComment("report file name");
                entity.Property(e => e.StorageFileName).HasColumnName("storage_file_name").HasComment("report file name in storage");
                entity.Property(e => e.StorageFolder).HasColumnName("storage_folder").HasComment("folder for storage_file_name");
                entity.Property(e => e.FileSize).HasColumnName("file_size").HasComment("report file size in bytes");

                entity.Property(e => e.ClientId).HasColumnName("client_reg_id").HasComment("client registration id");
                entity.Property(e => e.Created)
                    .HasColumnName("created")
                    .HasColumnType("timestamp")
                    .HasComment("created time");
                entity.Property(e => e.Accessed)
                    .HasColumnName("accessed")
                    .HasColumnType("timestamp")
                    .HasComment("accessed time");
                entity.Property(e => e.Expired)
                    .HasColumnName("expired")
                    .HasColumnType("timestamp")
                    .HasComment("expire time");
            });

            
        }
    }
}
