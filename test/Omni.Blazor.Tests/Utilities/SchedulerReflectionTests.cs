using Omni.Blazor.Utilities;

namespace Omni.Blazor.Tests.Utilities;

public class SchedulerReflectionTests
{
    private sealed class Appointment
    {
        public string Title { get; init; } = "Review";
    }

    [Fact]
    public void GetValue_reads_and_caches_existing_property()
    {
        var appointment = new Appointment();

        Assert.Equal("Review", SchedulerReflection.GetValue(appointment, nameof(Appointment.Title)));
        Assert.Equal(1, SchedulerReflection.CachedPropertyCount(typeof(Appointment)));
    }

    [Fact]
    public void Invalid_consumer_names_do_not_grow_cache()
    {
        var appointment = new Appointment();
        int before = SchedulerReflection.CachedPropertyCount(typeof(Appointment));

        for (int index = 0; index < 1_000; index++)
            Assert.Null(SchedulerReflection.GetValue(appointment, $"Missing{index}"));

        Assert.Equal(before, SchedulerReflection.CachedPropertyCount(typeof(Appointment)));
    }

    [Fact]
    public void Concurrent_reads_are_consistent()
    {
        var appointment = new Appointment();
        var values = new string?[256];

        Parallel.For(
            0,
            values.Length,
            index => values[index] = SchedulerReflection.GetValue(
                appointment,
                nameof(Appointment.Title)) as string);

        Assert.All(values, value => Assert.Equal("Review", value));
        Assert.Equal(1, SchedulerReflection.CachedPropertyCount(typeof(Appointment)));
    }
}
