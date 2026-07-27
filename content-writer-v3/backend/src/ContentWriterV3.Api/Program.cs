using ContentWriterV3.Api.Hosting;
using ContentWriterV3.Infrastructure;
using ContentWriterV3.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Port=5432;Database=content_writer_v3;User Id=postgres;Password=postgres;";
builder.Services.AddContentWriterV3(connectionString);
builder.Services.AddContentWriterV3Api();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

// Run migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ContentWriterV3DbContext>();
    dbContext.Database.Migrate();
}

app.Run();
