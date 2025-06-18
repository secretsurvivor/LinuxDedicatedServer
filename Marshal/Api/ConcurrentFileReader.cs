using System.Collections.Concurrent;

namespace LinuxDedicatedServer.Api;

public class ConcurrentFileReader<TResult>(IEnumerable<string> files)
{
    private readonly Queue<string> fileQueue = new(files);
    private int threadCount = 1;
    private bool hasThrottled = false;
    private readonly int maxConcurrency = Environment.ProcessorCount * 2;

    public async Task<IEnumerable<TResult>> Run(Func<FileStream, Task<TResult>> task)
    {
        List<TResult> results = [];
        List<string> bag = [];

        while (fileQueue.Count > 0 || bag.Count > 0)
        {
            while (fileQueue.Count > 0 && bag.Count < threadCount)
            {
                bag.Add(fileQueue.Dequeue());
            }

            var (success, successBag, failedBag) = await RunBatchAsync(bag, task);

            if (!success)
            {
                Throttle();
                foreach (var file in failedBag)
                    fileQueue.Enqueue(file);
            }
            else
            {
                Accelerate();
            }

            results.AddRange(successBag);
            bag.Clear();
        }

        return results;
    }

    private void Throttle()
    {
        threadCount = Math.Max(1, threadCount - 2);
        hasThrottled = true;
    }

    private void Accelerate()
    {
        if (!hasThrottled)
            threadCount = Math.Min(maxConcurrency, threadCount * 2);
    }

    private async Task<(bool success, List<TResult> successBag, List<string> failedBag)> RunBatchAsync(IEnumerable<string> files, Func<FileStream, Task<TResult>> task)
    {
        var successBag = new ConcurrentBag<TResult>();
        var failedBag = new ConcurrentBag<string>();

        var tasks = files.Select(async file =>
        {
            try
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var result = await task(stream);
                successBag.Add(result);
            }
            catch (IOException)
            {
                failedBag.Add(file);
            }
        });

        await Task.WhenAll(tasks);

        return (failedBag.IsEmpty, successBag.ToList(), failedBag.ToList());
    }
}

public static class ConcurrentStringExtension
{
    public static Task<IEnumerable<TResult>> SelectFiles<TResult>(this IEnumerable<string> files, Func<FileStream, Task<TResult>> func)
    {
        var reader = new ConcurrentFileReader<TResult>(files);
        return reader.Run(func);
    }
}
