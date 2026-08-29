var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApiConfig();
builder.Services.AddFrontendCorsConfig(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUiConfig();
}

app.UseHttpsRedirection();



app.Run();

