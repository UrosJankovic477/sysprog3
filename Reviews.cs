namespace sysprog3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Catalyst;
using Mosaik.Core;
using Newtonsoft.Json.Linq;
using Version = Mosaik.Core.Version;
using Catalyst.Models;

public class Review
{
    public string Text;
    public int Rating;
}
public class ReviewStream : IObservable<Review>
{
    private readonly Subject<Review> reviewSubject = new Subject<Review>();
    public IDisposable Subscribe(IObserver<Review> observer)
    {
        return reviewSubject.Subscribe(observer);
    }
    private static string GetDocData(IDocument doc)
    {
        return $"Input text:\n\t'{doc.Value}'\n\nTokenized Value:\n\t'{doc.TokenizedValue(mergeEntities: true)}'\n\nEntities: \n{string.Join("\n", doc.SelectMany(span => span.GetEntities()).Select(e => $"\t{e.Value} [{e.EntityType.Type}]"))}";
    }

    public async Task<JToken> Search(string location, params int[] prices)
    {
        
        var client = new HttpClient();
        var url = $"https://api.yelp.com/v3/businesses/search?categories=cafes&location={location}&sort_by=best_match&limit=20";
        foreach (var price in prices)
        {
        url += $"&price={price}";
        }
        var requestUri = new Uri(url);
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = requestUri,
            Headers =
            {
                { "accept", "application/json" },
                { "Authorization", "Bearer wisPWwASEC-vJNrsxLKwF7YIOOuRZSIKuNQUlXID68xUnmH7cG2TcOZRnB-KMtAb0_6gns4JALPNFMPM_pnRM0cVqcWhU5d_u2S_jsmvmMqh6SUSKSPvDB_-yI-RZHYx" },
            },
        };
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var parsed = JObject.Parse(body);
            return parsed["businesses"];
        }
    }

    public async Task<JToken> GetReviewsFull(string id)
    {
        
        var client = new HttpClient();
        var url = $"https://api.yelp.com/v3/businesses/{id}/reviews";
        var requestUri = new Uri(url);
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = requestUri,
            Headers =
            {
                { "accept", "application/json" },
                { "Authorization", "Bearer wisPWwASEC-vJNrsxLKwF7YIOOuRZSIKuNQUlXID68xUnmH7cG2TcOZRnB-KMtAb0_6gns4JALPNFMPM_pnRM0cVqcWhU5d_u2S_jsmvmMqh6SUSKSPvDB_-yI-RZHYx" },
            },
        };
        Console.WriteLine(url);
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var parsed = JObject.Parse(body);
            return parsed["reviews"];
        }
    }
    public async void GetReviews(string location, params int[] prices)
    {
        try
        {
            var result = await Search(location, prices);
        
            List<Task> tasks = new List<Task>();
            var nlp = await Pipeline.ForAsync(Language.English);
            nlp.Add(await AveragePerceptronEntityRecognizer.FromStoreAsync(language: Language.English, version: Version.Latest, tag: "WikiNER"));

            foreach (var item in result)
            {
                Task task = new Task(async () => {
                    var reviews = await GetReviewsFull(item["id"].ToString());
                    foreach(var r in reviews)
                    {
                        Review review = new Review()
                        {
                            Text = r["text"].ToString(),
                            Rating = int.Parse(r["rating"].ToString())
                        };
                       
                        
                        var doc = new Document(review.Text, Language.English);
                        nlp.ProcessSingle(doc);
                        
                        review.Text = GetDocData(doc);
                        reviewSubject.OnNext(review);


                    }
                });
                tasks.Add(task);
                task.Start();
                await Task.Delay(200);
                
            }
            await Task.WhenAll(tasks.ToArray());
            reviewSubject.OnCompleted();
        }
        catch (System.Exception ex)
        {
            reviewSubject.OnError(ex);
        }
    }
}
public class ReviewObesrver : IObserver<Review>
{
    public readonly string name;
    public ReviewObesrver(string name)
    {
        this.name = name;
    }
    public void OnCompleted()
    {
        Console.WriteLine($"{name}: Completed.");
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"{name}: Error!");
    }

    public void OnNext(Review value)
    {
        Console.Write($"{name}\nText: {value.Text}\nRating: {value.Rating}\n");
    }
}