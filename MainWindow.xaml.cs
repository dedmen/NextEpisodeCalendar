using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
using Newtonsoft.Json;

namespace NextEpisode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<DateTime, TreeViewItem> dateTreeItems = new Dictionary<DateTime, TreeViewItem>();

        public MainWindow()
        {
            InitializeComponent();

            for (DateTime dt = DateTime.Now.AddMonths(-1); dt <= DateTime.Now.AddMonths(1); dt = dt.AddDays(1))
            {
                var dayitem = new TreeViewItem
                {
                    Header = dt.Date.ToString("D"),
                    IsExpanded = true
                };
                if (dt.Date == DateTime.Today)
                    dayitem.Background = Brushes.Red;

                dateTreeItems[dt.Date] = dayitem;

                TreeView.Items.Add(dayitem);

                if (dt.Date == DateTime.Today)
                    dayitem.BringIntoView();
            }


            Task.WhenAll(
                ParseAndAddSeries(ParseSeries(69,"The Blacklist")),
                ParseAndAddSeries(ParseSeries(84, "Family Guy")),
                ParseAndAddSeries(ParseSeries(216, "Rick and Morty")),
                ParseAndAddSeries(ParseSeries(28152, "9-1-1")),
                ParseAndAddSeries(ParseSeries(60, "NCIS")),
                ParseAndAddSeries(ParseSeries(28160, "Seal Team")),
                ParseAndAddSeries(ParseSeries(21532, "S.W.A.T.")),
                ParseAndAddSeries(ParseSeries(21845,"The Good Doctor")),
                ParseAndAddSeries(ParseSeries(305, "Black Mirror")),
                ParseAndAddSeries(ParseSeries(112, "South Park")),
                ParseAndAddSeries(ParseSeries(25110, "Condor")),
                ParseAndAddSeries(ParseSeries(73, "The Walking Dead")),
                ParseAndAddSeries(ParseSeries(1824, "Fear the Walking Dead")),
                ParseAndAddSeries(ParseSeries(50287, "Tales of the Walking Dead")),
                ParseAndAddSeries(ParseSeries(60879, "The Walking Dead: Dead City")),
                ParseAndAddSeries(ParseSeries(45194, "The Walking Dead: World Beyond")),
                ParseAndAddSeries(ParseSeries(64501, "The Walking Dead: Daryl Dixon")),
                ParseAndAddSeries(ParseSeries(215, "American Dad!")),
                ParseAndAddSeries(ParseSeries(146836, "Tatort")),
                ParseAndAddSeries(ParseSeries("https://next-episode.net/strater")),
                //ParseAndAddSeries(ParseSeries(58014, "Blackout")), // only check rarely, series has ended
                ParseAndAddSeries(ParseSeries(335, "Sherlock")), // only check rarely
                ParseAndAddSeries(ParseSeries(1414, "Mayday")),
                ParseAndAddSeries(ParseSeries(5079, "Tom Clancy's Jack Ryan")),
                ParseAndAddSeries(ParseSeries(41134, "Feuer & Flamme")),
                ParseAndAddSeries(ParseSeries(46562, "The Last of Us")) // only check rarely
            ).ContinueWith(results =>
            {
                dateTreeItems[DateTime.Today.Date].BringIntoView();
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

        async Task ParseAndAddSeries(Task<List<Result>> parseTask)
        {
            await parseTask.ContinueWith(results =>
            {
                var allResults = results.Result;

                List<string> titleDuplicateCheck = new List<string>();

                titleDuplicateCheck.Clear();

                foreach (var result in allResults)
                {
                    if (!dateTreeItems.TryGetValue(result.date.Date, out var dayitem))
                        continue;

                    if (titleDuplicateCheck.Any(x => x == result.title))
                        continue;
                    titleDuplicateCheck.Add(result.title);

                    var episodeItem = new TreeViewItem
                    {
                        Header = $"{result.seriesName} - S{result.season:D2}ES{result.episode:D2} - {result.title}"
                    };
                    dayitem.Items.Add(episodeItem);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        async Task<List<Result>> ParseSeries(string URL, bool withSeason = true)
        {
            List<Result> results = new List<Result>();

            var url = URL;

            if (url.Contains("next-episode"))
            {
                await ParseSeries_NextEpisode(withSeason, url, results);
            }

            return results;
        }

        async Task<List<Result>> ParseSeries(int showId, string seriesName)
        {
            List<Result> results = new List<Result>();

            await ParseSeries_tvmaze(showId, seriesName, results);
  

            return results;
        }

        private async Task ParseSeries_NextEpisode(bool withSeason, string url, List<Result> results)
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);
            await Task.Delay(TimeSpan.FromMilliseconds(1000)); // rate limiting

            var seriesName = doc.DocumentNode.SelectSingleNode("//div[@id=\"show_name\"]")?.InnerText?.Trim();

            var prevEpisode = doc.DocumentNode.SelectSingleNode("//div[@id=\"previous_episode\"]");
            if (prevEpisode != null)
            {
                var prevSubheadlines = prevEpisode.SelectNodes(".//div[@class=\"subheadline\"]");

                var title = prevEpisode.SelectSingleNode(".//div[@class=\"sub_main\"]")?.InnerText?.Trim();
                var date = prevSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Date"))?.NextSibling?.InnerText?.Trim();
                var season = prevSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Season"))?.NextSibling?.InnerText
                    ?.Trim();
                var episode = prevSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Episode"))?.NextSibling?.InnerText
                    ?.Trim();

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
                var season = nextSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Season"))?.NextSibling?.InnerText
                    ?.Trim();
                var episode = nextSubheadlines.FirstOrDefault(x => x.InnerText.Contains("Episode"))?.NextSibling?.InnerText
                    ?.Trim();

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
                    results.AddRange(await ParseSeason_NextEpisode($"https://next-episode.net/{s}", seriesName));
                }
        }

        async Task<List<Result>> ParseSeason_NextEpisode(string URL, string seriesName)
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

        struct TVMaze_Season
        {
            public int Id { get; set; }
            public string Url { get; set; }
            public int Number { get; set; }
            public string Name { get; set; }

            public int? EpisodeOrder { get; set; }
            public DateTime? PremiereDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        struct TVMaze_Episode
        {
            public int Id { get; set; }
            public string Url { get; set; }
            public int Season { get; set; }
            public int Number { get; set; }
            public string Name { get; set; }

            public DateTime AirDate { get; set; }
        }


        private async Task ParseSeries_tvmaze(int showId, string seriesName, List<Result> results)
        {
            var client = new HttpClient();
            //var body = await client.GetAsync($"https://api.tvmaze.com/shows/{showId}/seasons");
            //var seasons = JsonConvert.DeserializeObject<List<TVMaze_Season>>(await body.Content.ReadAsStringAsync()).Where(x => x.EndDate > DateTime.Now.AddMonths(-1));

            var body = await client.GetAsync($"https://api.tvmaze.com/shows/{showId}/episodes");
            var episodes = JsonConvert.DeserializeObject<List<TVMaze_Episode>>(await body.Content.ReadAsStringAsync()).Where(x => x.AirDate > DateTime.Now.AddMonths(-1));

            results.AddRange(
                episodes.Select(ep => new Result
                {
                    title = ep.Name,
                    season = ep.Season,
                    episode = $"{ep.Number:D2}",
                    type = Result.EpisodeType.Next,
                    date = ep.AirDate,
                    seriesName = seriesName

                })
            );
        }




    }
}
