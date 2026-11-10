using System.IO;
using System.Reflection;
using Amazon.Rekognition;
using Amazon.S3;
using Autofac;
using BobsBookstoreClassic.Data;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Microsoft.AspNetCore.Hosting;

namespace Bookstore.Web
{
    public static class DependencyInjectionSetup
    {
        // Called from Program.cs through builder.Host.ConfigureContainer<ContainerBuilder>(...) with the
        // AutofacServiceProviderFactory. ASP.NET Core activates controllers itself, so RegisterControllers is gone.
        // ConfigurationSetup.ConfigureConfiguration() runs before the container is built, so the Services/* settings are available here.
        public static void ConfigureContainer(ContainerBuilder builder, IWebHostEnvironment environment)
        {
            builder.RegisterType<BookService>().As<IBookService>();
            builder.RegisterType<OrderService>().As<IOrderService>();
            builder.RegisterType<ReferenceDataService>().As<IReferenceDataService>();
            builder.RegisterType<OfferService>().As<IOfferService>();
            builder.RegisterType<CustomerService>().As<ICustomerService>();
            builder.RegisterType<AddressService>().As<IAddressService>();
            builder.RegisterType<ShoppingCartService>().As<IShoppingCartService>();
            builder.RegisterType<ImageResizeService>().As<IImageResizeService>();

            // ApplicationDbContext is registered in Program.cs through AddDbContext/UseSqlServer, which makes it scoped (one per request).

            builder.RegisterType<CustomerRepository>().As<ICustomerRepository>();
            builder.RegisterType<AddressRepository>().As<IAddressRepository>();
            builder.RegisterType<BookRepository>().As<IBookRepository>();
            builder.RegisterType<OfferRepository>().As<IOfferRepository>();
            builder.RegisterType<ShoppingCartRepository>().As<IShoppingCartRepository>();
            builder.RegisterType<OrderRepository>().As<IOrderRepository>();
            builder.RegisterType<ReferenceDataRepository>().As<IReferenceDataRepository>();

            builder.RegisterGeneric(typeof(PaginatedList<>)).As(typeof(IPaginatedList<>)).InstancePerLifetimeScope();

            if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
            {
                builder.RegisterType<AmazonS3Client>().As<IAmazonS3>();
                builder.RegisterType<S3FileService>().As<IFileService>();
            }
            else
            {
                // LocalFileService writes under <web root>/Content (it returns "/Content/images/coverimages/..." URLs);
                // IWebHostEnvironment.WebRootPath replaces System.Web.HttpRuntime.AppDomainAppPath.
                var webRootPath = !string.IsNullOrEmpty(environment.WebRootPath) ?
                    Path.Combine(environment.WebRootPath, "Content") :
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                builder.RegisterInstance(new LocalFileService(webRootPath)).As<IFileService>();
            }

            if (BookstoreConfiguration.GetSetting("Services/ImageValidationService") == "aws")
            {
                builder.RegisterType<AmazonRekognitionClient>().As<IAmazonRekognition>();
                builder.RegisterType<RekognitionImageValidationService>().As<IImageValidationService>();
            }
            else
            {
                builder.RegisterType<LocalImageValidationService>().As<IImageValidationService>();
            }

            // LocalAuthenticationMiddleware is conventional ASP.NET Core middleware activated by
            // app.UseMiddleware<LocalAuthenticationMiddleware>() in AuthenticationConfig.UseBookstoreAuthentication,
            // so it no longer needs a container registration.
        }
    }
}
