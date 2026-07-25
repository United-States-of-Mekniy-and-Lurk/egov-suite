using Gov.Cli.Core;

namespace Gov.Cli.Tests;

public class DiffEngineTests
{
    [Fact]
    public void BuildPlan_DetectsCreateUpdateDeleteForClientsAndRoles()
    {
        var desired = new DesiredState(
            "citizen-portal",
            [
                new DesiredClient("web", ["https://portal.local/*"], ["citizen.read"], ["citizen-api"]),
                new DesiredClient("mobile", ["https://mobile.local/*"], ["citizen.read"], ["citizen-api"]),
            ],
            ["citizen"]);

        var current = new CurrentState(
            [
                new CurrentClient("web", "1", ["https://old.local/*"], ["citizen.read"], []),
                new CurrentClient("legacy", "2", ["https://legacy.local/*"], ["legacy.read"], []),
            ],
            ["admin"]);

        var plan = DiffEngine.BuildPlan(desired, current);

        Assert.Single(plan.ClientsToCreate);
        Assert.Equal("mobile", plan.ClientsToCreate[0].LogicalName);

        Assert.Single(plan.ClientsToUpdate);
        Assert.Equal("web", plan.ClientsToUpdate[0].LogicalName);

        Assert.Single(plan.ClientsToDelete);
        Assert.Equal("legacy", plan.ClientsToDelete[0].LogicalName);

        Assert.Single(plan.RolesToCreate);
        Assert.Equal("citizen", plan.RolesToCreate[0].Name);

        Assert.Single(plan.RolesToDelete);
        Assert.Equal("admin", plan.RolesToDelete[0].Name);
    }

    [Fact]
    public void BuildPlan_IsEmpty_WhenCurrentMatchesDesired()
    {
        var desired = new DesiredState(
            "citizen-portal",
            [new DesiredClient("web", ["https://portal.local/*"], ["citizen.read"], ["citizen-api"])],
            ["citizen"]);

        var current = new CurrentState(
            [new CurrentClient("web", "1", ["https://portal.local/*"], ["citizen.read"], ["citizen-api"])],
            ["citizen"]);

        var plan = DiffEngine.BuildPlan(desired, current);

        Assert.False(plan.HasChanges);
    }

    [Fact]
    public void BuildPlan_UpdatesClient_WhenAudienceDiffers()
    {
        var desired = new DesiredState(
            "registry",
            [new DesiredClient("web", ["https://registry.local/signin-oidc"], ["openid"], ["registry-api"])],
            []);
        var current = new CurrentState(
            [new CurrentClient("web", "1", ["https://registry.local/signin-oidc"], ["openid"], [])],
            []);

        var plan = DiffEngine.BuildPlan(desired, current);

        Assert.Single(plan.ClientsToUpdate);
        Assert.Equal(["registry-api"], plan.ClientsToUpdate[0].Audiences);
    }
}
