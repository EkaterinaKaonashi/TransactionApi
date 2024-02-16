using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using System;
using TransactionApi.API.Common.BackgroundServices;
using TransactionApi.API.Common.Models;
using TransactionApi.API.Common.Validators;
using TransactionApi.API.Services;

namespace TransactionApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.Configure<DbSettings>(builder.Configuration.GetSection("TransactionDb"));
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ITransactionService,TransactionService>();

            builder.Services.AddControllers();
            builder.Services.AddScoped<IValidator<Transaction>, TransactionValidator>();
            builder.Services.AddHostedService<RatesGetterBackgroundService>();
            builder.Services.AddHostedService<ExchangeRatesBackgroundService>();
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

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

            //app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
