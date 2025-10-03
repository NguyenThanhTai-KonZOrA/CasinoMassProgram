using Common.SystemConfiguration;
using Implement.ApplicationDbContext;
using Implement.Repositories;
using Implement.Repositories.Interface;
using Implement.Services;
using Implement.Services.Interface;
using Implement.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowSpa", p => p
        .WithOrigins("http://localhost:5173", "http://localhost:5175", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Db (SQL Server)
builder.Services.AddDbContext<CasinoMassProgramDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

#region Add Services
builder.Services.AddSingleton<ISystemConfiguration, SystemConfiguration>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddTransient<IExcelService, ExcelService>();
builder.Services.AddTransient<IAwardSettlementRepository, AwardSettlementRepository>();
builder.Services.AddTransient<ITeamRepresentativeMemberRepository, TeamRepresentativeMemberRepository>();
builder.Services.AddTransient<ITeamRepresentativeRepository, TeamRepresentativeRepository>();
builder.Services.AddTransient<IMemberRepository, MemberRepository>();
builder.Services.AddTransient<IImportBatchRepository, ImportBatchRepository>();
builder.Services.AddTransient<IImportCellErrorRepository, ImportCellErrorRepository>();
builder.Services.AddTransient<IImportRowRepository, ImportRowRepository>();
builder.Services.AddTransient<IPaymentTeamRepresentativeRepository, PaymentTeamRepresentativeRepository>();
builder.Services.AddTransient<ISettlementStatementService, SettlementStatementService>();
#endregion
// MVC + JSON
builder.Services
    .AddControllers()
    .AddJsonOptions(o => { o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowSpa");

// Apply migrations / create DB at startup
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<CasinoMassProgramDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed at startup");
            // Optionally rethrow in non-prod:
            // throw;
        }
    }
}
app.UseMiddleware<ApiMiddleware>();
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();