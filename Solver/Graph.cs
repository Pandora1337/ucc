using System.Text.Json;

namespace ucc.Solver;

public class Graph
{
    public Dictionary<Guid, HashSet<Guid>> GuidAdj { get; private set; } = [];

    /// <summary>
    /// Adds the specified node to a graph.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="parent"></param>
    /// <returns>
    /// true if the guid is linked to the parent node; false if the node is already present or parent is null
    /// </returns>
    public bool AddNode(Guid guid, Guid? parent = null)
    {
        GuidAdj[guid] = GuidAdj.GetValueOrDefault(guid, []);
        return parent.HasValue && !GuidAdj[parent.Value].Add(guid);
    }

    public List<List<int>> ConvertToInt()
    {
        var adj = new List<List<int>>();
        foreach ((Guid node, HashSet<Guid> deps) in GuidAdj)
        {
            var index = GetOrAddId(node);
            adj.Add([]);
            foreach (Guid dep in deps)
            {
                adj[^1].Add(GetOrAddId(dep));
            }
        }
        return adj;
    }

    Dictionary<Guid, int> _guidToInt = [];
    List<Guid> _intToGuid = [];
    int GetOrAddId(Guid guid)
    {
        if (!_guidToInt.TryGetValue(guid, out int index))
        {
            index = _intToGuid.Count;
            _intToGuid.Add(guid);
            _guidToInt.Add(guid, index);
        }
        return index;
    }

    public void Print()
    {
        Console.WriteLine(JsonSerializer.Serialize(GuidAdj));
        foreach (var node in GuidAdj)
        {
            Console.WriteLine($"{node.Key.ToString().Split("-")[0]}");

            foreach (var dep in node.Value)
            {
                Console.WriteLine($"    {dep.ToString().Split("-")[0]}");
            }
        }
    }

    #region Tarjans

    // The function to do DFS traversal.
    // It uses findSCC() to find all strongly connected components
    public List<List<Guid>> GetSCCs()
    {
        List<List<int>> adj = this.ConvertToInt();
        int n = adj.Count;

        int[] disc = new int[n];
        int[] low = new int[n];
        bool[] inSt = new bool[n];

        for (int i = 0; i < n; i++)
            disc[i] = -1;

        var st = new Stack<int>();
        int timer = 0;

        var allSCCs = new List<List<Guid>>();

        // Call the recursive helper function to find SCCs
        // in DFS tree with vertex i
        for (int i = 0; i < n; i++)
        {
            if (disc[i] == -1)
            {
                FindSCC(i, adj, disc, low, inSt, st, ref timer, allSCCs);
            }
        }

        return allSCCs;
    }

    private void FindSCC(int u, List<List<int>> adj, int[] disc, int[] low,
                        bool[] inSt, Stack<int> st, ref int timer, List<List<Guid>> allSCCs)
    {
        // Initialize discovery time and low value
        disc[u] = low[u] = ++timer;

        // Push current vertex to stack and mark it as in stack
        st.Push(u);
        inSt[u] = true;

        // Go through all vertices adjacent to this
        foreach (int v in adj[u])
        {
            // If v is not visited yet, then recur for it
            // Case 1: Tree edge
            if (disc[v] == -1)
            {
                FindSCC(v, adj, disc, low, inSt, st, ref timer, allSCCs);

                // Check if the subtree rooted with v has a
                // connection to one of the ancestors of u
                low[u] = Math.Min(low[u], low[v]);
            }

            // Update low value of u only if v is still in stack
            // Case 2: Back edge (not cross edge)
            else if (inSt[v])
            {
                low[u] = Math.Min(low[u], disc[v]);
            }
        }

        // If u is head node of SCC, pop the stack and store the SCC
        if (low[u] == disc[u])
        {
            var scc = new List<Guid>();

            // Pop all vertices from stack till u is found
            while (true)
            {
                int x = st.Pop();
                inSt[x] = false;
                // scc.Add(x);
                scc.Add(_intToGuid[x]);

                if (x == u)
                    break;
            }

            // Store one strongly connected component
            allSCCs.Add(scc);
        }
    }
    #endregion
}