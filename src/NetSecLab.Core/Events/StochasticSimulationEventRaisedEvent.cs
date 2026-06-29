using NetSecLab.Core.Models;

namespace NetSecLab.Core.Events;

public sealed class StochasticSimulationEventRaisedEvent
{
    public StochasticSimulationEventRaisedEvent(StochasticSimulationEvent simulationEvent)
    {
        SimulationEvent = simulationEvent;
    }

    public StochasticSimulationEvent SimulationEvent { get; }
}
