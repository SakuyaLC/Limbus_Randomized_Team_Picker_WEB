var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("WikiHttpClient", client =>
{
    client.DefaultRequestHeaders.UserAgent.TryParseAdd("LimbusRandomizedTeamPicker/1.0");
    client.DefaultRequestHeaders.Accept.TryParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
});
builder.Services.AddScoped<Limbus_Randomized_Team_Picker_WEB.Services.IIdentityScraperService, Limbus_Randomized_Team_Picker_WEB.Services.IdentityScraperService>();
builder.Services.AddScoped<Limbus_Randomized_Team_Picker_WEB.Services.ITeamAssemblyService, Limbus_Randomized_Team_Picker_WEB.Services.TeamAssemblyService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"Limbus_Randomized_Team_Picker_WEB.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
