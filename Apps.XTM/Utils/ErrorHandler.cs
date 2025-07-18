using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.XTM.Utils
{
    public static class ErrorHandler
    {
        public static async Task ExecuteWithErrorHandlingAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException(ex.Message);
            }
        }

        public static async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException(ex.Message);
            }
        }

        public static T ExecuteWithErrorHandling<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException(ex.Message);
            }
        }

        public static TEnum ParseOrMisconfig<TEnum>(string value, string paramName)
            where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
                throw new PluginMisconfigurationException($"No matching value `{paramName}`: {value}. Please check your input and try again");
            return result;
        }
    }
}
