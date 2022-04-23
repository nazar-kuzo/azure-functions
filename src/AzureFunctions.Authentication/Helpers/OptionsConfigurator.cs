using System;

namespace Azure.Functions.Authentication.Helpers
{
    /// <summary>
    /// Registers configure action for specified Options class
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    internal class OptionsConfigurator<TOptions>
    {
        public Action<TOptions> Configure { get; set; }
    }
}
