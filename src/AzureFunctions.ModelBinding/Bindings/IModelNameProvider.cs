namespace Microsoft.Azure.WebJobs.Extensions.Http
{
    public interface IModelNameProvider
    {
        /// <summary>
        /// Mimics MVC ModelBinding attttribute that have ability to override name
        /// </summary>
        string Name { get; }
    }
}
