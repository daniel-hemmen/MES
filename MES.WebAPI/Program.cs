using MES.Common;
using MES.Common.Validators;
using MES.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MES.WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        List<StationOptionsConfiguration> stationConfig;



        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            string optionsConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "StationConfig.json");
            stationConfig = JsonSerializer.Deserialize<List<StationOptionsConfiguration>>(File.ReadAllText(optionsConfigPath), jsonOptions);
            StationOptionsValidator.Validate(stationConfig);
        }
        catch (Exception e)
        {

            Console.WriteLine($"Error loading configuration: {e.Message}");
            return;
        }


        var builder = WebApplication.CreateBuilder(args);

        string connectionString;

        try
        {
            connectionString = DbConnectionHelper.GetConnectionString();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }


        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddSingleton(stationConfig); // Register stationConfig so it can be used in the controller
        builder.Services.AddDbContext<DataContext>(options =>
        options.UseSqlite(connectionString));

        //builder.Services.AddDbContext<DataContext>(options =>
        //    options.UseSqlite());

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.UseCors(policy =>
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowAnyOrigin());


        app.MapControllers();

        app.Run();
    }
}
