namespace WelfareSystem.Api.Services;

// An unsupervised, non-parametric nearest-neighbour model. Its distance is not a fraud probability.
public static class AmountNoveltyModel {
    public record Result(bool Ready, int Samples, double Distance, double Threshold, bool IsNovel);
    public static Result Evaluate(IEnumerable<double> approvedAmounts, double candidate) {
        var samples = approvedAmounts.Where(x => double.IsFinite(x) && x > 0).Take(500).ToArray();
        if (samples.Length < 50 || !double.IsFinite(candidate) || candidate <= 0) return new(false, samples.Length, 0, 0, false);
        var scale = Math.Max(1, samples.Order().ElementAt(samples.Length / 2));
        double Distance(double value, int? except) => samples.Select((x, i) => new { x, i }).Where(x => x.i != except).Select(x => Math.Abs(x.x - value) / scale).Order().Take(5).Average();
        var trainingDistances = samples.Select((value, index) => Distance(value, index)).Order().ToArray();
        var threshold = Math.Max(0.15, trainingDistances[(int)Math.Floor((trainingDistances.Length - 1) * .95)] * 1.5);
        var distance = Distance(candidate, null);
        return new(true, samples.Length, distance, threshold, distance > threshold);
    }
}
