using System.Reflection;
using Application.Interfaces.Services;

namespace ArchitectureTests.Ai;

public class AiProjectionSurfaceTests
{
    private const string _approvedFileName = "AiProjectionSurface.approved.txt";

    private static readonly Assembly _applicationAssembly = typeof(IFaqAssistantService).Assembly;

    private static IReadOnlyList<Type> Projections() =>
        AiProjectionRules.ProductionProjections(_applicationAssembly);

    [Fact]
    public void Exposure_surface_matches_the_approved_file()
    {
        var actual = AiProjectionRules.DescribeSurface(Projections());
        var approved = ReadApproved();

        if (actual.SequenceEqual(approved, StringComparer.Ordinal))
        {
            return;
        }

        Assert.Fail(
            $"""
             The AI exposure surface changed.

             {AiProjectionRules.Diff(approved, actual)}
             Every line here is a field the language model can read. If each addition above is
             intentional and contains no user identity (EPIC 3 §6), update:

               tests/ArchitectureTests/Ai/{_approvedFileName}

             The updated file is the review artifact — a reviewer should be able to approve or reject
             this change by reading that diff alone.
             """);
    }

    [Fact]
    public void Approved_file_and_namespace_agree_on_emptiness()
    {
        var approved = ReadApproved();
        var projections = Projections();

        if (approved.Count > 0)
        {
            Assert.False(
                projections.Count == 0,
                $"{_approvedFileName} lists {approved.Count} approved field(s) but "
                + $"'{AiProjectionRules.ProjectionNamespace}' contains no types. Either the namespace was "
                + "removed or renamed — which would make every exposure guard silently vacuous — or the "
                + "approved file is stale.");
        }
    }

    [Fact]
    public void Projections_are_sealed_records_with_no_base_type()
    {
        var violations = AiProjectionRules.SealedRecordViolations(Projections())
            .Concat(AiProjectionRules.BaseTypeViolations(Projections()))
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"""
             AI projections must be sealed records deriving only from object.

             {AiProjectionRules.Format(violations)}
             """);
    }

    [Fact]
    public void Projections_expose_no_user_identity()
    {
        var violations = AiProjectionRules.ForbiddenNameViolations(Projections());

        Assert.True(
            violations.Count == 0,
            $"""
             A forbidden property reached an AI projection. The model must receive no user names, emails,
             avatars, or user identifiers (EPIC 3 Decision 5) — assignment is expressed as
             IsAssignedToMe, resolved server-side.

             {AiProjectionRules.Format(violations)}
             """);
    }

    [Fact]
    public void Role_properties_say_whose_role_they_are()
    {
        var violations = AiProjectionRules.RoleNamingViolations(Projections());

        Assert.True(
            violations.Count == 0,
            $"""
             A role property is ambiguous about whose role it holds, which reads as innocent in a
             manifest diff (EPIC 3 Decision 14).

             {AiProjectionRules.Format(violations)}
             """);
    }

    [Fact]
    public void Projections_expose_no_domain_entities()
    {
        var violations = AiProjectionRules.EntityLeakViolations(Projections());

        Assert.True(
            violations.Count == 0,
            $"""
             A domain entity reached an AI projection. Entities carry BaseEntity audit columns
             (CreatedById, UpdatedById, DeletedById) and navigation properties to User.

             {AiProjectionRules.Format(violations)}
             """);
    }

    [Fact]
    public void Application_does_not_reference_a_mapping_library()
    {
        var violations = AiProjectionRules.MapperReferenceViolations(_applicationAssembly);

        Assert.True(
            violations.Count == 0,
            $"""
             Convention-based mapping copies every matching property, which is exactly how an audit
             column reaches an AI projection unnoticed. Project explicitly instead.

             {AiProjectionRules.Format(violations)}
             """);
    }

    private static IReadOnlyList<string> ReadApproved()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Ai", _approvedFileName);

        // .gitignore has a repo-wide *.txt rule; !**/*.approved.txt is what keeps this file committable.
        Assert.True(
            File.Exists(path),
            $"Approved file not found at '{path}'. Either it is missing from the repository — check that "
            + "the .gitignore negation for *.approved.txt is intact — or it is not being copied to the "
            + "output directory by ArchitectureTests.csproj.");

        return
        [
            .. File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
        ];
    }
}
