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
        var tagGroupIds = tagGroups.Select(x => x.Id).ToArray();

        List<TagResponse> tags = [];
        foreach (var tagGroupId in tagGroupIds)
        {
            var groupIdTags = await Client.ExecuteXtmWithJson<List<TagResponse>>(
                $"{ApiEndpoints.TagGroups}/{tagGroupId}/tags",
                RestSharp.Method.Get,
                null,
                Creds
            );
            tags.AddRange(groupIdTags);
        }

        return tags.Select(x => new DataSourceItem(x.Id, x.Name)).ToList();
    }
}
