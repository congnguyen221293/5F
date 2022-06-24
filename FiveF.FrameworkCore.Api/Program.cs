using System.Reflection;
using TCDev.ApiGenerator.Extension;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

await builder.Services.AddApiGeneratorServices(builder.Configuration, Assembly.GetExecutingAssembly());

var app = builder.Build();

// Configure the HTTP request pipeline.
//app.UseAutomaticApiMigrations(true);
app.UseApiGenerator();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseApiGenerator();
app.MapControllers();

//app.UseEndpoints(endpoints =>
//{
//    endpoints.UseApiGeneratorEndpoints();
//    endpoints.MapControllers();
//});
app.Run();
