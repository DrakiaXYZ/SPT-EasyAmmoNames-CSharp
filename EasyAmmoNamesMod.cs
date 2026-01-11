using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace EasyAmmoNames;

// Hide some warnings
#pragma warning disable CS0162 // Unreachable code, for debug
#pragma warning disable CS1998 // The fact our code doesn't await

// We want to make sure we load after anything that might be setting custom names
// as that is our sole purpose. So use a relatively high offset
[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 1000)]
public class EasyAmmoNamesMod(
	ISptLogger<EasyAmmoNamesMod> logger,
	DatabaseService databaseService,
    LocaleService localeService,
    ModHelper modHelper,
    JsonUtil jsonUtil,
    FileUtil fileUtil) : IOnLoad
{
    private const bool DEBUG = false;

    private Config? config;
    private readonly string configFolderPath = Path.Join(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config");

    public async Task OnLoad()
	{
        try
        {
            config = modHelper.GetJsonDataFromFile<Config>(configFolderPath, "config.json");
        }
        catch (Exception ex)
        {
            logger.Error("Error loading EasyAmmoNames config data. Disabling EasyAmmoNames", ex);
            return;
        }

        // Attach a LazyLoad transformer to all languages, so we overwrite the values for all users
        foreach (var lazyloadedValue in databaseService.GetLocales().Global.Values)
        {
            lazyloadedValue.AddTransformer(lazyloadedLocaleData =>
            {
                // VS thinks this can be null, so just return if it is
                if (lazyloadedLocaleData == null) return lazyloadedLocaleData;

                foreach (var (itemId, itemConfig) in config.Items)
                {
                    string itemName = $"{itemId} Name";
                    string itemShortName = $"{itemId} ShortName";
                    string itemDescription = $"{itemId} Description";
                    if (!string.IsNullOrEmpty(itemConfig.Name))
                    {
                        lazyloadedLocaleData[itemName] = itemConfig.Name;
                    }

                    if (!string.IsNullOrEmpty(itemConfig.ShortName))
                    {
                        if (itemConfig.ShortName.Length > 9)
                        {
                            logger.Error("Provided shortname was too long! Shortnames have a maximum of 9 characters.");
                            logger.Error($"Trimming {itemConfig.ShortName} to {itemConfig.ShortName.Substring(0, 9)}");
                            itemConfig.ShortName = itemConfig.ShortName.Substring(0, 9);
                        }

                        lazyloadedLocaleData[itemShortName] = itemConfig.ShortName;
                    }

                    if (!string.IsNullOrEmpty(itemConfig.Description))
                    {
                        lazyloadedLocaleData[itemDescription] = itemConfig.Description;
                    }
                }

                return lazyloadedLocaleData;
            });
        }

        if (DEBUG)
        {
            AddNewItemsToConfig();
        }

        return;
	}

    private void AddNewItemsToConfig()
    {
        var _locales = localeService.GetLocaleDb("en");

        bool updatedConfig = false;
        foreach (var (itemId, item) in databaseService.GetItems())
        {
            // Only process items with a parent of ammo/ammo_box
            if (item.Type != "Item") continue;
            if (item.Parent != BaseClasses.AMMO && item.Parent != BaseClasses.AMMO_BOX) continue;

            // Don't process items we already have
            if (config!.Items.ContainsKey(itemId)) continue;

            // Don't process items that aren't in the handbook
            if (databaseService.GetHandbook().Items?.FirstOrDefault(item => item.Id == itemId) == null) continue;

            // Don't process shrapnel or signal flares
            if (item.Name!.Contains("shrapnel", StringComparison.InvariantCultureIgnoreCase) || 
                item.Name!.Contains("patron_rsp", StringComparison.InvariantCultureIgnoreCase) ||
                item.Name!.Contains("patron_26x75", StringComparison.InvariantCultureIgnoreCase)) continue;

            // Skip if the item has an ammoType, but it's not "bullet" or "grenade"
            if (item.Properties?.AmmoType != null && item.Properties.AmmoType != "bullet" && item.Properties.AmmoType != "grenade") continue;

            // We've encountered a missing item that we should handle, output and add it to our config to be written to disk
            logger.Warning($"Item {item.Name} was not in list. tpl: {item.Id}");
            string itemName = $"{item.Id} Name";
            string itemShortName = $"{item.Id} ShortName";
            config.Items[item.Id] = new() {
                Name = $"FIXME {_locales[itemName]}",
                ShortName = _locales[itemShortName],
                Description = ""
            };
            updatedConfig = true;
        }

        // If we wrote new entries, dump the config back to disk
        if (updatedConfig)
        {
            fileUtil.WriteFile(Path.Combine(configFolderPath, "config.json"), jsonUtil.Serialize(config!, true)!);
        }
    }
}
