using DotnetQuest.Api.Domain;

namespace DotnetQuest.Api.Features.Sessions;

/// <summary>Entity → DTO dönüşümü. Doğru cevaplar sadece soru cevaplandıktan sonra eklenir.</summary>
internal static class SessionMapper
{
    public static SessionResponse ToResponse(GameSession session) => new(
        session.Id,
        session.CategoryId,
        session.Category.Title,
        session.Category.Icon,
        session.Difficulty,
        session.Score,
        session.MaxScore,
        session.IsCompleted,
        session.Questions.OrderBy(q => q.Order).Select(q => ToQuestion(session.Id, q)).ToList());

    public static AnswerFeedback ToFeedback(Guid sessionId, SessionQuestion sessionQuestion)
    {
        var question = sessionQuestion.Question;

        int? correctOption = null;
        if (question.Type == QuestionType.MultipleChoice)
        {
            var permutation = OptionShuffler.GetPermutation(sessionId, question.Id, question.Options.Count, avoidOriginalOrder: false);
            correctOption = Array.IndexOf(permutation, question.CorrectOption!.Value);
        }

        return new AnswerFeedback(
            sessionQuestion.IsCorrect == true,
            sessionQuestion.PointsEarned,
            question.Explanation,
            correctOption,
            question.Type == QuestionType.BugHunt ? question.CorrectLine : null,
            question.Type == QuestionType.Ordering ? question.Options : null);
    }

    private static SessionQuestionResponse ToQuestion(Guid sessionId, SessionQuestion sessionQuestion)
    {
        var question = sessionQuestion.Question;

        var options = question.Type switch
        {
            QuestionType.MultipleChoice or QuestionType.Ordering => OptionShuffler.Apply(
                question.Options,
                OptionShuffler.GetPermutation(
                    sessionId,
                    question.Id,
                    question.Options.Count,
                    avoidOriginalOrder: question.Type == QuestionType.Ordering)),
            _ => [],
        };

        return new SessionQuestionResponse(
            question.Id,
            sessionQuestion.Order,
            question.Type,
            question.Prompt,
            question.Code,
            options,
            sessionQuestion.IsAnswered ? ToFeedback(sessionId, sessionQuestion) : null);
    }
}
