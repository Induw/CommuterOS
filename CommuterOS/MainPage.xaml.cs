using System.ComponentModel;
using CommuterOS.Services;
using CommuterOS.Models;

namespace CommuterOS;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    // --- VERIFIED IDS ---
    // Sigma (Upplands Väsby kn)
    private const string SITE_SIGMA = "740066762";    
    // Stockholm Sveaplan
    private const string SITE_SVEAPLAN = "740046037"; 

    private readonly ResRobotService _service;
    private readonly IDispatcherTimer _timer;
    
    // State
    private bool _isWorkMode = true; // Default: Going to work
    private DateTime _targetTime = DateTime.MinValue;
    private bool _isLoading = false;

    // UI Properties
    public string CurrentTimeString { get; set; } = "SYSTEM READY";
    public string TimeToLeaveString { get; set; } = "--:--:--";
    public string ModeText { get; set; } = "TARGET: APOTEA";
    public string SuggestionText { get; set; } = "PRESS REFRESH";
    public Color TimerColor { get; set; } = Color.FromArgb("#FFB000");

    public string RouteStep1 { get; set; } = "";
    public string RouteStep2 { get; set; } = "";
    public string RouteStep3 { get; set; } = "";

    public MainPage(ResRobotService service)
    {
        InitializeComponent();
        BindingContext = this;
        _service = service;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();

        UpdateStaticUI();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        if (_isLoading) return;
        _isLoading = true;

        SuggestionText = "UPLOADING...";
        OnPropertyChanged(nameof(SuggestionText));

        try
        {
            string from = _isWorkMode ? SITE_SIGMA : SITE_SVEAPLAN;
            string to = _isWorkMode ? SITE_SVEAPLAN : SITE_SIGMA;

            var trip = await _service.GetNextTripAsync(from, to);
            
            if (trip != null) ProcessTrip(trip);
            else 
            {
                SuggestionText = "CONNECTION ERROR";
                OnPropertyChanged(nameof(SuggestionText));
            }
        }
        catch 
        {
            SuggestionText = "SYSTEM FAILURE";
            OnPropertyChanged(nameof(SuggestionText));
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ProcessTrip(Trip trip)
    {
        var departure = trip.GetDepartureTime();

        // --- CUSTOM LOGIC ---
        if (_isWorkMode)
        {
            // MORNING: Bus leaves Sigma. You walk 2 mins.
            // Target = Bus Departure - 2 mins.
            _targetTime = departure.AddMinutes(-2);
            SuggestionText = $"BUS DEPARTS {departure:HH:mm} (WALK 2 MIN)";
        }
        else
        {
            // EVENING: Leave work now. 
            // The API handles the connections.
            _targetTime = departure;
            SuggestionText = $"DEPART WORK AT {departure:HH:mm}";
        }

        // Format Steps
        var legs = trip.LegList.Legs;
        RouteStep1 = legs.Count > 0 ? FormatLeg(legs[0]) : "";
        RouteStep2 = legs.Count > 1 ? FormatLeg(legs[1]) : "";
        RouteStep3 = legs.Count > 2 ? FormatLeg(legs[2]) : "";

        OnPropertyChanged(nameof(RouteStep1));
        OnPropertyChanged(nameof(RouteStep2));
        OnPropertyChanged(nameof(RouteStep3));
        OnPropertyChanged(nameof(SuggestionText));
    }

    private void OnSwitchModeClicked(object sender, EventArgs e)
    {
        _isWorkMode = !_isWorkMode;
        _targetTime = DateTime.MinValue;
        TimeToLeaveString = "--:--:--";
        OnPropertyChanged(nameof(TimeToLeaveString));
        UpdateStaticUI();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        CurrentTimeString = DateTime.Now.ToString("HH:mm:ss").ToUpper();
        OnPropertyChanged(nameof(CurrentTimeString));

        if (_targetTime != DateTime.MinValue)
        {
            var diff = _targetTime - DateTime.Now;

            if (diff.TotalSeconds < 0)
            {
                TimeToLeaveString = "[ DEPART NOW ]";
                TimerColor = Colors.Red;
            }
            else
            {
                TimeToLeaveString = $"[ {diff.Hours:D2} : {diff.Minutes:D2} : {diff.Seconds:D2} ]";
                TimerColor = Color.FromArgb("#FFB000");
            }
            
            OnPropertyChanged(nameof(TimeToLeaveString));
            OnPropertyChanged(nameof(TimerColor));
        }
    }

    private void UpdateStaticUI()
    {
        ModeText = _isWorkMode ? "TARGET: APOTEA" : "TARGET: SIGMA";
        OnPropertyChanged(nameof(ModeText));
    }

    private string FormatLeg(Leg leg)
    {
        string name = leg.Name.ToUpper()
            .Replace("LÄNSTRAFIK - BUSS", "BUS")
            .Replace("BUSS", "BUS")
            .Replace("PENDELTÅG", "TRAIN");
            
        return $"> {name} -> {leg.Destination.Name.ToUpper()}";
    }
}
