using System.ComponentModel;
using CommuterOS.Services;
using CommuterOS.Models;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace CommuterOS;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private const string SITE_SIGMA = "740066762";    
    private const string SITE_SVEAPLAN = "740046037"; 
	private const string SITE_NORTULL = "740046132";

    private readonly ResRobotService _service;
    private readonly IDispatcherTimer _timer;
    
    // State
    private bool _isGoToWorkMode = true; 
	public bool isComfort = true;
    private DateTime _targetTime = DateTime.MinValue;
    private bool _isLoading = false;

    // UI 
    public string CurrentTimeString { get; set; } = "SYSTEM READY";
    public string TimeToLeaveString { get; set; } = "--:--:--";
    public string ModeText { get; set; } = "TARGET: WORK";
    public string SuggestionText { get; set; } = "PRESS REFRESH";
	public string PriorityButtonText { get; set; } = "[MODE: COMFORT]";
	public Color TimerColor { get; set; } = Color.FromArgb("#FFB000");
    public ObservableCollection<String> RouteSteps {get; set;} = [];


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
		await FetchTripAsync();
    }

	private async Task FetchTripAsync()
    {
        if (_isLoading) return;
        _isLoading = true;

        SuggestionText = "FETCHING...";
        OnPropertyChanged(nameof(SuggestionText));

        try
        {
            string from = _isGoToWorkMode ? SITE_SIGMA : SITE_SVEAPLAN;
            string to = _isGoToWorkMode ? SITE_NORTULL : SITE_SIGMA;

            var trip = await _service.GetNextTripAsync(from, to, isComfort);
            
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
        var departureTime = trip.GetDepartureTime();

        var legs = trip.LegList.Legs;

        var firstLeg = legs.First();

        if (_isGoToWorkMode)
        {
            if (firstLeg.IsWalk())
            {
                _targetTime = departureTime.AddMinutes(-2);
                SuggestionText = $"LEAVE HOME AT {departureTime:HH:mm}\n(2 MIN BUFFER)"; 
            }
            else
            {
                _targetTime = departureTime.AddMinutes(-4);
                SuggestionText = $"BUS LEAVES AT {departureTime:HH:mm}\n(WALK 4 MINS TO BUS STOP)";
            }
            
        }
        else
        {
            if (firstLeg.IsWalk())
            {
                _targetTime = departureTime.AddMinutes(-2);
                SuggestionText = $"LEAVE WORK AT {departureTime:HH:mm}\n(2 MIN BUFFER)"; 
            }
            else
            {
               _targetTime = departureTime.AddMinutes(-6);
                SuggestionText = $"BUS LEAVES AT {departureTime:HH:mm}\n(WALK 6 MINS TO BUS STOP)"; 
            }
            
        }


        RouteSteps.Clear();
        if (legs != null)
        {
            for (int i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                RouteSteps.Add(FormatLeg(leg));

                if (i < legs.Count - 1)
                {
                    var nextLeg = legs[i + 1];

                    if (DateTime.TryParse($"{leg.Destination.Date} {leg.Destination.Time}", out var arrivalTime) &&
                        DateTime.TryParse($"{nextLeg.Origin.Date} {nextLeg.Origin.Time}", out var nextLegDepartureTime))
                    {
                        var waitTime = nextLegDepartureTime - arrivalTime;

                        if (waitTime.TotalMinutes > 0)
                        {
                            RouteSteps.Add($"> WAIT {(int)waitTime.TotalMinutes} MIN");
                        }
                    }
                }
            }
        }

        OnPropertyChanged(nameof(SuggestionText));
    }

    private async void OnSwitchModeClicked(object sender, EventArgs e)
    {
        _isGoToWorkMode = !_isGoToWorkMode;
        _targetTime = DateTime.MinValue;
        TimeToLeaveString = "--:--:--";
        OnPropertyChanged(nameof(TimeToLeaveString));
        UpdateStaticUI();
        await FetchTripAsync();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        CurrentTimeString = DateTime.Now.ToString("HH:mm:ss").ToUpper();
        OnPropertyChanged(nameof(CurrentTimeString));

        if (_targetTime != DateTime.MinValue)
        {
            var diff = _targetTime - DateTime.Now;

            if (diff.TotalSeconds > 0)
            {                                                                                                                                                                                                  
                TimeToLeaveString = $"[ {diff.Hours:D2} : {diff.Minutes:D2} : {diff.Seconds:D2} ]";                                                                                                                  
                TimerColor = Color.FromArgb("#FFB000");                            
            }  
            else if (diff.TotalSeconds > -150)
            {
                TimeToLeaveString = "[ DEPART NOW ]";
                TimerColor = Colors.Red;
            }
            else
            {
                TimeToLeaveString = $"[ {diff.Hours:D2} : {diff.Minutes:D2} : {diff.Seconds:D2} ]";
                TimerColor = Colors.Grey;
                TimeToLeaveString = "[ MISSED WINDOW ]";
            }
            
            OnPropertyChanged(nameof(TimeToLeaveString));
            OnPropertyChanged(nameof(TimerColor));
        }
    }

    private void UpdateStaticUI()
    {
        ModeText = _isGoToWorkMode ? "[ TARGET : WORK ]" : "[ TARGET : HOME ]";
		PriorityButtonText = isComfort ? "[MODE: COMFORT]" : "[MODE: FAST]";
        OnPropertyChanged(nameof(ModeText));
		OnPropertyChanged(nameof(PriorityButtonText));
    }

	private async void OnPriorityClicked(object sender, EventArgs e)
    {
        isComfort = ! isComfort;
        UpdateStaticUI();
        await FetchTripAsync();
    }


    private string FormatLeg(Leg leg)
    {
        string time = leg.Origin.Time.Length >= 5 ? leg.Origin.Time.Substring(0, 5) : leg.Origin.Time;
        string name = leg.Name.ToUpper()
            .Replace("LÄNSTRAFIK - BUSS", "BUS")
			.Replace ("LÄNSTRAFIK - TÅG", "TRAIN")
			.Replace("PROMENAD", "WALK");
            
        return $"[{time}] {name} -> {leg.Destination.Name.ToUpper()}";
    }
}
