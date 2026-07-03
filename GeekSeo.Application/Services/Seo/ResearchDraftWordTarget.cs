namespace GeekSeo.Application.Services.Seo;

public static class ResearchDraftWordTarget
{
  public const int MinPillarWordCount = 1600;
  public const int DefaultPillarWordCount = 1800;
  public const int MinWordsPerMethodologySection = 250;
  public const int MethodologySectionCount = 4;

  public static int Resolve(int requestedWordCount, int benchmarkMedian) =>
      Math.Max(
          MinPillarWordCount,
          requestedWordCount > 0
              ? requestedWordCount
              : benchmarkMedian > 0
                  ? benchmarkMedian
                  : DefaultPillarWordCount);

  public static int WordsPerMethodologySection(int totalTarget) =>
      Math.Max(MinWordsPerMethodologySection, totalTarget / MethodologySectionCount);

  public static string BuildLengthInstructions(int totalTarget)
  {
    var perSection = WordsPerMethodologySection(totalTarget);
    return
        $"Minimum article length: {totalTarget} words total. "
        + $"Each of the four methodology <h2> body sections: at least 2–3 substantive paragraphs (~{perSection} words each). "
        + $"Closing FAQ: each of the {ContentWritingRules.ClosingFaqCount} answers must be a full paragraph (40–80 words). "
        + "Do not stop early — depth and implementation detail matter more than brevity.";
  }
}
