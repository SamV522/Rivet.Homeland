using Rivet;

namespace Rivet.Homeland.Infrastructure;

internal readonly record struct ModelPlacement(string? Path, Vec3 VisualScale, Vec3 ColliderHalfExtents)
{
    public static ModelPlacement Missing(Vec3 half)=>new(null,new Vec3(1,1,1),half);
}

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
        Exact("Kenney","blocky-characters","character-r.glb")
        ??Find("kickback","soldier")
        ??Find("soldier")
        ??Civilian("utility-jacket");

    // Human physics is 1.75 m tall. These compensate for the very different
    // source units/aspect ratios of the two Kenney character packs.
    public static Vec3 HumanVisualScale(string? path)
    {
        if(path is null)return new Vec3(1,1,1);
        var normalized=path.Replace('\\','/').ToLowerInvariant();
        if(normalized.Contains("/mini-characters/"))return new Vec3(.90f,2.60f,1.50f);
        if(normalized.Contains("/blocky-characters/"))return new Vec3(.45f,.65f,.75f);
        return new Vec3(1,1,1);
    }

    public static ModelPlacement House(string seed)
    {
        var path=Variant("city-kit-suburban",
            ["building-type-a.glb","building-type-h.glb","building-type-t.glb"],seed);
        return Placement(path,new Vec3(5.4f,5.4f,5.4f),new Vec3(3.5f,2.2f,2.8f));
    }

    public static ModelPlacement BazaarBuilding(string seed)
    {
        // Deliberately exclude the skyscraper from the bazaar.
        var path=Variant("city-kit-commercial",["building-a.glb","building-h.glb"],seed);
        return Placement(path,new Vec3(4.2f,4.2f,4.2f),new Vec3(1.9f,2.7f,2.0f));
    }

    public static ModelPlacement CbdBuilding(string seed)
    {
        var path=Variant("city-kit-commercial",
            ["building-a.glb","building-h.glb","building-skyscraper-a.glb"],seed);
        return Placement(path,new Vec3(5f,5f,5f),new Vec3(2.4f,4f,2.5f));
    }

    public static ModelPlacement FobBuilding()=>
        Placement(
            Exact("Kenney","city-kit-commercial","building-h.glb"),
            new Vec3(6.5f,6.5f,6.5f),
            new Vec3(2.9f,4.2f,3.3f));

    public static ModelPlacement PoliceStation(string seed)
    {
        var path=seed.Contains("Village",StringComparison.OrdinalIgnoreCase)
            ? Exact("Kenney","city-kit-commercial","building-a.glb")
            : Exact("Kenney","city-kit-commercial","building-h.glb");
        return Placement(path,new Vec3(5.5f,5.5f,5.5f),new Vec3(2.5f,3.6f,2.7f));
    }

    public static ModelPlacement Car()=>
        Placement(
            Exact("Kenney","car-kit","sedan.glb")??Find("sedan")??Find("car"),
            new Vec3(1.20f,1.10f,1.50f),
            new Vec3(.90f,.72f,1.92f));

    public static ModelPlacement Truck()=>
        Placement(
            Exact("Kenney","car-kit","truck.glb")??Find("truck"),
            new Vec3(1.20f,1.10f,1.50f),
            new Vec3(.90f,.72f,2.22f));

    public static ModelPlacement PoliceVehicle()=>
        Placement(
            Exact("Kenney","car-kit","police.glb")??Find("police","vehicle"),
            new Vec3(1.20f,1.10f,1.50f),
            new Vec3(.90f,.72f,2.33f));

    public static string? RoadStraight()=>Exact("Kenney","city-kit-roads","road-straight.glb");

    private static ModelPlacement Placement(string? path,Vec3 scale,Vec3 fallbackHalf)
    {
        if(path is null)return ModelPlacement.Missing(fallbackHalf);
        var source=SourceSize(path);
        if(source is null)return new ModelPlacement(path,scale,fallbackHalf);
        var size=source.Value;
        return new ModelPlacement(
            path,
            scale,
            new Vec3(size.X*scale.X*.5f,size.Y*scale.Y*.5f,size.Z*scale.Z*.5f));
    }

    private static Vec3? SourceSize(string path)
    {
        var normalized=path.Replace('\\','/').ToLowerInvariant();
        var file=Path.GetFileName(normalized);
        if(normalized.Contains("/city-kit-suburban/"))
            return file switch
            {
                "building-type-a.glb"=>new Vec3(1.300f,.834f,1.028f),
                "building-type-h.glb"=>new Vec3(1.300f,.737f,.916f),
                "building-type-t.glb"=>new Vec3(1.314f,1.156f,1.406f),
                _=>null
            };
        if(normalized.Contains("/city-kit-commercial/"))
            return file switch
            {
                "building-a.glb"=>new Vec3(.884f,1.293f,.940f),
                "building-h.glb"=>new Vec3(.884f,1.293f,1.008f),
                "building-skyscraper-a.glb"=>new Vec3(1.360f,2.880f,1.360f),
                _=>null
            };
        if(normalized.Contains("/car-kit/"))
            return file switch
            {
                "sedan.glb"=>new Vec3(1.500f,1.300f,2.550f),
                "truck.glb"=>new Vec3(1.500f,1.300f,2.950f),
                "police.glb"=>new Vec3(1.500f,1.300f,3.100f),
                _=>null
            };
        return null;
    }

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
