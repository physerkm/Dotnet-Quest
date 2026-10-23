using System.ComponentModel.DataAnnotations;
using DotnetQuest.Api.Domain;

namespace DotnetQuest.Api.Features.Sessions;

// İstek DTO'ları: sadece istemcinin göndermesine izin verdiğimiz alanlar (over-posting koruması).

public sealed record CreateSessionRequest(
    [Required, StringLength(32)] string CategoryId,
    [EnumDataType(typeof(Difficulty))] Difficulty Difficulty);

/// <param name="QuestionId">Cevaplanan sorunun id'si.</param>
/// <param name="SelectedOption">MultipleChoice: seçilen şıkkın (gösterilen sıradaki) 0 tabanlı indeksi.</param>
/// <param name="SelectedLine">BugHunt: seçilen satır (1 tabanlı).</param>
/// <param name="Ordering">Ordering: öğelerin oyuncunun dizdiği sırası.</param>
public sealed record SubmitAnswerRequest(
    [Range(1, int.MaxValue)] int QuestionId,
    int? SelectedOption,
    int? SelectedLine,
    [MaxLength(20)] IReadOnlyList<string>? Ordering);

// Yanıt DTO'ları: doğru cevap, soru cevaplanmadan ASLA istemciye gönderilmez.

public sealed record SessionResponse(
    Guid Id,
    string CategoryId,
    string CategoryTitle,
    string CategoryIcon,
    Difficulty Difficulty,
    int Score,
    int MaxScore,
    bool IsCompleted,
    IReadOnlyList<SessionQuestionResponse> Questions);

public sealed record SessionQuestionResponse(
    int Id,
    int Order,
    QuestionType Type,
    string Prompt,
    string? Code,
    IReadOnlyList<string> Options,
    AnswerFeedback? Feedback);

public sealed record AnswerFeedback(
    bool IsCorrect,
    int PointsEarned,
    string Explanation,
    int? CorrectOption,
    int? CorrectLine,
    IReadOnlyList<string>? CorrectOrder);

/// <param name="XpEarned">Tur tamamlandıysa ve oyuncu giriş yaptıysa kazanılan XP.</param>
public sealed record AnswerResponse(
    AnswerFeedback Feedback,
    int SessionScore,
    int MaxScore,
    bool SessionCompleted,
    bool IsPerfect,
    int? XpEarned);
