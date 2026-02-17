using Apps.XTM.Constants;
using Apps.XTM.Invocables;
using Apps.XTM.Models.Response.Tag;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.XTM.DataSourceHandlers;

public class TagDataHandler(InvocationContext context) : XtmInvocable(context), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        var tagGroups = await Client.ExecuteXtmWithJson<List<TagGroupResponse>>(
            ApiEndpoints.TagGroups,
            RestSharp.Method.Get,
            null,
            Creds
        );

        var getTagsTasks = tagGroups.Select(async group =>
        {
            var groupTags = await Client.ExecuteXtmWithJson<List<TagResponse>>(
                $"{ApiEndpoints.TagGroups}/{group.Id}/tags",
                RestSharp.Method.Get,
                null,
                Creds
            );

            return groupTags.Select(tag => new DataSourceItem(
                tag.Id,
                $"{tag.Name} ({group.Name})"
            ));
        }); 
        
        var results = await Task.WhenAll(getTagsTasks);

        return results
            .SelectMany(x => x)
            .Where(x =>
                context.SearchString == null ||
                x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
