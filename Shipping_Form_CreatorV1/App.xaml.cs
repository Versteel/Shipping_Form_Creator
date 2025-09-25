using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shipping_Form_CreatorV1.Data;
using Shipping_Form_CreatorV1.Services.Implementations;
using Shipping_Form_CreatorV1.Services.Interfaces;
using Shipping_Form_CreatorV1.ViewModels;
using System.Windows;

namespace Shipping_Form_CreatorV1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cXmRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdfdHRcRmdfVkJ3X0dWYEk=");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    @"\\store2\software\software\ShippingFormsCreator\Logs\DbLog.txt",
                    rollingInterval: RollingInterval.Day,   // 1 file per day
                    retainedFileCountLimit: 7,              // keep only last 7 days
                    fileSizeLimitBytes: 10_000_000,         // ~10 MB
                    rollOnFileSizeLimit: true               // roll if bigger than 10 MB
                )
                .CreateLogger();

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configure Logging
            services.AddLogging();

            // Register DbContext
            services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    "Server=store2,1433;Database=ShippingFormsDb;Integrated Security=SSPI;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;",
                    sql =>
                    {
                        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                        sql.CommandTimeout(60);
                    })
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine);
            });


            // Register Services
            services.AddSingleton<IOdbcService, OdbcService>();
            services.AddSingleton<ISqliteService, SqliteService>();
            services.AddSingleton<UserGroupService>();
            services.AddSingleton<DialogService>();
            services.AddSingleton<PrintService>();

            // Register ViewModels
            services.AddSingleton<MainViewModel>();

            // Register Views
            services.AddSingleton<MainWindow>();
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            // Dispose of services if needed
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            Log.CloseAndFlush();
        }
    }

}
