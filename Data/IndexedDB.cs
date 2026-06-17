using TG.Blazor.IndexedDB;

namespace ucc.Data;

public static class IndexedDB
{
    public readonly static string Items = "Items";
    public readonly static string Recipes = "Recipes";
    public readonly static string Icons = "Icons";

    public static void Inventory(DbStore dbStore)
    {
        dbStore.DbName = "Inventory";
        dbStore.Version = 2;
        dbStore.Stores.Add(new StoreSchema
        {
            Name = Items,
            PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = false, Unique = true },
            Indexes = new List<IndexSpec>
                    {
                        new IndexSpec{ Name="byName", KeyPath = "name", Auto = false, Unique = false },
                        new IndexSpec{ Name="byDate", KeyPath = "dateModified", Auto = false, Unique = false },
                    }
        });

        dbStore.Stores.Add(new StoreSchema
        {
            Name = Recipes,
            PrimaryKey = new IndexSpec { Name = "guid", KeyPath = "guid", Unique = true}
        });

        dbStore.Stores.Add(new StoreSchema
        {
            Name = Icons,
            PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Unique = true },
        });
    }
}