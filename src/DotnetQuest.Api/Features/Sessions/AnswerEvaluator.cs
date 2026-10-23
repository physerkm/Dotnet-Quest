using DotnetQuest.Api.Domain;

namespace DotnetQuest.Api.Features.Sessions;

public sealed record AnswerCheck(bool IsCorrect, string? ErrorField = null, string? ErrorMessage = null)
{
    public bool IsValid => ErrorField is null;

    public static AnswerCheck Invalid(string field, string message) => new(false, field, message);
}

/// <summary>Cevabın doğruluğunu sunucuda kontrol eder. İstemciye asla güvenilmez.</summary>
public static class AnswerEvaluator
{
    public static AnswerCheck Evaluate(Question question, Guid sessionId, SubmitAnswerRequest request) => question.Type switch
    {
        QuestionType.MultipleChoice => EvaluateMultipleChoice(question, sessionId, request.SelectedOption),
        QuestionType.BugHunt => EvaluateBugHunt(question, request.SelectedLine),
        QuestionType.Ordering => EvaluateOrdering(question, request.Ordering),
        _ => throw new InvalidOperationException($"Bilinmeyen soru tipi: {question.Type}"),
    };

    private static AnswerCheck EvaluateMultipleChoice(Question question, Guid sessionId, int? selected)
    {
        if (selected is not { } displayedIndex || displayedIndex < 0 || displayedIndex >= question.Options.Count)
        {
            return AnswerCheck.Invalid("selectedOption", "Geçerli bir şık seçmelisin.");
        }

        var permutation = OptionShuffler.GetPermutation(sessionId, question.Id, question.Options.Count, avoidOriginalOrder: false);
        return new AnswerCheck(permutation[displayedIndex] == question.CorrectOption);
    }

    private static AnswerCheck EvaluateBugHunt(Question question, int? selectedLine)
    {
        if (selectedLine is not { } line || line < 1 || line > question.CodeLineCount)
        {
            return AnswerCheck.Invalid("selectedLine", "Koddaki satırlardan birini seçmelisin.");
        }

        return new AnswerCheck(line == question.CorrectLine);
    }

    private static AnswerCheck EvaluateOrdering(Question question, IReadOnlyList<string>? ordering)
    {
        if (ordering is null
            || ordering.Count != question.Options.Count
            || !ordering.Order(StringComparer.Ordinal).SequenceEqual(question.Options.Order(StringComparer.Ordinal), StringComparer.Ordinal))
        {
            return AnswerCheck.Invalid("ordering", "Tüm öğeleri bir kez kullanarak sıralamalısın.");
        }

        return new AnswerCheck(ordering.SequenceEqual(question.Options, StringComparer.Ordinal));
    }
}
