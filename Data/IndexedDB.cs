using TG.Blazor.IndexedDB;

namespace ucc.Data;

public static class IndexedDB
{
    public static string Items = "Items";
    public static string Recipes = "Recipes";
    public static void Inventory(DbStore dbStore)
    {
        dbStore.DbName = "Inventory";
        dbStore.Version = 1;
        dbStore.Stores.Add(new StoreSchema
        {
            Name = Items,
            PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = false, Unique = true },
            Indexes = new List<IndexSpec>
                    {
                        new IndexSpec{ Name="byName", KeyPath = "name", Auto = false, Unique = false },
                        new IndexSpec{ Name="byDate", KeyPath = "createdAt", Auto = false, Unique = false },
                    }
        });

        dbStore.Stores.Add(new StoreSchema
        {
            Name = Recipes,
            PrimaryKey = new IndexSpec { Name = "guid", KeyPath = "guid", Unique = true}
        });
    }
}