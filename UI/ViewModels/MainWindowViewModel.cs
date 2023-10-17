using ReactiveUI;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ParserCore;
namespace UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{

    private Regex url_validate_pattern = new Regex("^[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b(?:[-a-zA-Z0-9()@:%_\\+.~#?&\\/=]*)$");

    private int _RPMLimit;
    private Parser parser;

    public int RPMLimit
    {
        get => _RPMLimit;
        set
        {
            if (parser != null)
                parser.RPM = value;
            this.RaiseAndSetIfChanged(ref _RPMLimit, value);
        }
    }


    private bool _IsProcessing;
    public bool IsProcessing
    {
        get => _IsProcessing;
        set => this.RaiseAndSetIfChanged(ref _IsProcessing, value);
    }


    private Parser.ParserStatus _ParserStatusDescr;

    public Parser.ParserStatus ParserStatus
    {
        get => _ParserStatusDescr;
        set => this.RaiseAndSetIfChanged(ref _ParserStatusDescr, value);
    }


    private bool _ReadyToRun;
    public bool ReadyToRun
    {
        get => _ReadyToRun;
        set => this.RaiseAndSetIfChanged(ref _ReadyToRun, value);
    }

    private int _PageLimit;

    public int PageLimit
    {
        get => _PageLimit;
        set
        {
            if (parser != null)
                parser.PageLimit = value;
            this.RaiseAndSetIfChanged(ref _PageLimit, value);
        }
    }


    private string? _CurrentURL;
    public string? CurrentURL
    {
        get => _CurrentURL;
        set => this.RaiseAndSetIfChanged(ref _CurrentURL, value);
    }

    private string? _URL;
    public string URL
    {
        get => _URL!;
        set
        {
            var m = url_validate_pattern.Match(value);
            ReadyToRun = m.Success;
            this.RaiseAndSetIfChanged(ref _URL, value);
        }
    }

    private string? _StartButtonText;
    public string StartButtonText
    {
        get => _StartButtonText!;
        set => this.RaiseAndSetIfChanged(ref _StartButtonText, value);

    }


    private string _DomainLinks;
    public string DomainLinks
    {
        get => _DomainLinks!;
        set
        {
            Console.WriteLine($"DomainLinks updated. Lenght: {value.Length}");
            this.RaiseAndSetIfChanged(ref _DomainLinks, value);
        }
    }

    private string[] _DomainImages;
    public string[] DomainImages
    {
        get => _DomainImages!;
        set => this.RaiseAndSetIfChanged(ref _DomainImages, value);
    }

    private int _LinkVisitedCounter;
    public int LinkVisitedCounter
    {
        get => _LinkVisitedCounter!;
        set => this.RaiseAndSetIfChanged(ref _LinkVisitedCounter, value);
    }
    public MainWindowViewModel()
    {
                IsProcessing = false;
        ReadyToRun = false;
        URL = "https://ssau.ru";
        StartButtonText = "Построить граф";
        LinkVisitedCounter = 0;
        parser = new Parser();
				RPMLimit = 120;
        PageLimit = 500;
        parser.AddNewPageNotifier(UpdateCurrentLink);
        parser.AddStatusChangedNotifier(UpdateParserStatusDescr);
    }


    public async void StartButton()
    {
        switch (ParserStatus)
        {
            case Parser.ParserStatus.Waiting:
                parser.URL = URL;
                parser.PauseToggle();
                IsProcessing = !IsProcessing;
                StartButtonText = "Пауза";
                break;
            case Parser.ParserStatus.Processing:

                parser.PauseToggle();


                IsProcessing = !IsProcessing;
                StartButtonText = "Продолжить";
                break;
            case Parser.ParserStatus.Paused:
                IsProcessing = !IsProcessing;
                StartButtonText = "Пауза";
                parser.PauseToggle();
                break;
        }
    }

    public void UpdateCurrentLink(string url)
    {
        CurrentURL = url;
        LinkVisitedCounter++;
    }


    public void UpdateParserStatusDescr(Parser.ParserStatus status)
    {
        ParserStatus = status;
        UpdateResults();
    }

    private async void UpdateResults()
    {
        if (ParserStatus == Parser.ParserStatus.Stopping)
        {
            while (ParserStatus != Parser.ParserStatus.Paused)
                await Task.Delay(10);
        }

        DomainLinks = String.Join("\n", parser.DomainTree);
        DomainImages = parser.DomainImages.ToArray();

    }
}

