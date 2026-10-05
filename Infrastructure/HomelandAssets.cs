namespace Rivet.Homeland.Infrastructure;
internal static class HomelandAssets
{
    static string Root=>Path.Combine(AppContext.BaseDirectory,"assets");
    static readonly Dictionary<string,string?> Cache=[];
    public static string? Find(params string[] tokens)
    {
        var key=string.Join('|',tokens).ToLowerInvariant();if(Cache.TryGetValue(key,out var c))return c;
        var roots=new[]{Path.Combine(Root,"ThirdParty","Quaternius"),Path.Combine(Root,"ThirdParty","Kenney")};
        var exts=new HashSet<string>(StringComparer.OrdinalIgnoreCase){".glb",".gltf",".obj",".fbx"};
        foreach(var r in roots.Where(Directory.Exists))
        {
            var files=Directory.EnumerateFiles(r,"*.*",SearchOption.AllDirectories).Where(f=>exts.Contains(Path.GetExtension(f))).OrderBy(f=>Path.GetExtension(f).Equals(".glb",StringComparison.OrdinalIgnoreCase)?0:1);
            var hit=files.FirstOrDefault(f=>tokens.All(t=>Path.GetFileNameWithoutExtension(f).Contains(t,StringComparison.OrdinalIgnoreCase)))??files.FirstOrDefault(f=>tokens.Any(t=>Path.GetFileNameWithoutExtension(f).Contains(t,StringComparison.OrdinalIgnoreCase)));
            if(hit is not null)return Cache[key]=hit;
        }
        return Cache[key]=null;
    }
    public static string? Civilian()=>Find("character")??Find("human");
    public static string? Cisf()=>Find("soldier")??Find("police");
    public static string? Car()=>Find("car");
    public static string? Truck()=>Find("truck");
    public static string? House()=>Find("house");
    public static string? Building()=>Find("building");
    public static string? Road()=>Find("road");
}
