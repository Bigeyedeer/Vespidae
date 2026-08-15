using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The pool of "did you know" lines the loading screen draws from.
///
/// The authored list lives in <c>Assets/Resources/WaspFacts.txt</c>, one fact per line, so the text can
/// be rewritten without touching code. The built-in list below is only the fallback for a build where
/// that file is missing or empty.
///
/// Facts are handed out in a shuffled cycle rather than picked at random each time, so a player who
/// loads a few matches in a row does not see the same line twice before seeing the rest.
/// </summary>
public static class WaspFacts
{
    private const string ResourcePath = "WaspFacts";
    private const string CommentPrefix = "#";

    private static readonly string[] BuiltInFacts =
    {
        "Most wasps are solitary. Of the tens of thousands of described species, only a small fraction live in colonies at all.",
        "Only female wasps can sting. The sting is a modified egg-laying tube, so males never had one to begin with.",
        "Paper wasps really do make paper: they scrape plant fibre, chew it with saliva, and spread it into a nest by hand.",
        "A paper wasp colony starts with a single overwintered queen. Everything you command grew out of one insect.",
        "Adult wasps live on sugar, but their larvae need meat. The colony hunts caterpillars to feed the brood.",
        "A single social wasp colony can take thousands of caterpillars and aphids off surrounding plants in one season.",
        "Wasps pollinate too. They visit flowers for nectar and carry pollen from bloom to bloom while they do it.",
        "The European paper wasp reached South Africa's Western Cape in the late 2000s and has been spreading ever since.",
        "The German wasp has been established in the Western Cape since the 1970s and raids honeybee hives for protein.",
        "The Cape Floral Region holds around nine thousand plant species, and roughly seven in ten grow nowhere else on Earth.",
        "Wasps recognise their nestmates by smell. A colony carries a shared chemical signature.",
        "Killing a native wasp nest removes free pest control. Identify before you intervene."
    };

    private static readonly List<string> shuffled = new List<string>();
    private static string[] loaded;
    private static int cursor;

    /// <summary>Every fact currently in the pool, authored file first and built-in list as fallback.</summary>
    public static IReadOnlyList<string> All
    {
        get
        {
            EnsureLoaded();
            return loaded;
        }
    }

    /// <summary>
    /// The next fact to show. Walks a shuffled copy of the pool and reshuffles once it runs out, so
    /// repeats are as far apart as the pool allows.
    /// </summary>
    public static string Next()
    {
        EnsureLoaded();

        if (loaded.Length == 0)
            return string.Empty;

        if (cursor >= shuffled.Count)
            Reshuffle();

        return shuffled[cursor++];
    }

    /// <summary>Forces the authored file to be read again. Useful after editing it during play mode.</summary>
    public static void Reload()
    {
        loaded = null;
        shuffled.Clear();
        cursor = 0;
    }

    private static void EnsureLoaded()
    {
        if (loaded != null)
            return;

        List<string> facts = new List<string>();
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);

        if (asset != null)
        {
            foreach (string line in asset.text.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith(CommentPrefix))
                    continue;

                facts.Add(trimmed);
            }
        }

        if (facts.Count == 0)
            facts.AddRange(BuiltInFacts);

        loaded = facts.ToArray();
        Reshuffle();
    }

    private static void Reshuffle()
    {
        shuffled.Clear();
        shuffled.AddRange(loaded);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            (shuffled[i], shuffled[swap]) = (shuffled[swap], shuffled[i]);
        }

        cursor = 0;
    }
}
