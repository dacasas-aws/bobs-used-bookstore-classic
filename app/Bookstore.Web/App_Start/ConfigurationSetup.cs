using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using BobsBookstoreClassic.Data;
using Bookstore.Common;

namespace Bookstore.Web
{
    public static class ConfigurationSetup
    {
        public static void ConfigureConfiguration()
        {
            var rootPath = "/" + Constants.AppName;

            const string databasePath = "/Database";
            const string authenticationPath = "/Authentication";
            const string fileServicePath = "/Files";


            if (BookstoreConfiguration.GetSetting("Services/Database") == "aws")
            {
                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    var request = new GetParameterRequest { Name = $"{rootPath}{databasePath}/ConnectionStrings/BookstoreDatabaseConnection" };
                    var response = client.GetParameterAsync(request).GetAwaiter().GetResult();

                    if (response?.Parameter != null)
                    {
                        BookstoreConfiguration.AddSetting(response.Parameter.Name.Replace($"{rootPath}{databasePath}/", string.Empty), response.Parameter.Value);
                    }
                }
            }

            if (BookstoreConfiguration.GetSetting("Services/Authentication") == "aws")
            {
                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    LoadParametersByPath(client, $"{rootPath}{authenticationPath}/", rootPath);
                }
            }

            if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
            {
                using (var client = new AmazonSimpleSystemsManagementClient())
                {
                    LoadParametersByPath(client, $"{rootPath}{fileServicePath}/", rootPath);
                }
            }
        }

        private static void LoadParametersByPath(IAmazonSimpleSystemsManagement client, string path, string rootPath)
        {
            string nextToken = null;

            do
            {
                var request = new GetParametersByPathRequest { Path = path, Recursive = true, NextToken = nextToken };
                var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();

                if (response?.Parameters != null)
                {
                    foreach (var parameter in response.Parameters)
                    {
                        BookstoreConfiguration.AddSetting(parameter.Name.Replace($"{rootPath}/", string.Empty), parameter.Value);
                    }
                }

                nextToken = response?.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));
        }
    }
}