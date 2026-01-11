using SPTarkov.Server.Core.Models.Spt.Mod;

namespace EasyAmmoNames;

public record EasyAmmoNamesMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "mcdewgle.easyammonames";
    public override string Name { get; init; } = "Easy Ammo Names";
    public override string Author { get; init; } = "McDewgle";
    public override List<string>? Contributors { get; init; } = new() { "DrakiaXYZ" };
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}