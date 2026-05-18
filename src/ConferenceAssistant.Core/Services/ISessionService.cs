using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Services;

public interface ISessionService
{
    // --- Session identity ---
    string SessionCode { get; set; }
    string SessionName { get; set; }
    bool IsEnded { get; }
    DateTimeOffset? SessionStartedAt { get; }

    // --- Slide navigation state ---
    Slide? ActiveSlide { get; }
    int ActiveSlideIndex { get; }
    int TotalSlides { get; }

    // --- UI change notifications ---
    event Action<Slide>? SlideChanged;
    event Action<SessionTopic>? TopicChanged;
    event Action? QuestionsChanged;
    event Action? InsightsChanged;
    event Action? SessionEnded;

    // --- Loading ---
    Task SwitchConferenceAsync(string topicsPath, string slidesPath, string code, string name, string? sessionId = null);
    Task LoadTopicsAsync(string jsonPath, string? sessionId = null);
    Task LoadSlidesAsync(string slidesPath);

    // --- Topics ---
    SessionTopic? GetCurrentTopic();
    IReadOnlyList<SessionTopic> GetAllTopics();
    SessionTopic? AdvanceToNextTopic();
    bool CanAdvanceTopic();

    // --- Slides ---
    bool CanAdvanceSlide();
    bool CanGoBackSlide();
    Task AdvanceSlideAsync();
    Task GoBackSlideAsync();
    Task GoToSlideAsync(string slideId);
    Slide? GetNextSlide();
    Slide? GetPreviousSlide();
    List<Slide> GetAllSlides();
    List<Slide> GetSlidesForTopic(string topicId);

    // --- Insights ---
    void AddInsight(Insight insight);
    IReadOnlyList<Insight> GetInsights();
    IReadOnlyList<Insight> GetInsightsForPoll(string pollId);

    // --- Audience questions ---
    AudienceQuestion AddQuestion(string text, string? attendeeId = null);
    void ApproveQuestion(string questionId);
    void RejectQuestion(string questionId);
    void AnswerQuestion(string questionId, string answer);
    void UpvoteQuestion(string questionId);
    IReadOnlyList<AudienceQuestion> GetQuestions();
    IReadOnlyList<AudienceQuestion> GetApprovedQuestions();
    IReadOnlyList<AudienceQuestion> GetPendingQuestions();

    // --- Session lifecycle ---
    void EndSession();
}
