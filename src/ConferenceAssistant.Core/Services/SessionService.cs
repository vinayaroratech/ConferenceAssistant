using System.Text.Json;
using ConferenceAssistant.Core.Domain;
using ConferenceAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace ConferenceAssistant.Core.Services;

public class SessionService : ISessionService
{
    private readonly ILogger<SessionService> _logger;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly InMemoryStore<SessionTopic> _topics = new();
    private readonly InMemoryStore<Insight> _insights = new();
    private readonly InMemoryStore<AudienceQuestion> _questions = new();
    private List<Slide> _slides = [];
    private int _activeSlideIndex = -1;

    private string _currentTopicId = "";
    public string SessionCode { get; set; } = "AICONF";
    public string SessionName { get; set; } = "AI in .NET - The Living Presentation";

    public event Action<Slide>? SlideChanged;
    public event Action<SessionTopic>? TopicChanged;
    public event Action? QuestionsChanged;
    public event Action? InsightsChanged;
    public event Action? SessionEnded;

    /// <summary>UTC time the session became active (first topic loaded).</summary>
    public DateTimeOffset? SessionStartedAt { get; private set; }

    /// <summary>True once EndSession() has been called - locks voting and questions.</summary>
    public bool IsEnded { get; private set; }

    public SessionService(ILogger<SessionService> logger, DomainEventDispatcher dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public Slide? ActiveSlide => _activeSlideIndex >= 0 && _activeSlideIndex < _slides.Count
        ? _slides[_activeSlideIndex] : null;
    public int ActiveSlideIndex => _activeSlideIndex;
    public int TotalSlides => _slides.Count;

    public async Task SwitchConferenceAsync(string topicsPath, string slidesPath, string code, string name, string? sessionId = null)
    {
        _logger.LogInformation("Switching conference to {Code}", code);
        SessionCode = code;
        SessionName = name;
        IsEnded = false;
        SessionStartedAt = null;
        _currentTopicId = "";
        _activeSlideIndex = -1;
        _slides = [];
        _topics.Clear();
        _insights.Clear();
        _questions.Clear();

        await LoadTopicsAsync(topicsPath, sessionId);
        await LoadSlidesAsync(slidesPath);

        var current = GetCurrentTopic();
        if (current is not null) TopicChanged?.Invoke(current);
        if (ActiveSlide is not null) SlideChanged?.Invoke(ActiveSlide);
    }

    public async Task LoadTopicsAsync(string jsonPath, string? sessionId = null)
    {
        var json = await File.ReadAllTextAsync(jsonPath);

        List<SessionTopic>? topics = null;

        var jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        using var doc = System.Text.Json.JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var firstElem = doc.RootElement.EnumerateArray().FirstOrDefault();
            bool isMultiSession = firstElem.ValueKind == System.Text.Json.JsonValueKind.Object
                && firstElem.TryGetProperty("sessionId", out _);

            if (isMultiSession)
            {
                // Multi-session array: [{sessionId, topics:[...]}, ...]
                JsonElement sessionElem = default;
                bool found = false;
                foreach (var elem in doc.RootElement.EnumerateArray())
                {
                    if (elem.TryGetProperty("sessionId", out var sid) &&
                        (sessionId is null || sid.GetString()?.Equals(sessionId, StringComparison.OrdinalIgnoreCase) == true))
                    {
                        sessionElem = elem;
                        found = true;
                        break;
                    }
                }
                if (!found) sessionElem = doc.RootElement.EnumerateArray().First();

                if (sessionElem.TryGetProperty("topics", out var topicsElem))
                    topics = System.Text.Json.JsonSerializer.Deserialize<List<SessionTopic>>(topicsElem.GetRawText(), jsonOpts);
            }
            else
            {
                // Legacy: flat array of topic objects
                topics = System.Text.Json.JsonSerializer.Deserialize<List<SessionTopic>>(json, jsonOpts);
            }
        }
        else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                 && doc.RootElement.TryGetProperty("topics", out var singleTopicsElem))
        {
            // Single-session wrapper object: {sessionId, title, topics:[...]}
            topics = System.Text.Json.JsonSerializer.Deserialize<List<SessionTopic>>(singleTopicsElem.GetRawText(), jsonOpts);
        }

        topics ??= [];

        foreach (var topic in topics.OrderBy(t => t.Order))
            _topics.Add(topic.Id, topic);

        if (topics.Count > 0)
        {
            _currentTopicId = topics.OrderBy(t => t.Order).First().Id;
            var first = _topics.Get(_currentTopicId);
            if (first is not null)
            {
                first.Activate();
                _dispatcher.DispatchAndClear(first);
            }
            SessionStartedAt ??= DateTimeOffset.UtcNow;
        }

        _logger.LogInformation("SessionService: loaded {Count} topics from {Path}", topics.Count, jsonPath);
    }

    public async Task LoadSlidesAsync(string slidesPath)
    {
        _slides = await SlideMarkdownParser.ParseFileAsync(slidesPath);
        foreach (var topic in _topics.GetAll())
        {
            topic.SetSlides(_slides.Where(s => s.TopicId == topic.Id));
        }
        if (_slides.Count > 0)
        {
            _activeSlideIndex = 0;
            SlideChanged?.Invoke(_slides[0]);
        }
        _logger.LogInformation("SessionService: loaded {Count} slides from {Path}", _slides.Count, slidesPath);
    }

    public SessionTopic? GetCurrentTopic()
        => string.IsNullOrEmpty(_currentTopicId) ? null : _topics.Get(_currentTopicId);

    public IReadOnlyList<SessionTopic> GetAllTopics()
        => _topics.GetAll().OrderBy(t => t.Order).ToList();

    public SessionTopic? AdvanceToNextTopic()
    {
        var topics = GetAllTopics();
        var currentIndex = topics.ToList().FindIndex(t => t.Id == _currentTopicId);

        if (currentIndex >= 0 && currentIndex < topics.Count - 1)
        {
            var current = _topics.Get(_currentTopicId);
            if (current is not null)
            {
                current.Complete();
                _dispatcher.DispatchAndClear(current);
            }

            _currentTopicId = topics[currentIndex + 1].Id;
            var next = _topics.Get(_currentTopicId);
            if (next is not null)
            {
                next.Activate();
                _dispatcher.DispatchAndClear(next);
            }

            // Jump to the first slide belonging to the new topic
            var firstSlide = _slides.FirstOrDefault(s => s.TopicId == _currentTopicId);
            if (firstSlide is not null)
            {
                _activeSlideIndex = _slides.IndexOf(firstSlide);
                SlideChanged?.Invoke(firstSlide);
            }

            if (next is not null) TopicChanged?.Invoke(next);
            return next;
        }
        return null;
    }

    public bool CanAdvanceTopic()
    {
        var topics = GetAllTopics();
        var idx = topics.ToList().FindIndex(t => t.Id == _currentTopicId);
        return idx >= 0 && idx < topics.Count - 1;
    }

    public bool CanAdvanceSlide() => _activeSlideIndex < _slides.Count - 1;
    public bool CanGoBackSlide() => _activeSlideIndex > 0;

    public Task AdvanceSlideAsync()
    {
        if (_activeSlideIndex < _slides.Count - 1)
        {
            _activeSlideIndex++;
            var slide = _slides[_activeSlideIndex];
            SyncTopicFromSlide(slide);
            SlideChanged?.Invoke(slide);
        }
        return Task.CompletedTask;
    }

    public Task GoBackSlideAsync()
    {
        if (_activeSlideIndex > 0)
        {
            _activeSlideIndex--;
            var slide = _slides[_activeSlideIndex];
            SyncTopicFromSlide(slide);
            SlideChanged?.Invoke(slide);
        }
        return Task.CompletedTask;
    }

    public Task GoToSlideAsync(string slideId)
    {
        var idx = _slides.FindIndex(s => s.Id == slideId);
        if (idx >= 0)
        {
            _activeSlideIndex = idx;
            var slide = _slides[_activeSlideIndex];
            SyncTopicFromSlide(slide);
            SlideChanged?.Invoke(slide);
        }
        return Task.CompletedTask;
    }

    private void SyncTopicFromSlide(Slide slide)
    {
        if (string.IsNullOrEmpty(slide.TopicId) || slide.TopicId == _currentTopicId) return;
        var prev = _topics.Get(_currentTopicId);
        if (prev is not null) { prev.Complete(); _dispatcher.DispatchAndClear(prev); }
        _currentTopicId = slide.TopicId;
        var next = _topics.Get(_currentTopicId);
        if (next is not null) { next.Activate(); _dispatcher.DispatchAndClear(next); }
        if (next is not null) TopicChanged?.Invoke(next);
    }

    public List<Slide> GetAllSlides() => _slides.ToList();

    public void EndSession()
    {
        IsEnded = true;
        // Mark any remaining active topic as completed
        foreach (var t in _topics.GetAll().Where(t => t.Status == TopicStatus.Active))
        {
            t.Complete();
            _dispatcher.DispatchAndClear(t);
        }
        // Dispatch domain event so SessionEndedHandler can auto-trigger the summary workflow
        _dispatcher.Dispatch(new Domain.Events.SessionEnded());
        SessionEnded?.Invoke();
    }

    public Slide? GetNextSlide() =>
        _activeSlideIndex + 1 < _slides.Count ? _slides[_activeSlideIndex + 1] : null;

    public Slide? GetPreviousSlide() =>
        _activeSlideIndex > 0 ? _slides[_activeSlideIndex - 1] : null;

    public List<Slide> GetSlidesForTopic(string topicId) =>
        _slides.Where(s => s.TopicId == topicId).ToList();

    public void AddInsight(Insight insight)
    {
        _insights.Add(insight.Id, insight);
        _dispatcher.DispatchAndClear(insight);
        InsightsChanged?.Invoke();
    }

    public IReadOnlyList<Insight> GetInsights()
        => _insights.GetAll().OrderByDescending(i => i.GeneratedAt).ToList();

    public IReadOnlyList<Insight> GetInsightsForPoll(string pollId)
        => _insights.Query(i => i.PollId == pollId);

    public AudienceQuestion AddQuestion(string text, string? attendeeId = null)
    {
        var question = AudienceQuestion.Submit(text, attendeeId);
        _questions.Add(question.Id, question);
        _dispatcher.DispatchAndClear(question);
        QuestionsChanged?.Invoke();
        return question;
    }

    public void ApproveQuestion(string questionId)
    {
        var q = _questions.Get(questionId);
        if (q is not null) { q.Approve(); _dispatcher.DispatchAndClear(q); QuestionsChanged?.Invoke(); }
    }

    public void RejectQuestion(string questionId)
    {
        var q = _questions.Get(questionId);
        if (q is not null) { q.Reject(); _dispatcher.DispatchAndClear(q); QuestionsChanged?.Invoke(); }
    }

    public void AnswerQuestion(string questionId, string answer)
    {
        var q = _questions.Get(questionId);
        if (q is not null) { q.SetAnswer(answer); _dispatcher.DispatchAndClear(q); }
    }

    public void UpvoteQuestion(string questionId)
    {
        var q = _questions.Get(questionId);
        if (q is not null) q.Upvote();
    }

    public IReadOnlyList<AudienceQuestion> GetQuestions()
        => _questions.GetAll().OrderByDescending(q => q.Upvotes).ToList();

    /// <summary>Only questions explicitly approved (IsApproved == true).</summary>
    public IReadOnlyList<AudienceQuestion> GetApprovedQuestions()
        => _questions.GetAll().Where(q => q.IsApproved == true).OrderByDescending(q => q.Upvotes).ToList();

    /// <summary>Questions still awaiting moderator decision (IsApproved == null).</summary>
    public IReadOnlyList<AudienceQuestion> GetPendingQuestions()
        => _questions.GetAll().Where(q => q.IsApproved == null).OrderBy(q => q.AskedAt).ToList();
}
