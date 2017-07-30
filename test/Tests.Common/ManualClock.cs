// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Utilities;

namespace Yarp.Tests.Common;

/// <summary>
/// Simulates passage of time, used for testing.
/// </summary>
/// <remarks>
/// This timer doesn't track real time, but instead tracks virtual time.
/// Time only advances when any of the following methods are called:
/// <list type="bullet">
/// <item><see cref="AdvanceClockBy"/></item>
/// <item><see cref="AdvanceClockTo(TimeSpan)"/></item>
/// <item><see cref="AdvanceStep"/></item>
/// </list>
/// </remarks>
public class ManualClock : IClock
{
    private readonly SortedList<TimeSpan, DelayItem> _delayItems = new SortedList<TimeSpan, DelayItem>();

    private TimeSpan _currentTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManualClock" /> class.
    /// </summary>
    /// <param name="initialTime">Initial value for current time. Zero if not specified.</param>
    public ManualClock(TimeSpan? initialTime = null)
    {
        _currentTime = initialTime ?? TimeSpan.Zero;
    }

    /// <inheritdoc/>
    public long TickCount => (long)_currentTime.TotalMilliseconds;

    /// <summary>
    /// Advances time by the specified amount.
    /// </summary>
    /// <param name="howMuch">How much to advance <see cref="CurrentTime"/> by.</param>
    public void AdvanceClockBy(TimeSpan howMuch)
    {
        AdvanceClockTo(_currentTime + howMuch);
    }

    /// <summary>
    /// Advances time to the specified point.
    /// </summary>
    /// <param name="targetTime">Advances <see cref="CurrentTime"/> until it equals <paramref name="targetTime"/>.</param>
    public void AdvanceClockTo(TimeSpan targetTime)
    {
        if (targetTime < _currentTime)
        {
            throw new InvalidOperationException("Time should not flow backwards");
        }

        // Signal any delays that have expired by advancing the clock.
        while (_delayItems.Count > 0 && _delayItems.ElementAt(0).Key <= targetTime)
        {
            AdvanceStep();
        }

        _currentTime = targetTime;
    }

    public DateTimeOffset GetUtcNow() => new DateTime(_currentTime.Ticks, DateTimeKind.Utc);

    public TimeSpan GetStopwatchTime() => _currentTime;

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        return DelayUntil(_currentTime + delay, cancellationToken);
    }

    public Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
    {
        return DelayUntil(_currentTime + TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
    }

    /// <summary>
    /// Advances time to schedule the next item of work.
    /// </summary>
    /// <returns>True if any timers were found and signaled.</returns>
    public bool AdvanceStep()
    {
        if (_delayItems.Count > 0)
        {
            var next = _delayItems.ElementAt(0);
            _currentTime = next.Key;

            // Note: this will unfortunately have O(N) cost. However, this code is only used for testing right now, and the list is generally short.
            // If that ever changes, consider using a PriorityQueue.
            _delayItems.RemoveAt(0);

            // Unblock the task. It's no longer asleep.
            next.Value.Signal.TrySetResult(0);

            // Note that TPL normally schedules tasks synchronously. When used with
            // the SingleThreadedTaskScheduler, we can assume all tasks have completed by the
            // time SetResult returns, provided that AdvanceClockTo was invoked outside of the task scheduler
            // loop.
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a task that completes when CurrentTime >= expiryTime.
    /// </summary>
    /// <param name="expiryTime">Time at which the returned task will be completed.</param>
    /// <param name="cancelationToken">Cancellation token for the created task.</param>
    /// <returns>A task which completed at <paramref name="expiryTime"/>.</returns>
    private async Task DelayUntil(TimeSpan expiryTime, CancellationToken cancelationToken)
    {
        if (expiryTime <= _currentTime)
        {
            return;
        }

        var delayTask = new DelayItem
        {
            When = expiryTime,
            Signal = new TaskCompletionSource<int>(cancelationToken),
        };

        var task = delayTask.Signal.Task;

        // Note: sorted list doesn't allow duplicates, so increment expiry until unique.
        while (_delayItems.ContainsKey(expiryTime))
        {
            expiryTime += TimeSpan.FromTicks(1);
        }

        _delayItems.Add(expiryTime, delayTask);

        using (cancelationToken.Register(() => CancelTask(delayTask)))
        {
            await task;
        }
    }

    private void CancelTask(DelayItem delayTask)
    {
        var i = _delayItems.IndexOfValue(delayTask);
        if (i != -1)
        {
            _delayItems.RemoveAt(i);
        }

        delayTask.Signal.TrySetCanceled();
    }

    private class DelayItem
    {
        public TimeSpan When { get; set; }

        public TaskCompletionSource<int> Signal { get; set; }
    }
}
