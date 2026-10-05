namespace Rivet.Homeland.Infrastructure;

internal static class HomelandAssets
{
    private static string Root=>Path.Combine(AppContext.BaseDirectory,"assets","ThirdParty");
    private static readonly Dictionary<string,string?> Cache=[];

    private static string? Exact(params string[] parts)
    {
        var path=Path.Combine([Root,..parts]);
        return File.Exists(path)?path:null;
    }

    public static string? Find(params string[] tokens)
    {
        var key=string.Join('|',tokens).ToLowerInvariant();
        if(Cache.TryGetValue(key,out var cached))return cached;

        var roots=new[]{Path.Combine(Root,"Quaternius"),Path.Combine(Root,"Kenney")};
        var exts=new HashSet<string>(StringComparer.OrdinalIgnoreCase){".glb",".gltf",".obj",".fbx"};
        foreach(var root in roots.Where(Directory.Exists))
        {
            var files=Directory.EnumerateFiles(root,"*.*",SearchOption.AllDirectories)
                .Where(f=>exts.Contains(Path.GetExtension(f)))
                .OrderBy(f=>Path.GetExtension(f).Equals(".glb",StringComparison.OrdinalIgnoreCase)?0:1)
                .ThenBy(f=>f,StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var hit=files.FirstOrDefault(f=>tokens.All(t=>Path.GetFileNameWithoutExtension(f).Contains(t,StringComparison.OrdinalIgnoreCase)))
                ??files.FirstOrDefault(f=>tokens.Any(t=>Path.GetFileNameWithoutExtension(f).Contains(t,StringComparison.OrdinalIgnoreCase)));
            if(hit is not null)return Cache[key]=hit;
        }
        return Cache[key]=null;
    }

    public static string? Civilian(string? outfit=null)
    {
        // Keep identity/outfit selection deterministic even when all four baseline
        // models are visually different rather than modular clothing pieces.
        var variants=new[]
        {
            Exact("Kenney","mini-characters","character-male-a.glb"),
            Exact("Kenney","mini-characters","character-male-c.glb"),
            Exact("Kenney","mini-characters","character-female-b.glb"),
            Exact("Kenney","mini-characters","character-female-d.glb")
        }.Where(x=>x is not null).Cast<string>().ToArray();

        if(variants.Length>0)
        {
            var key=outfit??"civilian";
            var hash=17;
            foreach(var ch in key)hash=unchecked(hash*31+ch);
            return variants[(hash&int.MaxValue)%variants.Length];
        }

        return Find("character")??Find("human");
    }

    public static string? Cisf()=>
        Find("kickback","soldier")
        ??Find("soldier")
        ??Exact("Kenney","blocky-characters","character-r.glb")
        ??Civilian("utility-jacket");

    public static string? Car()=>Exact("Kenney","car-kit","sedan.glb")??Find("sedan")??Find("car");
    public static string? Truck()=>Exact("Kenney","car-kit","truck.glb")??Find("truck");
    public static string? PoliceVehicle()=>Exact("Kenney","car-kit","police.glb")??Find("police","vehicle");

    public static string? House(string seed)
    {
        var names=new[]{"building-type-a.glb","building-type-h.glb","building-type-t.glb"};
        return Variant("city-kit-suburban",names,seed)??Find("building","type");
    }

    public static string? Building(string seed)
    {
        var names=new[]{"building-a.glb","building-h.glb","building-skyscraper-a.glb"};
        return Variant("city-kit-commercial",names,seed)??Find("building");
    }

    public static string? RoadStraight()=>Exact("Kenney","city-kit-roads","road-straight.glb");
    public static string? RoadCrossroad()=>Exact("Kenney","city-kit-roads","road-crossroad-path.glb");
    public static string? RoadIntersection()=>Exact("Kenney","city-kit-roads","road-intersection-path.glb");
    public static string? RoadBend()=>Exact("Kenney","city-kit-roads","road-bend-square.glb");

    private static string? Variant(string folder,string[] names,string seed)
    {
        var hash=23;
        foreach(var ch in seed)hash=unchecked(hash*37+ch);
        for(var i=0;i<names.Length;i++)
        {
            var name=names[((hash&int.MaxValue)+i)%names.Length];
            var path=Exact("Kenney",folder,name);
            if(path is not null)return path;
        }
        return null;
    }
}
