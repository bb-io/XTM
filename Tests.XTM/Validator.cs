using Tests.XTM.Base;
using Apps.XTM.Connections;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Tests.XTM;

[TestClass]
public class Validator : TestBase
{
    [TestMethod]
    public async Task ValidatesCorrectConnection()
    {
        // Arrange
        var validator = new ConnectionValidator();

        // Act
        var tasks = CredsGroups.Select(x => validator.ValidateConnection(x, CancellationToken.None).AsTask());
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(x => x.IsValid));
    }

    [TestMethod]
    public async Task DoesNotValidateIncorrectConnection()
    {
        // Arrange
        var validator = new ConnectionValidator();

        var newCreds = CredsGroups.First().Select(x => new AuthenticationCredentialsProvider(x.KeyName, x.Value + "_incorrect"));
        var result = await validator.ValidateConnection(newCreds, CancellationToken.None);
        Assert.IsFalse(result.IsValid);
    }
}