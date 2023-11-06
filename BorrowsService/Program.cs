using RabbitMQ.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Uygulama baþladýðýnda RabbitMQ baðlantýlarýný kapatmayý unutmayýn
//var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
//lifetime.ApplicationStopped.Register(() =>
//{
//    borrowChannel.Close();
//    borrowConnection.Close();
//});

app.Run();