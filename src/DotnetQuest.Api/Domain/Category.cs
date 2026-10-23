namespace DotnetQuest.Api.Domain;

public sealed class Category
{
    public required string Id { get; init; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Icon { get; set; }
    public int SortOrder { get; set; }

    public List<Question> Questions { get; } = [];
}
