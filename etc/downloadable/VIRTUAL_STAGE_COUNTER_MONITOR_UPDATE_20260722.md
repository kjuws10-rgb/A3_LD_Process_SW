# Virtual Stage Counter and Process Monitor V6

## Purpose

Automation1 Virtual mode cannot create the third-party Stage Y axis or receive its physical AUX encoder. The simulation therefore uses a software counter inside AeroScript and exposes that counter to the WPF client.

## Reserved Controller Variables

```text
$rglobal[0]  simulated Stage Y position
$rglobal[1]  simulated Stage speed
$rglobal[2]  next Stage target
$iglobal[0]  current generated process sequence
$iglobal[1]  total generated process targets
```

The Controller configuration must allocate at least three global real variables and two global integer variables.

## Counter Formula

```text
PositionIncrement = StageSpeed * VirtualStageTickSeconds
NextPosition = CurrentPosition + Direction * PositionIncrement
```

`AdvanceVirtualStageCounter()` waits one Tick with `Dwell()`, updates `$rglobal[0]`, and clamps the final increment to `$rglobal[2]`. No `Y`, `MoveAbsolute(Y, ...)`, Stage `PositionFeedback`, AUX, Laser, or PSO command is generated.

## Reverse Transport Order

The physical board feature order is defined in board coordinates, not by the sign of the Stage controller. Commands are sorted as follows:

```text
1. LocalY ascending: AK1 side -> AK2 side
2. LocalX ascending within the same Y row
3. Scanner head index
```

The filtered coordinates written to one script retain this order and receive a contiguous monitor sequence from 1 to N.

## WPF Monitoring

The WPF client reads the Controller global variables every 300 ms while a task is running. It updates only:

- Virtual Stage Y text
- Current Cell and Scanner text
- Process progress bar
- Current board-cell outline

The board and matrix canvases are not rebuilt for each status sample.

## UI Performance Changes

- Window resize redraw is delayed by 120 ms to collapse repeated resize events.
- Only the currently selected Design, Process, or Review matrix is created.
- Controller log text is bounded to prevent indefinite memory growth.
- Process monitoring changes existing visual properties instead of recreating controls.
