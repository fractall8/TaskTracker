using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Services.Configuration;
using Services.DI;
using WebApp.Shared.Toast;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<WebApp.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;

    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;

    config.SnackbarConfiguration.SuccessIcon = Icons.Material.Outlined.CheckCircleOutline;
    config.SnackbarConfiguration.ErrorIcon = Icons.Material.Outlined.ErrorOutline;
    config.SnackbarConfiguration.InfoIcon = Icons.Material.Outlined.Info;
    config.SnackbarConfiguration.WarningIcon = Icons.Material.Outlined.WarningAmber;
});

builder.Services.AddWebAppServices(builder.Configuration);

var apiOptions = builder.Configuration.GetSection(ApiClientOptions.SectionName).Get<ApiClientOptions>()!;

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    foreach (var scope in apiOptions.Scopes)
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
    }

    options.ProviderOptions.LoginMode = "redirect";
});

builder.Services.AddScoped<CustomToastService>();

await builder.Build().RunAsync();
