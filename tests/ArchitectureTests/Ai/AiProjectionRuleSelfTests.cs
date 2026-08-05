using ArchitectureTests.Ai.Fixtures;

namespace ArchitectureTests.Ai;

public class AiProjectionRuleSelfTests
{
    private static Type[] Only<T>() => [typeof(T)];

    [Fact]
    public void Clean_projection_violates_nothing()
    {
        var clean = Only<RuleFixtures.GoodBoardSummary>();

        Assert.Empty(AiProjectionRules.SealedRecordViolations(clean));
        Assert.Empty(AiProjectionRules.BaseTypeViolations(clean));
        Assert.Empty(AiProjectionRules.ForbiddenNameViolations(clean));
        Assert.Empty(AiProjectionRules.RoleNamingViolations(clean));
        Assert.Empty(AiProjectionRules.EntityLeakViolations(clean));
    }

    [Theory]
    [InlineData(typeof(RuleFixtures.LeaksAuditColumn), "CreatedById")]
    [InlineData(typeof(RuleFixtures.LeaksAssignee), "AssigneeId")]
    [InlineData(typeof(RuleFixtures.LeaksDisplayName), "DisplayName")]
    [InlineData(typeof(RuleFixtures.LeaksDescription), "Description")]
    public void Forbidden_property_is_rejected(Type projection, string expectedProperty)
    {
        var violations = AiProjectionRules.ForbiddenNameViolations([projection]);

        Assert.Contains(violations, violation => violation.Contains(expectedProperty, StringComparison.Ordinal));
    }

    [Fact]
    public void Assignee_id_is_caught_even_though_it_does_not_end_in_ById()
    {
        // The reason the rule is a denylist and not just a "*ById" suffix check.
        Assert.False("AssigneeId".EndsWith("ById", StringComparison.Ordinal));
        Assert.NotEmpty(AiProjectionRules.ForbiddenNameViolations(Only<RuleFixtures.LeaksAssignee>()));
    }

    [Fact]
    public void Role_without_My_prefix_is_rejected()
    {
        var violations = AiProjectionRules.RoleNamingViolations(Only<RuleFixtures.AmbiguousRole>());

        Assert.Contains(violations, violation => violation.Contains("must be named My*", StringComparison.Ordinal));
    }

    [Fact]
    public void Role_named_My_is_accepted() =>
        Assert.Empty(AiProjectionRules.RoleNamingViolations(Only<RuleFixtures.GoodBoardSummary>()));

    [Theory]
    [InlineData(typeof(RuleFixtures.ExposesEntity))]
    [InlineData(typeof(RuleFixtures.ExposesEntityCollection))]
    public void Domain_entity_as_a_property_type_is_rejected(Type projection)
    {
        var violations = AiProjectionRules.EntityLeakViolations([projection]);

        Assert.Contains(violations, violation => violation.Contains("TaskItem", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(typeof(RuleFixtures.NotSealed))]
    [InlineData(typeof(RuleFixtures.NotARecord))]
    public void Non_sealed_record_is_rejected(Type projection) =>
        Assert.NotEmpty(AiProjectionRules.SealedRecordViolations([projection]));

    [Fact]
    public void Inherited_base_type_is_rejected()
    {
        var violations = AiProjectionRules.BaseTypeViolations(Only<RuleFixtures.InheritsAudit>());

        Assert.Contains(violations, violation => violation.Contains("AuditedBase", StringComparison.Ordinal));
    }

    [Fact]
    public void Surface_is_rendered_sorted_and_stable()
    {
        var surface = AiProjectionRules.DescribeSurface(Only<RuleFixtures.GoodBoardSummary>());

        Assert.Equal(
            [
                "GoodBoardSummary.Id : Guid",
                "GoodBoardSummary.IsArchived : bool",
                "GoodBoardSummary.MyBoardRole : BoardRoleDto?",
                "GoodBoardSummary.Name : string",
                "GoodBoardSummary.TaskCount : int"
            ],
            surface);
    }

    [Fact]
    public void Diff_reports_added_and_removed_lines()
    {
        var diff = AiProjectionRules.Diff(
            approved: ["A.Keep : string", "A.Gone : int"],
            actual: ["A.Keep : string", "A.New : Guid"]);

        Assert.Contains("+ A.New : Guid", diff, StringComparison.Ordinal);
        Assert.Contains("- A.Gone : int", diff, StringComparison.Ordinal);
        Assert.DoesNotContain("A.Keep", diff, StringComparison.Ordinal);
    }
}
