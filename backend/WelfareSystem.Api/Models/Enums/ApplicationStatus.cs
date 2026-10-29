namespace WelfareSystem.Api.Models.Enums;

public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    UnderHRReview = 3,
    HRRecommended = 4,
    CommitteeReview = 5,
    CommitteeRecommended = 6,
    PendingManagementApproval = 7,
    Approved = 8,
    Rejected = 9,
    RequiresAdditionalInfo = 10,
    FlaggedForReview = 11,
    Cancelled = 12
}
