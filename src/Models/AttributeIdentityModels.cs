using System.Collections.Generic;

namespace Gaffer.Models;

public record RoleFitScore(string RoleName, string Phase, double Score);

public record AttributeIdentityResult(
    int PlayerId,
    string PlayerName,
    string RegisteredPosition,
    string SuggestedPrimaryRole,
    string SuggestedSecondaryRole,
    bool IsMisaligned,
    IReadOnlyList<RoleFitScore> TopFits,
    string Summary);
