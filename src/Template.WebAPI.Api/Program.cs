global using Template.WebAPI.Api;
global using Template.WebAPI.Api.Common;
global using Template.WebAPI.Api.Common.Filters;
global using Template.WebAPI.Api.Common.Extensions;
global using Template.WebAPI.Api.Common.Configuration;
global using Template.WebAPI.Data;
global using Template.WebAPI.Data.Models;

global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.Extensions.Options;
global using Microsoft.EntityFrameworkCore;
global using System.Security.Claims;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
Log.Information("Starting Template.WebAPI!");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddAllServices();
    
    await using var app = builder.Build();
    
    app.Configure();
    
    await app.RunAsync();
    
    Log.Information("Stopping Template.WebAPI!");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;

