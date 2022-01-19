using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;

namespace NextEpisode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Task.WhenAll(
                ParseSeries("https://next-episode.net/the-blacklist"),
                ParseSeries("https://next-episode.net/family-guy"),
                ParseSeries("https://next-episode.net/rick-and-morty"),
                ParseSeries("https://next-episode.net/9-1-1"),
                ParseSeries("https://next-episode.net/ncis"),
                ParseSeries("https://next-episode.net/seal-team"),
                ParseSeries("https://next-episode.net/s.w.a.t."),
                ParseSeries("https://next-episode.net/the-good-doctor"),
                ParseSeries("https://next-episode.net/black-mirror", false),
                ParseSeries("https://next-episode.net/south-park"),
                ParseSeries("https://next-episode.net/condor", false),
                ParseSeries("https://next-episode.net/tom-clancys-jack-ryan", false),
                ParseSeries("https://next-episode.net/the-walking-dead", false),
                ParseSeries("https://next-episode.net/mayday"),
                ParseSeries("https://next-episode.net/fear-the-walking-dead"),
                ParseSeries("https://next-episode.net/american-dad"),
                ParseSeries("https://next-episode.net/tatort"),
                ParseSeries("https://next-episode.net/strater"),
                ParseSeries("https://next-episode.net/blackout-2021"),
                ParseSeries("https://next-episode.net/sherlock"),
                ParseSeries("https://next-episode.net/mayday")
            ).ContinueWith(results =>
            {
                var allResults = results.Result.SelectMany(x => x).ToArray();

                List<string> titleDuplicateCheck = new List<string>();

                DateTime endDate = DateTime.Today;
                for (DateTime dt = DateTime.Now.AddMonths(-1); dt <= DateTime.Now.AddMonths(1); dt = dt.AddDays(1))
                {
                    var dayitem = new TreeViewItem();
                    dayitem.Header = dt.Date.ToString("D");
                    dayitem.IsExpanded = true;
                    if (dt.Date == DateTime.Today)
                        dayitem.Background = Brushes.Red;


                    titleDuplicateCheck.Clear();
                    
                    foreach (var result in allResults.Where(x => x.date.Date == dt.Date))
                    {
                        if (titleDuplicateCheck.Any(x => x == result.title))
                            continue;
                        titleDuplicateCheck.Add(result.title);

                        var episodeItem = new TreeViewItem();
                        episodeItem.Header =
                            $"{result.seriesName} - S{result.season:D2}ES{result.episode:D2} - {result.title}";
                        dayitem.Items.Add(episodeItem);
                    }
                    
                    TreeView.Items.Add(dayitem);

                    if (dt.Date == DateTime.Today)
                        dayitem.BringIntoView();

                }

            }, TaskScheduler.FromCurrentSynchronizationContext());

        }

        public class Result
        {
            public enum EpisodeType
            {
                Previous,
                Next
            }


            public EpisodeType type;
            public string seriesName;
            public string title;
            public int season;
            public string episode;
            public DateTime date;
        }

        async Task<List<Result>> ParseSeries(string URL, bool withSeason = true)
        {
            List<Result> results = new List<Result>();

            var url = URL;
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);

            var seriesName = doc.DocumentNode.SelectSingleNode("//div[@id=\"show_name\"]")?.InnerText?.Trim();

            var prevEpisode = doc.DocumentNode.SelectSingleNode("//div[@id=\"previous_episode\"]");
            if (prevEpisode != null)
            {
                var prevSubheadlines = prevEpisode.SelectNodes(".//div[@class=\"subheadline\"]");

                var title = prevEpisode.SelectSingleNode(".//div[@class=\"sub_main\"]")?.InnerText?.Trim();
                var date = prevSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Date"))?.NextSibling?.InnerText?.Trim();
                var season = prevSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Season"))?.NextSibling?.InnerText?.Trim();
                var episode = prevSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Episode"))?.NextSibling?.InnerText?.Trim();

                if (title != null && date != null && season != null && episode != null)
                {
                    try
                    {
                        if (int.TryParse(episode, out int episodeInt))
                            episode = $"{episodeInt:D2}";

                        results.Add(new Result
                        {
                            title = title,
                            seriesName = seriesName ?? "",
                            season = int.Parse(season),
                            episode = episode,
                            type = Result.EpisodeType.Previous,
                            date = DateTime.Parse(date)
                        });
                    }
                    catch (Exception ex)
                    {
                        Debugger.Break();
                    }
                }

            }


            var nextEpisode = doc.DocumentNode.SelectSingleNode("//div[@id=\"next_episode\"]");
            var nextSubheadlines = nextEpisode?.SelectNodes(".//div[@class=\"subheadline\"]");
            if (nextSubheadlines != null)
            {
                var title = nextEpisode.SelectSingleNode(".//div[@class=\"sub_main\"]")?.InnerText?.Trim();
                var date = nextSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Date"))?.NextSibling?.InnerText?.Trim();
                var season = nextSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Season"))?.NextSibling?.InnerText?.Trim();
                var episode = nextSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Episode"))?.NextSibling?.InnerText?.Trim();

                if (title != null && date != null && season != null && episode != null)
                {

                    if (int.TryParse(episode, out int episodeInt))
                        episode = $"{episodeInt:D2}";

                    results.Add(new Result
                    {
                        title = title,
                        season = int.Parse(season),
                        episode = episode,
                        type = Result.EpisodeType.Next,
                        date = DateTime.Parse(date),
                        seriesName = seriesName ?? ""
                    });
                }
            }
          

            var seasons = doc.DocumentNode.SelectNodes("//div[@id=\"inner_schedule_seasons\"]/a/@href");

            if (withSeason && seasons != null)
                foreach (var s in seasons.Select(x => x.GetAttributeValue("href", "")).Take(1))
                {
                    results.AddRange(await ParseSeason($"https://next-episode.net/{s}", seriesName));
                }

            return results;
        }

        async Task<List<Result>> ParseSeason(string URL, string seriesName)
        {
            // https://next-episode.net/9-1-1/season-4


            List<Result> results = new List<Result>();

            var url = URL;
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);

            var episodes = doc.DocumentNode.SelectNodes("//tr[@itemprop=\"episode\"]");

            var season = int.Parse(URL.Substring(URL.LastIndexOf('-')+1));

            foreach (var episode in episodes)
            {

                var episodeNr = episode.SelectSingleNode("td[@class=\"number\"]")?.InnerText?.Trim();
                var date = episode.SelectSingleNode("td[@class=\"date\"]")?.InnerText?.Trim();
                var title = episode.SelectSingleNode("td/h2")?.InnerText?.Trim();

                if (int.TryParse(episodeNr, out int episodeInt))
                    episodeNr = $"{episodeInt:D2}";

                if (date == null)
                    continue;

                results.Add(new Result
                {
                    title = title,
                    season = season,
                    episode = episodeNr,
                    type = DateTime.Parse(date) < DateTime.Now ? Result.EpisodeType.Previous : Result.EpisodeType.Next,
                    date = DateTime.Parse(date),
                    seriesName = seriesName
                });

            }



            return results;
        }

    }
}
