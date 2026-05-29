using System.Text.Json;
using Microsoft.JSInterop;

namespace ucc.Data;

public class LocalStorage(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime JS = jsRuntime;

    public async ValueTask Set(string key, object value)
    {
        var serialisedData = JsonSerializer.Serialize(value);
        await JS.InvokeVoidAsync("localStorage.setItem", key, serialisedData);
    }

    public async ValueTask<TResult> Get<TResult>(string key, TResult? defaultValue = default)
    {
        string? data = await JS.InvokeAsync<string>("localStorage.getItem", key);
        if (data == null)
        {
            return defaultValue!;
        }

        var result = JsonSerializer.Deserialize<TResult>(data!);
        return result!;
    }

    public async ValueTask Remove(string key)
    {
        await JS.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async ValueTask Clear()
    {
        await JS.InvokeVoidAsync("localStorage.clear");
    }
}
