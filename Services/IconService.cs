using Microsoft.JSInterop;
using TG.Blazor.IndexedDB;
using ucc.Models;
using ucc.Data;
using System.Timers;
using System.Text.Json;

namespace ucc.Services;

public class IconService(IndexedDBManager db, IJSRuntime jsRuntime, LocalStorage ls) : IAsyncDisposable
{
    private readonly IndexedDBManager DB = db;
    private readonly IJSRuntime JS = jsRuntime;
    private readonly LocalStorage LS = ls;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, CacheEntry> cache = [];
    private class CacheEntry
    {
        public required string url;
        public int Count;
    }

    private readonly double TimerInterval = 10000;
    private System.Timers.Timer timer = null!;

    public bool IsIconsEnabled { get; private set; } = true;

    public event Action<string>? OnIconUpdate;

    public async Task InitializeAsync()
    {
        IsIconsEnabled = await LS.Get<bool>("iconIsEnabled", IsIconsEnabled);
    }

    public async ValueTask DisposeAsync()
    {
        await EvictCache(true);
    }

    #region Eviction Timer

    private void StartEvictionCountdown()
    {
        // Some idempotency
        if (timer != null)
            return;

        timer = new(TimerInterval)
        {
            AutoReset = false,
            Enabled = true,
        };

        timer.Elapsed += OnTimeout;
    }

    private async void OnTimeout(object? sender, ElapsedEventArgs e)
    {
        await EvictCache();
    }

    #endregion

    #region Icon Ops

    /// <summary>
    /// Adds Icon object into the Database
    /// </summary>
    /// <param name="icon">Icon object with byte[] data</param>
    public async Task AddIcon(Icon icon, bool isUpdate = false)
    {
        if (icon == null || string.IsNullOrEmpty(icon.Id))
            return;

        await EvictIcon(icon.Id);
        await SetIconSize(icon.Id, icon.FileBytes.Length);

        StoreRecord<Icon> record = new()
        {
            Storename = IndexedDB.Icons,
            Data = icon,
        };

        // Console.WriteLine($"Adding icon {icon.Id} new: {isUpdate}");
        if (isUpdate)
        {
            await DB.UpdateRecord(record);
        }
        else
        {
            await DB.AddRecord(record);
        }

        OnIconUpdate?.Invoke(icon.Id);
    }

    /// <summary>
    /// Gets Icon object from the DB
    /// </summary>
    /// <param name="id">Icon Id in the Database</param>
    /// <returns>Icon with byte[] data</returns>
    public async Task<Icon> GetIcon(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null!;

        return await DB.GetRecordById<string, Icon>(IndexedDB.Icons, id);
    }

    /// <summary>
    /// Converts Icon object into a blob url
    /// </summary>
    /// <param name="icon">Icon object with byte[] data</param>
    /// <returns>Blob URL</returns>
    public async Task<string> GetIconUrlFromObject(Icon icon)
    {
        if (icon == null)
            return "";

        string url = await JS.InvokeAsync<string>("createBlobUrl", icon.FileBytes, icon.FileType);
        // Console.WriteLine($"URL FOR {icon.Id} {url}");

        return url;
    }

    /// <summary>
    /// Revokes Icon object URL
    /// </summary>
    /// <param name="id">Icon id</param>
    public async Task RevokeIconUrl(string url)
    {
        // Console.WriteLine($"REVOKED URL: {url}");
        await JS.InvokeVoidAsync("URL.revokeObjectURL", url);
    }

    /// <summary>
    /// Deletes Icon from the cache, database, and size dict
    /// </summary>
    /// <param name="id">Icon id</param>
    public async Task DeleteIcon(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        await EvictIcon(id);
        await DB.DeleteRecord(IndexedDB.Icons, id);
        await RemoveIconSize(id);
        OnIconUpdate?.Invoke(id);
    }

    /// <summary>
    /// Deletes the entire cache, IndexedDB icon store, and size dict
    /// </summary>
    public async Task ClearIcons()
    {
        await EvictCache(true);
        await DB.ClearStore(IndexedDB.Icons);
        await LS.Remove("iconSizeDict");
    }

    #endregion

    #region Cache Ops

    /// <summary>
    /// Gets Icon from cache, or fetches from DB, and then converts it into a blob.
    /// </summary>
    /// <param name="id">Icon Id in the Database</param>
    /// <returns>Blob URL</returns>
    public async Task<string> GetIconUrl(string id)
    {
        if (string.IsNullOrEmpty(id) || !IsIconsEnabled)
            return "";

        await _lock.WaitAsync();
        try
        {
            if (cache.TryGetValue(id, out CacheEntry? entry))
            {
                entry.Count++;
                // Console.WriteLine($"Cached return: {id}, {entry.url} : {entry.Count}");
                return entry.url;
            }

            string iconUrl = await GetIconUrlFromObject(await GetIcon(id));

            // Prevents non-existent icons from being added to the cache,
            // which means DB is queried every time an icon is requested and is null
            // if (string.IsNullOrEmpty(iconUrl))
            //     return "";

            CacheEntry newentry = new()
            {
                url = iconUrl,
                Count = 1,
            };

            // Console.WriteLine($"Cached add: {id}, {newentry.url} : {newentry.Count}");
            cache.Add(id, newentry);
            return newentry.url;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Decrements ref count of an icon in cache
    /// </summary>
    /// <param name="id">Icon ID</param>
    public async Task DisposeOfIcon(string id)
    {
        await _lock.WaitAsync();
        try
        {
            if (!cache.TryGetValue(id, out CacheEntry? entry))
                return;

            entry.Count--;
            // Console.WriteLine($"Cached dispose: {id}, {entry.url} : {entry.Count}");

            if (entry.Count == 0)
            {
                StartEvictionCountdown();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Evicts an icon from cache, if it exists
    /// </summary>
    /// <param name="id">Icon Id</param>
    private async Task EvictIcon(string id)
    {
        await _lock.WaitAsync();
        try
        {
            if (!cache.TryGetValue(id, out CacheEntry? entry))
                return;

            // Console.WriteLine($"Evicted: {id}");
            await RevokeIconUrl(entry.url);
            cache.Remove(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Evicts icons without references
    /// </summary>
    /// <param name="evictAll">Evict all icons regardless of reference count?</param>
    public async Task EvictCache(bool evictAll = false)
    {
        if (timer != null)
        {
            timer.Elapsed -= OnTimeout;
            timer.Dispose();
            timer = null!;
        }

        await _lock.WaitAsync();
        try
        {
            foreach ((string id, CacheEntry entry) in cache)
            {
                if (entry.Count > 0 && !evictAll)
                    continue;

                // Console.WriteLine($"BEGONE, {id}");
                await RevokeIconUrl(entry.url);
                cache.Remove(id);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Icon Sizes

    /// <summary>
    /// Sets the icon size in the size dictionary
    /// </summary>
    /// <param name="id"></param>
    /// <param name="bytes"></param>
    private async Task SetIconSize(string id, long bytes)
    {
        var SizeDict = await LS.Get<Dictionary<string, long>>("iconSizeDict", []);
        SizeDict[id] = bytes;
        await LS.Set("iconSizeDict", SizeDict);
    }

    /// <summary>
    /// Gets icon size dictionary from locaal storage, and sums it
    /// </summary>
    /// <returns>Cumulative icon size</returns>
    public async Task<int> GetTotalIconSize()
    {
        var SizeDict = await LS.Get<Dictionary<string, int>>("iconSizeDict", []);
        int size = 0;

        foreach ((string id, int bytes) in SizeDict)
        {
            size += bytes;
        }
        return size;
    }

    /// <summary>
    /// Loads all icons into memory (scary!), and writes their sizes to local storage
    /// </summary>
    /// <returns>Cumulative icon size</returns>
    public async Task<int> RecalculateTotalIconSize()
    {
        int size = 0;
        Dictionary<string, int> SizeDict = [];
        List<Icon> icons = await DB.GetRecords<Icon>(IndexedDB.Icons);
        foreach (Icon icon in icons)
        {
            var fileSize = icon.FileBytes.Length;
            size += fileSize;
            SizeDict[icon.Id] = fileSize;
        }

        await LS.Set("iconSizeDict", SizeDict);
        return size;
    }

    /// <summary>
    /// Removes the icon size from the size dictionary
    /// </summary>
    /// <param name="id"></param>
    private async Task RemoveIconSize(string id)
    {
        var SizeDict = await LS.Get<Dictionary<string, long>>("iconSizeDict", []);
        if (SizeDict.Count == 0)
            return;

        SizeDict.Remove(id);
        await LS.Set("iconSizeDict", SizeDict);
    }

    #endregion

    public async void ToggleIcons()
    {
        IsIconsEnabled = !IsIconsEnabled;
        await LS.Set("iconIsEnabled", IsIconsEnabled);

        if (!IsIconsEnabled)
        {
            await EvictCache(true);
        }
    }

    public void ShowCache()
    {
        Console.WriteLine(JsonSerializer.Serialize(cache, new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
        }));
    }
}