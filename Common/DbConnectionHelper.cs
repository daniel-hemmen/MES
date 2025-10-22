using MES.Common.Exceptions;
using System.Text.Json;

namespace MES.Common
{
    public static class DbConnectionHelper
    {
        public static string GetConnectionString()
        {
            string baseDir = AppContext.BaseDirectory;
            string solutionDir = Directory.GetParent(baseDir).Parent.Parent.Parent.Parent.FullName;

            try
            {
                if (!Directory.Exists(Path.Combine(solutionDir, "Data")))
                {
                    Directory.CreateDirectory(Path.Combine(solutionDir, "Data"));
                }

                string dataDir = Path.Combine(solutionDir, "Data");
                string dbConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "DatabaseConfig.json");
                string dbConfigJson = File.ReadAllText(dbConfigPath);
                using var jsonDoc = JsonDocument.Parse(dbConfigJson);
                string configConnString = jsonDoc.RootElement.GetProperty("ConnectionString").GetString();
                string dbName = configConnString.Replace("Data Source=", "").Trim();
                string dbFullPath = Path.Combine(dataDir, dbName);
                string connectionString = $"Data Source={dbFullPath}";
                return connectionString;
            }
            catch (Exception ex)
            {

                throw new InvalidConfigurationException($"There was a problem getting the path to the database: {ex}");
            }
        }
    }
}
