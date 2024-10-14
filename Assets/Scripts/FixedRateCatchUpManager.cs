using System.Diagnostics;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

public unsafe class FixedRateCatchUpManager : IRateManager
{
    float m_FixedTimestep;

    /// <inheritdoc cref="IRateManager.Timestep"/>
    public float Timestep
    {
        get => m_FixedTimestep;
        set
        {
            m_FixedTimestep = math.clamp(value, 0.001f, 10);
        }
    }

    double m_LastFixedUpdateTime;
    long m_FixedUpdateCount;
    bool m_DidPushTime;
    double m_MaxFinalElapsedTime;

    /// <summary>
    /// Double rewindable allocators to remember before pushing in rate group allocators.
    /// </summary>
    DoubleRewindableAllocators* m_OldGroupAllocators = null;

    /// <summary>
    /// Construct a new instance
    /// </summary>
    /// <param name="fixedDeltaTime">The constant fixed timestep to use during system group updates (in seconds)</param>
    public FixedRateCatchUpManager(float fixedDeltaTime)
    {
        Timestep = fixedDeltaTime;
    }

    /// <inheritdoc cref="IRateManager.ShouldGroupUpdate"/>
    public bool ShouldGroupUpdate(ComponentSystemGroup group)
    {
        float worldMaximumDeltaTime = group.World.MaximumDeltaTime;
        float maximumDeltaTime = math.max(worldMaximumDeltaTime, m_FixedTimestep);

        // if this is true, means we're being called a second or later time in a loop
        if (m_DidPushTime)
        {
            group.World.PopTime();
            group.World.RestoreGroupAllocator(m_OldGroupAllocators);
        }
        else
        {
            m_MaxFinalElapsedTime = m_LastFixedUpdateTime + maximumDeltaTime;
        }

        double finalElapsedTime = math.min(m_MaxFinalElapsedTime, group.World.Time.ElapsedTime);
        if (m_FixedUpdateCount == 0)
        {
            // First update should always occur at t=0
        }
        else if (finalElapsedTime - m_LastFixedUpdateTime >= m_FixedTimestep)
        {
            // Advance the timestep and update the system group
            m_LastFixedUpdateTime += m_FixedTimestep;
        }
        else
        {
            // No update is necessary at this time.
            m_DidPushTime = false;
            return false;
        }

        m_FixedUpdateCount++;

        group.World.PushTime(new TimeData(
            elapsedTime: m_LastFixedUpdateTime,
            deltaTime: m_FixedTimestep));

        m_DidPushTime = true;

        m_OldGroupAllocators = group.World.CurrentGroupAllocators;
        group.World.SetGroupAllocator(group.RateGroupAllocators);
        return true;
    }

    internal void SetTime(double elapsedTime)
    {
        m_LastFixedUpdateTime = elapsedTime;
    }
}

public unsafe class VariableRateManager : IRateManager
{
    /// <summary>
    /// The minimum allowed update rate in Milliseconds
    /// </summary>
    private const uint MinUpdateRateMS = 1;

    /// <summary>
    /// Should the world have <see cref="TimeData"/> pushed to it?
    /// </summary>
    private readonly bool m_ShouldPushToWorld;

    /// <summary>
    /// A cached copy of <see cref="Stopwatch.Frequency"/> as a <see cref="float"/>.
    /// </summary>
    /// <remarks>This is used explicitly when trying to calculate the <see cref="m_Timestep"/>.</remarks>
    private readonly float m_TicksPerSecond = Stopwatch.Frequency;

    /// <summary>
    ///     The required number of ticks to trigger an update when compared against <see cref="m_TickCount"/>
    ///     during <see cref="ShouldGroupUpdate"/>.
    /// </summary>
    private readonly long m_UpdateRate;

    /// <summary>
    /// The latest polled ticks from the timer mechanism.
    /// </summary>
    private long m_CurrentTimestamp;

    /// <summary>
    /// The elapsed time which the rate manager has operated.
    /// </summary>
    /// <remarks>
    ///     This does not have any protection against rollover issues, and is only updated if
    ///     <see cref="m_ShouldPushToWorld"/> is toggled.
    /// </remarks>
    private double m_ElapsedTime;

    /// <summary>
    /// The previous iterations ticks from the timer mechanism.
    /// </summary>
    private long m_PreviousTimestamp;

    /// <summary>
    /// Was <see cref="TimeData"/> pushed to the world?
    /// </summary>
    private bool m_DidPushTime;

    /// <summary>
    /// An accumulator of ticks observed during <see cref="ShouldGroupUpdate"/>.
    /// </summary>
    private long m_TickCount;

    /// <summary>
    /// The calculated delta time between updates.
    /// </summary>
    private float m_Timestep;

    /// <summary>
    /// Double rewindable allocators to remember before pushing in rate group allocators.
    /// </summary>
    DoubleRewindableAllocators* m_OldGroupAllocators = null;

    /// <summary>
    /// Construct a <see cref="VariableRateManager"/> with a given Millisecond update rate.
    /// </summary>
    /// <remarks>
    ///     Utilizes an accumulator where when it exceeds the indicated tick count, triggers the update and
    ///     resets the counter.
    /// </remarks>
    /// <param name="updateRateInMS">
    ///     The update rate for the manager in Milliseconds, if the value is less then
    ///     <see cref="MinUpdateRateMS"/> it will be set to it.
    /// </param>
    /// <param name="pushToWorld">
    ///     Should <see cref="TimeData"/> be pushed onto the world? If systems inside of this group do not
    ///     require the use of the <see cref="World.Time"/>, a minor performance gain can be made setting this
    ///     to false.
    /// </param>
    public VariableRateManager(float time,uint updateRateInMS = 66, bool pushToWorld = true)
    {
        // Ensure update rate is valid
        if (updateRateInMS < MinUpdateRateMS)
        {
            updateRateInMS = MinUpdateRateMS;
        }

        // Cache our update rate in ticks
        m_UpdateRate = (long)(updateRateInMS * (Stopwatch.Frequency / 1000f));
        m_ShouldPushToWorld = pushToWorld;

        // Initialize our time data
        m_CurrentTimestamp = Stopwatch.GetTimestamp();
        m_PreviousTimestamp = m_CurrentTimestamp;

        // Make sure that the first call updates
        m_TickCount = m_UpdateRate;
        m_ElapsedTime = time;
    }

    /// <summary>
    /// Determines if the group should be updated this invoke.
    /// </summary>
    /// <remarks>The while loop happens once.</remarks>
    /// <param name="group">The system group to check</param>
    /// <returns>True if <paramref name="group"/> should update its member systems, or false if the group should skip its update.</returns>
    public bool ShouldGroupUpdate(ComponentSystemGroup @group)
    {
        // We're going to use the internal ticks to ensure this works in worlds without time systems.
        m_CurrentTimestamp = Stopwatch.GetTimestamp();

        // Calculate the difference between our current timestamp and the previous, but also account for the
        // possibility that the value may have rolled over.
        long difference;
        if (m_CurrentTimestamp < m_PreviousTimestamp)
        {
            // Rollover protection
            difference = (long.MaxValue - m_PreviousTimestamp) + m_CurrentTimestamp;
        }
        else
        {
            difference = m_CurrentTimestamp - m_PreviousTimestamp;
        }

        // Save/increment
        m_PreviousTimestamp = m_CurrentTimestamp;
        m_TickCount += difference;

        // Remove that time we pushed on the world
        if (m_ShouldPushToWorld && m_DidPushTime)
        {
            @group.World.PopTime();
            m_DidPushTime = false;
            group.World.RestoreGroupAllocator(m_OldGroupAllocators);
        }

        // We haven't elapsed enough ticks, thus false.
        if (m_TickCount < m_UpdateRate)
        {
            return false;
        }

        // Calculate what we believe is the delta elapsed since our last reset
        m_Timestep = m_TickCount / m_TicksPerSecond;

        // Push the current world time
        if (m_ShouldPushToWorld)
        {
            m_ElapsedTime += m_Timestep;
            group.World.PushTime(new TimeData(
                elapsedTime: m_ElapsedTime,
                deltaTime: m_Timestep));
            m_DidPushTime = true;

            m_OldGroupAllocators = group.World.CurrentGroupAllocators;
            group.World.SetGroupAllocator(group.RateGroupAllocators);
        }

        // Reset tick count
        m_TickCount = 0;
        return true;
    }

    /// <inheritdoc />
    public float Timestep
    {
        get => m_Timestep;
        set => m_Timestep = value;
    }
}