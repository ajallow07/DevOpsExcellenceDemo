using HiringApi.Models;

namespace HiringApi.Validation;

public interface IRoleValidator
{
    (bool IsValid, string? ErrorMessage) ValidateCreateRequest(CreateRoleRequest request);
}

public class RoleValidator : IRoleValidator
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 5000;
    private const int MaxDepartmentLength = 100;
    private const int MaxLocationLength = 200;

    public (bool IsValid, string? ErrorMessage) ValidateCreateRequest(CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return (false, "Title is required");

        if (request.Title.Length > MaxTitleLength)
            return (false, $"Title cannot exceed {MaxTitleLength} characters");

        if (string.IsNullOrWhiteSpace(request.Description))
            return (false, "Description is required");

        if (request.Description.Length > MaxDescriptionLength)
            return (false, $"Description cannot exceed {MaxDescriptionLength} characters");

        if (!string.IsNullOrWhiteSpace(request.Department) && request.Department.Length > MaxDepartmentLength)
            return (false, $"Department cannot exceed {MaxDepartmentLength} characters");

        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Length > MaxLocationLength)
            return (false, $"Location cannot exceed {MaxLocationLength} characters");

        return (true, null);
    }
}
