using MercuryOutpost.Web.AI;
using MercuryOutpost.Web.Components;
using MercuryOutpost.Web.Outpost.Runaround;
using MercuryOutpost.Web.Outpost;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddChatClients(builder.Configuration);

builder.Services.AddSingleton<World>();

builder.Services.AddSingleton(sp => new Human(
    RunaroundObjects.Human,
    sp.GetRequiredService<IChatClient>(),
    DefaultInstructions.Human,
    new HumanTools(sp.GetRequiredService<World>(), RunaroundObjects.Human).All));

builder.Services.AddSingleton(sp => new Robot(
    RunaroundObjects.Robot,
    sp.GetRequiredService<IChatClient>(),
    DefaultInstructions.Robot,
    new RobotTools(sp.GetRequiredService<World>(), RunaroundObjects.Robot).All));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
