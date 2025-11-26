using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecurityReportWeb.Database.Models;
using SecurityReportWeb.Import.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ReportDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 匯入服務
builder.Services.AddScoped<IImportService, ImportService>();

// HTML 解析服務
builder.Services.AddScoped<IHtmlParserService, HtmlParserService>();

// XLSX 解析服務
builder.Services.AddScoped<IXlsxParserService, XlsxParserService>();

// CORS 設定
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3333")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 啟用 CORS（必須在 UseAuthorization 之前）
app.UseCors("AllowLocalhost");

app.UseAuthorization();

app.MapControllers();

app.Run();
