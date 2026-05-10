using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ucc;
using ucc.Services;
using ucc.Data;
using TG.Blazor.IndexedDB;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddIndexedDB(IndexedDB.Inventory);
builder.Services.AddScoped<LocalStorage>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<CraftingService>();

var app = builder.Build();

await app.Services.GetRequiredService<InventoryService>().InitializeAsync();

await app.RunAsync();
