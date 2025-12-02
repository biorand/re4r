using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    /// <summary>
    /// Represents a context for retrieving and replacing files in a pak for a standalone mod or rando.
    /// </summary>
    public interface IPatchContext
    {
        /// <summary>
        /// Gets the RSZ type repository for retrieving RSZ type definitions.
        /// </summary>
        RszTypeRepository TypeRepository { get; }

        /// <summary>
        /// Gets the dynamic data, data that may local or downloaded just-in-time.
        /// </summary>
        DynamicData DynamicData { get; }

        /// <summary>
        /// Gets the data for a vanilla file, or the data for a replaced file.
        /// </summary>
        /// <param name="path">E.g. "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2"</param>
        /// <returns>The raw file data.</returns>
        byte[]? GetFile(string path);

        /// <summary>
        /// Replaces a file with new data.
        /// </summary>
        /// <param name="path">E.g. "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2"</param>
        /// <param name="data">The raw file data.</param>
        void SetFile(string path, byte[] data);

        /// <summary>
        /// Gets a supplement file, e.g. a zip file containing resources to use.
        /// </summary>
        /// <param name="path">E.g. "flamethrower.zip" or "wpstats.csv".</param>
        byte[]? GetSupplementFile(string path);

        /// <summary>
        /// Gets a randomizer config option or the default value provided if not specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        T? GetConfigOption<T>(string key, T? defaultValue = default);

        bool ExportingMod { get; }
    }
}
