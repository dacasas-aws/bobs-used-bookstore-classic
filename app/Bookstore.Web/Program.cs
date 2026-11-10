using Autofac;
using Autofac.Extensions.DependencyInjection;
using BobsBookstoreClassic.Data;
using Bookstore.Data;
using Bookstore.Web;
using Bookstore.Web.Areas;
using Microsoft.EntityFrameworkCore;
using NLog.Web;

// Replaces Global.asax (MvcApplication.Application_Start / Application_Error) and the OWIN Startup.Configuration.
var builder = WebApplication.CreateBuilder(args);

// BookstoreConfiguration is the static settings facade used throughout the app; it must be bound to
// IConfiguration before anything reads a setting (logging, SSM parameters, container and auth registration).
BookstoreConfiguration.Initialize(builder.Configuration);

// LoggingSetup.ConfigureLogging() builds the NLog configuration (AWS CloudWatch or debugger target);
// UseNLog routes ILogger<T> to it.
LoggingSetup.ConfigureLogging();
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// ConfigurationSetup.ConfigureConfiguration() pulls the AWS Systems Manager parameters when a Services/* setting is "aws".
// It runs before builder.Build() because the container and authentication registrations below read those settings.
ConfigurationSetup.ConfigureConfiguration();

if (BookstoreConfiguration.GetSetting("Services/Database") == "aws")
{
    // ConfigurationSetup stores the SSM connection string as a BookstoreConfiguration override; surface it to
    // IConfiguration so the AddDbContext registration below picks it up.
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:BookstoreDatabaseConnection"] =
            BookstoreConfiguration.GetSetting("ConnectionStrings/BookstoreDatabaseConnection")
    });
}

// DependencyInjectionSetup (Autofac): the host owns the container and gives each request its own lifetime scope.
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>((_, containerBuilder) =>
    DependencyInjectionSetup.ConfigureContainer(containerBuilder, builder.Environment));

// Replaces the per-request ApplicationDbContext(connectionString) registration.
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("BookstoreDatabaseConnection")));

// AuthenticationConfig: Cognito (OpenID Connect + cookie) or the local cookie scheme.
builder.Services.AddBookstoreAuthentication();
builder.Services.AddAuthorization();

// FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters).
builder.Services.AddControllersWithViews(options => FilterConfig.RegisterGlobalFilters(options.Filters));

// [PORT-NOTE] dropped BundleConfig.RegisterBundles(BundleTable.Bundles): System.Web.Optimization does not exist on
// ASP.NET Core. The layout references the scripts and stylesheets directly; they are served from wwwroot/Scripts and wwwroot/Content.

var app = builder.Build();

// Replaces the legacy EF database initializer: create the database if it is missing and seed the reference data and books.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    BookstoreDbInitializer.Initialize(context);
}

// Application_Error logged the unhandled exception through NLog and HandleErrorAttribute rendered the Error view.
// The exception handler middleware logs the exception through ILogger (routed to NLog) and re-executes the anonymous
// HomeController.Error action. In Development the developer exception page is shown instead.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

// AuthenticationConfig.UseBookstoreAuthentication: UseAuthentication(), followed by the LocalAuthenticationMiddleware
// when Services:Authentication is "local" (anything other than "aws").
app.UseBookstoreAuthentication();

app.UseAuthorization();

// AreaRegistration.RegisterAllAreas(): the Admin area route is registered before the default route.
AdminAreaRegistration.RegisterArea(app);

// RouteConfig.RegisterRoutes(RouteTable.Routes).
RouteConfig.RegisterRoutes(app);

app.Run();
