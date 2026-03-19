using JobRecon.Domain.Common;
using JobRecon.Profile.Contracts;

namespace JobRecon.Profile.Services;

public interface IProfileService
{
    Task<Result<ProfileResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ProfileResponse>> CreateProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<Result<SkillResponse>> AddSkillAsync(Guid userId, AddSkillRequest request, CancellationToken cancellationToken = default);
    Task<Result> RemoveSkillAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);

    Task<Result<JobPreferenceResponse>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<JobPreferenceResponse>> UpdatePreferencesAsync(Guid userId, UpdateJobPreferenceRequest request, CancellationToken cancellationToken = default);

    Task<Result<CVDocumentResponse>> UploadCVAsync(Guid userId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Result<(Stream FileStream, string FileName, string ContentType)>> DownloadCVAsync(Guid userId, Guid documentId, CancellationToken cancellationToken = default);
    Task<Result> DeleteCVAsync(Guid userId, Guid documentId, CancellationToken cancellationToken = default);
    Task<Result> SetPrimaryCVAsync(Guid userId, Guid documentId, CancellationToken cancellationToken = default);
}
