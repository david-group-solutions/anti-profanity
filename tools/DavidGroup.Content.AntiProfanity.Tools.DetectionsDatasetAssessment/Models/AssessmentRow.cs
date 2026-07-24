using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Enums;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Models;

public class AssessmentRow : DetectionRow
{
    public AssessmentAnswer Answer { get; init; }
}
