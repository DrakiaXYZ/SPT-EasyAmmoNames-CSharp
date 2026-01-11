using SPTarkov.Server.Core.Models.Common;

namespace EasyAmmoNames;

internal class Config
{
    public required Dictionary<MongoId, ConfigItem> Items { get; set; }
}

internal class ConfigItem
{
    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public required string Description { get; set; }
}