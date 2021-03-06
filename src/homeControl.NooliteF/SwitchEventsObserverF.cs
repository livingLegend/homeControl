using System;
using System.Threading;
using System.Threading.Tasks;
using homeControl.Domain.Events.Switches;
using homeControl.NooliteF.SwitchController;
using Serilog;

namespace homeControl.NooliteF
{
    internal sealed class SwitchEventsObserverF : IObserver<AbstractSwitchEvent>, IDisposable
    {
        private const int DelayBeforeProcessInMs = 50;

        private readonly ISwitchController _switchController;
        private readonly ILogger _log;
        private readonly CancellationToken _ct;
        private readonly SemaphoreSlim _completionSemaphore = new SemaphoreSlim(0, 1);

        public SwitchEventsObserverF(ISwitchController switchController, ILogger log, CancellationToken ct)
        {
            Guard.DebugAssertArgumentNotNull(switchController, nameof(switchController));
            Guard.DebugAssertArgumentNotNull(log, nameof(log));
            
            _switchController = switchController;
            _log = log;
            _ct = ct;
        }


        public Task Completion(CancellationToken ct)
        {
            return _completionSemaphore.WaitAsync(ct);
        }
            
        public void OnCompleted()
        {
            lock (_lock)
            {
                if (_processingTask != null)
                {
                    _processingTask.ContinueWith(t => DoComplete(), _ct);
                }
                else
                {
                    DoComplete();
                }
            }
        }

        private void DoComplete()
        {
            _log.Debug("Complete!");
            _completionSemaphore.Release();
        }
            

        public void OnError(Exception error)
        {
            OnCompleted();
        }

        private readonly object _lock = new object();
        private AbstractSwitchEvent _waitingForProcess;
        private Task _processingTask;
            
        public void OnNext(AbstractSwitchEvent next)
        {
            _log.Debug("New event: {switchEvent}.", next);
            
            lock (_lock)
            {
                if (!SkipCurrentEvent(_waitingForProcess, next) && _waitingForProcess != null)
                {
                    HandleEvent(_waitingForProcess);
                }
                else
                {
                    _log.Debug("Cancelling event processing: {switchEvent}.", _waitingForProcess);
                }

                _waitingForProcess = next;
                var current = next;
                    
                _log.Debug("Enqueuing event: {switchEvent}.", next);
                _processingTask = Task
                    .Delay(DelayBeforeProcessInMs, _ct)
                    .ContinueWith(t =>
                    {
                        if (_waitingForProcess != current)
                        {
                            _log.Debug("Event {switchEvent} has been cancelled, skipping.", current);
                            return;
                        }

                        lock (_lock)
                        {
                            if (_waitingForProcess != current)
                            {
                                _log.Debug("Event {switchEvent} has been cancelled, skipping.", current);
                                return;
                            }

                            HandleEvent(current);
                            _waitingForProcess = null;
                            _processingTask = null;
                        }
                    }, _ct);
            }
        }
            
        private static bool SkipCurrentEvent(AbstractSwitchEvent current, AbstractSwitchEvent next)
        {
            return IsOnOrOff(current) && IsOnOrOff(next)
                   || IsSetPower(current) && IsSetPower(next);
        }
        private static bool IsOnOrOff(AbstractSwitchEvent @event) => @event is TurnSwitchOnEvent || @event is TurnSwitchOffEvent;
        private static bool IsSetPower(AbstractSwitchEvent @event) => @event is SetSwitchPowerEvent;

        private void HandleEvent(AbstractSwitchEvent switchEvent)
        {
            Guard.DebugAssertArgumentNotNull(switchEvent, nameof(switchEvent));

            if (!_switchController.CanHandleSwitch(switchEvent.SwitchId))
            {
                _log.Warning("Switch not supported: {SwitchId}", switchEvent.SwitchId);
                return;
            }

            if (switchEvent is TurnSwitchOnEvent)
            {
                _switchController.TurnOn(switchEvent.SwitchId);
                _log.Information("Switch turned on: {SwitchId}", switchEvent.SwitchId);
            }
            else if (switchEvent is TurnSwitchOffEvent)
            {
                _switchController.TurnOff(switchEvent.SwitchId);
                _log.Information("Switch turned off: {SwitchId}", switchEvent.SwitchId);
            }
            else if (switchEvent is SetSwitchPowerEvent setPower)
            {
                _switchController.SetPower(switchEvent.SwitchId, setPower.Power);
                _log.Information("Adjusted switch power: {SwitchId}, {Power:G}", setPower.SwitchId, setPower.Power);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
            
        public void Dispose()
        {
            _completionSemaphore?.Dispose();
        }
    }
}