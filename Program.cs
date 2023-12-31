﻿namespace sysprog3;

using Catalyst;
using Mosaik.Core;
using Newtonsoft.Json.Linq;

using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
public static class Program
{
    
    

    static public async Task<int> Main(string[] args)
    {
        Catalyst.Models.English.Register(); //You need to pre-register each language (and install the respective NuGet Packages)

        

        var reviewStream = new ReviewStream();

        var reviewObserver1 = new ReviewObesrver("a");
        var reviewObserver2 = new ReviewObesrver("b");
        var reviewObserver3 = new ReviewObesrver("c");
        
        var filteredStream = reviewStream.Where(p => p.Rating == 1);

        var subription1 = filteredStream.Subscribe(reviewObserver1);
        var subription2 = filteredStream.Subscribe(reviewObserver2);
        var subription3 = filteredStream.Subscribe(reviewObserver3);

        string? location = Console.ReadLine();
        if (location == null)
        {
            location = string.Empty;
        }
        string? pricesString = Console.ReadLine();
        if (pricesString == null)
        {
            pricesString = string.Empty; 
        }
        List<int> prices = new List<int>();
        foreach(string price in pricesString.Split(' '))
        {
            prices.Add(int.Parse(price));
        }
        reviewStream.GetReviews(location, prices.ToArray());
        Console.ReadLine();

        subription1.Dispose();
        subription2.Dispose();
        subription3.Dispose();
        
        return 0;
    }
}





