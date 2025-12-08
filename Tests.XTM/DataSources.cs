using Tests.XTM.Base;
using Apps.XTM.Constants;
using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.XTM;

[TestClass]
public class DataSources : TestBaseMultipleConnections
{
    [ContextDataSource, TestMethod]
    public async Task CustomerHandler_IsSSuccess(InvocationContext context)
    {
        // Arrange
        var handler = new CustomerDataHandler(context);

        // Act
        var result = await handler.GetDataAsync(new DataSourceContext { SearchString = "" }, CancellationToken.None);

        // Assert
        PrintDataHandlerResult(result);
        Assert.IsNotNull(result);
    }

    [ContextDataSource(ConnectionTypes.GeneratedToken), TestMethod]
    public async Task DictionaryDataHandlerReturnsValues(InvocationContext context)
    {
        // Arrange
        var handler = new ProjectDataHandler(context);

        // Act
        var response = await handler.GetDataAsync(new() { SearchString = "" }, CancellationToken.None);

        // Assert
        PrintDataHandlerResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task ProjectDataHandlerReturnsValues(InvocationContext context)
    {
        // Arrange
        var handler = new ProjectDataHandler(context);

        // Act
        var response = await handler.GetDataAsync(new DataSourceContext { }, CancellationToken.None);

        // Assert
        PrintDataHandlerResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task WorkflowStepDataHandlerReturnsValues(InvocationContext context)
    {
        // Arrange
        var handler = new WorkflowStepDataHandler(context);

        // Act
        var response = await handler.GetDataAsync(new() { SearchString = "" }, CancellationToken.None);

        // Assert
        PrintDataHandlerResult(response);
        Assert.IsNotNull(response);
    }

    [ContextDataSource, TestMethod]
    public async Task WorkflowDataHandlerReturnsValues(InvocationContext context)
    {
        // Arrange
        var handler = new WorkflowDataHandler(context);

        // Act
        var response = await handler.GetDataAsync(new() { SearchString = "" }, CancellationToken.None);

        // Assert
        PrintDataHandlerResult(response);
        Assert.IsNotNull(response);
    }
}
